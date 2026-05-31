using BLL.Interfaces;
using Core.Configuration;
using Core.DTOs.Common;
using Core.DTOs.Documents;
using Core.Entities;
using DAL.Interfaces;
using Hangfire;
using Microsoft.Extensions.Logging;
using Microsoft.Extensions.Options;

namespace BLL.Services
{
    /// <summary>
    /// Handles document upload business rules, Supabase storage coordination, metadata persistence, 
    /// and enqueues background jobs for document chunking and embedding.
    /// </summary>
    public class DocumentService : IDocumentService
    {
        private readonly IDocumentRepository _documentRepository;
        private readonly IFolderRepository _folderRepository;
        private readonly ISupabaseStorageProvider _storageService;
        private readonly UploadOptions _uploadOptions;
        private readonly ILogger<DocumentService> _logger;
        private readonly IBackgroundJobClient _backgroundJobs;

        public DocumentService(
            IDocumentRepository documentRepository,
            IFolderRepository folderRepository,
            ISupabaseStorageProvider storageService,
            IOptions<UploadOptions> uploadOptions,
            ILogger<DocumentService> logger,
            IBackgroundJobClient backgroundJobs)
        {
            _documentRepository = documentRepository;
            _folderRepository = folderRepository;
            _storageService = storageService;
            _uploadOptions = uploadOptions.Value;
            _logger = logger;
            _backgroundJobs = backgroundJobs;
        }

        public async Task<IReadOnlyList<DocumentDto>> GetDocumentsForUserAsync(Guid userId)
        {
            var documents = await _documentRepository.GetByUserIdAsync(userId);
            return documents.Select(MapDocument).ToList();
        }

        public async Task<Result<DocumentDto>> UploadAsync(DocumentUploadDto uploadDto)
        {
            var validationError = ValidateUpload(uploadDto);
            if (!string.IsNullOrEmpty(validationError))
            {
                return Result<DocumentDto>.Failure(validationError);
            }

            var extension = Path.GetExtension(uploadDto.OriginalFileName).ToLowerInvariant();
            var storedFileName = $"{Guid.NewGuid():N}{extension}";
            var storagePath = BuildStoragePath(uploadDto.UserId, storedFileName);
            var now = DateTime.UtcNow;

            try
            {
                await _storageService.UploadAsync(storagePath, uploadDto.Content, NormalizeContentType(uploadDto.ContentType));
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Failed to upload file to storage for user {UserId}", uploadDto.UserId);
                return Result<DocumentDto>.Failure($"Supabase upload error: {ex.Message}");
            }

            try
            {
                Guid folderId;
                if (uploadDto.FolderId.HasValue && uploadDto.FolderId.Value != Guid.Empty)
                {
                    var folder = await _folderRepository.GetByIdWithOwnerAsync(uploadDto.FolderId.Value, uploadDto.UserId);
                    if (folder == null)
                    {
                        return Result<DocumentDto>.Failure("Thư mục không tồn tại hoặc bạn không có quyền truy cập.");
                    }
                    folderId = folder.Id;
                }
                else
                {
                    var folder = await _folderRepository.GetOrCreateDefaultUploadFolderAsync(uploadDto.UserId);
                    folderId = folder.Id;
                }

                var document = new Document
                {
                    Id = Guid.NewGuid(),
                    UserId = uploadDto.UserId,
                    FolderId = folderId,
                    Title = Path.GetFileNameWithoutExtension(uploadDto.OriginalFileName),
                    OriginalFileName = Path.GetFileName(uploadDto.OriginalFileName),
                    StoredFileName = storedFileName,
                    StoragePath = storagePath,
                    StorageUrl = storagePath,
                    MimeType = NormalizeContentType(uploadDto.ContentType),
                    FileType = extension.TrimStart('.'),
                    FileSize = uploadDto.FileSize,
                    ProcessingStatus = DocumentProcessingStatus.Uploaded,
                    UploadedAt = now,
                    CreatedAt = now,
                    UpdatedAt = now
                };

                await _documentRepository.AddAsync(document);

                // Enqueue the chunking job, followed by the embedding job
                var chunkingJobId = _backgroundJobs.Enqueue<IChunkingService>(x => x.ProcessFileChunkingAsync(document.Id, CancellationToken.None));
                _backgroundJobs.ContinueJobWith<IEmbeddingService>(chunkingJobId, x => x.ProcessEmbeddingsAsync(document.Id, CancellationToken.None));

                return Result<DocumentDto>.Success(MapDocument(document));
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Failed to save document metadata for user {UserId}", uploadDto.UserId);
                try
                {
                    await _storageService.DeleteAsync(storagePath);
                }
                catch (Exception cleanupEx)
                {
                    _logger.LogError(cleanupEx, "Failed to cleanup storage after DB save error.");
                }
                return Result<DocumentDto>.Failure($"Database save error: {ex.Message}");
            }
        }

        public async Task<Result> DeleteAsync(Guid documentId, Guid userId)
        {
            try
            {
                var document = await _documentRepository.GetByIdForUserAsync(documentId, userId);
                if (document == null)
                {
                    return Result.Failure("Document not found or access denied.");
                }

                try
                {
                    await _storageService.DeleteAsync(document.StoragePath);
                }
                catch (Exception ex)
                {
                    _logger.LogWarning(ex, "Failed to delete file from Supabase storage. Path: {StoragePath}", document.StoragePath);
                }

                await _documentRepository.DeleteAsync(document);
                return Result.Success();
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Failed to delete document {DocumentId}", documentId);
                return Result.Failure($"Delete error: {ex.Message}");
            }
        }

        /// <summary>
        /// Restarts the processing pipeline (chunking and embedding) for a document.
        /// Validates that the document belongs to the requesting user before processing.
        /// </summary>
        /// <param name="documentId">The ID of the document to retry.</param>
        /// <param name="userId">The ID of the user requesting the retry.</param>
        /// <returns>A Result indicating success or failure.</returns>
        public async Task<Result> RetryProcessingAsync(Guid documentId, Guid userId)
        {
            try
            {
                var document = await _documentRepository.GetByIdForUserAsync(documentId, userId);
                if (document == null)
                {
                    return Result.Failure("Document not found or access denied.");
                }

                // Enqueue the chunking job which will cascade to embedding
                var chunkingJobId = _backgroundJobs.Enqueue<IChunkingService>(x => x.ProcessFileChunkingAsync(documentId, CancellationToken.None));
                _backgroundJobs.ContinueJobWith<IEmbeddingService>(chunkingJobId, x => x.ProcessEmbeddingsAsync(documentId, CancellationToken.None));

                return Result.Success();
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Failed to retry processing for document {DocumentId}", documentId);
                return Result.Failure($"Retry processing error: {ex.Message}");
            }
        }

        private string ValidateUpload(DocumentUploadDto uploadDto)
        {
            if (uploadDto.UserId == Guid.Empty) return "User is not authenticated.";
            if (uploadDto.Content == Stream.Null) return "Please choose a file.";
            if (uploadDto.FileSize <= 0) return "File is empty.";
            if (uploadDto.FileSize > _uploadOptions.MaxFileSize) return $"File exceeds the limit of {_uploadOptions.MaxFileSize / (1024 * 1024)}MB.";

            var extension = Path.GetExtension(uploadDto.OriginalFileName);
            if (string.IsNullOrWhiteSpace(extension) || !_uploadOptions.AllowedMimeTypes.TryGetValue(extension, out var expectedMimeTypes))
            {
                return "This file type is not allowed for upload.";
            }

            var contentType = NormalizeContentType(uploadDto.ContentType);
            if (contentType != "application/octet-stream" && !expectedMimeTypes.Any(expected => string.Equals(expected, contentType, StringComparison.OrdinalIgnoreCase)))
            {
                return "File MIME type does not match the selected file extension.";
            }

            return string.Empty;
        }

        private static string BuildStoragePath(Guid userId, string storedFileName)
        {
            return $"{userId:N}/{DateTime.UtcNow:yyyy/MM}/{storedFileName}";
        }

        private static string NormalizeContentType(string contentType)
        {
            return string.IsNullOrWhiteSpace(contentType) ? "application/octet-stream" : contentType.Trim();
        }

        private static DocumentDto MapDocument(Document document)
        {
            return new DocumentDto
            {
                Id = document.Id,
                UserId = document.UserId,
                FolderId = document.FolderId,
                Title = document.Title,
                OriginalFileName = document.OriginalFileName,
                StoredFileName = document.StoredFileName,
                StoragePath = document.StoragePath,
                StorageUrl = document.StorageUrl,
                MimeType = document.MimeType,
                FileType = document.FileType,
                FileSize = document.FileSize,
                ProcessingStatus = (DocumentProcessingStatusDto)document.ProcessingStatus,
                UploadedAt = document.UploadedAt,
                CreatedAt = document.CreatedAt,
                UpdatedAt = document.UpdatedAt
            };
        }
    }
}
