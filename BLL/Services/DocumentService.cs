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
        private readonly ISubjectRepository _subjectRepository;
        private readonly IDocumentChunkRepository _documentChunkRepository;
        private readonly ISupabaseStorageProvider _storageService;
        private readonly UploadOptions _uploadOptions;
        private readonly ILogger<DocumentService> _logger;
        private readonly IBackgroundJobClient _backgroundJobs;
        private readonly string _supabaseUrl;
        private readonly string _bucket;

        public DocumentService(
            IDocumentRepository documentRepository,
            ISubjectRepository subjectRepository,
            IDocumentChunkRepository documentChunkRepository,
            ISupabaseStorageProvider storageService,
            IOptions<UploadOptions> uploadOptions,
            ILogger<DocumentService> logger,
            IBackgroundJobClient backgroundJobs,
            Microsoft.Extensions.Configuration.IConfiguration configuration)
        {
            _documentRepository = documentRepository;
            _subjectRepository = subjectRepository;
            _documentChunkRepository = documentChunkRepository;
            _storageService = storageService;
            _uploadOptions = uploadOptions.Value;
            _logger = logger;
            _backgroundJobs = backgroundJobs;

            var supabaseUrl = configuration["Supabase:Url"] ?? "";
            if (Uri.TryCreate(supabaseUrl, UriKind.Absolute, out var uri))
            {
                _supabaseUrl = $"{uri.Scheme}://{uri.Host}";
                if (uri.Port != 80 && uri.Port != 443)
                {
                    _supabaseUrl += $":{uri.Port}";
                }
            }
            else
            {
                _supabaseUrl = supabaseUrl.TrimEnd('/');
            }
            _bucket = configuration["Supabase:Bucket"] ?? "Document";
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
                var subject = await _subjectRepository.GetByIdAsync(uploadDto.SubjectId);
                if (subject == null)
                {
                    return Result<DocumentDto>.Failure("Môn học không tồn tại.");
                }

                if (subject.LecturerId != uploadDto.UserId)
                {
                    return Result<DocumentDto>.Failure("Bạn không có quyền upload tài liệu cho môn học này.");
                }

                var document = new Document
                {
                    Id = Guid.NewGuid(),
                    SubjectId = subject.Id,
                    UploadedBy = uploadDto.UserId,
                    FileName = Path.GetFileName(uploadDto.OriginalFileName),
                    FileUrl = storagePath,
                    Status = DocumentStatus.Pending,
                    CreatedAt = now,
                    UpdatedAt = now,
                    UpdatedBy = uploadDto.UserId
                };

                await _documentRepository.AddAsync(document);

                // Enqueue the chunking job, followed by the embedding job
                var chunkingJobId = _backgroundJobs.Enqueue<IChunkingService>(x => x.ProcessFileChunkingAsync(document.Id, CancellationToken.None));
                var embeddingJobId = _backgroundJobs.ContinueJobWith<IEmbeddingService>(chunkingJobId, x => x.ProcessEmbeddingsAsync(document.Id, CancellationToken.None));

                // Normally we would save the JobIds if the entity supported it, but we can just fire-and-forget for now.
                
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
                    await _storageService.DeleteAsync(document.FileUrl);
                }
                catch (Exception ex)
                {
                    _logger.LogWarning(ex, "Failed to delete file from Supabase storage. Path: {StoragePath}", document.FileUrl);
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

                bool hasChunks = await _documentChunkRepository.HasChunksAsync(documentId);
                if (hasChunks)
                {
                    document.Status = DocumentStatus.Processing;
                    var embeddingJobId = _backgroundJobs.Enqueue<IEmbeddingService>(x => x.ProcessEmbeddingsAsync(documentId, CancellationToken.None));
                }
                else
                {
                    document.Status = DocumentStatus.Pending;
                    var chunkingJobId = _backgroundJobs.Enqueue<IChunkingService>(x => x.ProcessFileChunkingAsync(documentId, CancellationToken.None));
                    var embeddingJobId = _backgroundJobs.ContinueJobWith<IEmbeddingService>(chunkingJobId, x => x.ProcessEmbeddingsAsync(documentId, CancellationToken.None));
                }

                await _documentRepository.UpdateAsync(document);

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

        private string GetAbsoluteStorageUrl(string url)
        {
            if (string.IsNullOrEmpty(url)) return string.Empty;
            if (url.StartsWith("http://") || url.StartsWith("https://"))
                return url;

            return $"{_supabaseUrl}/storage/v1/object/public/{_bucket}/{url.TrimStart('/')}";
        }

        private DocumentDto MapDocument(Document document)
        {
            return new DocumentDto
            {
                Id = document.Id,
                UserId = document.UploadedBy ?? Guid.Empty,
                FolderId = document.SubjectId ?? Guid.Empty, // DocumentDto.FolderId is Guid
                Title = document.FileName,
                OriginalFileName = document.FileName,
                StoredFileName = document.FileName,
                StoragePath = document.FileUrl,
                StorageUrl = GetAbsoluteStorageUrl(document.FileUrl),
                MimeType = "application/octet-stream",
                FileType = Path.GetExtension(document.FileName),
                FileSize = 0, // Not stored anymore
                ProcessingStatus = (DocumentProcessingStatusDto)document.Status,
                UploadedAt = document.CreatedAt,
                CreatedAt = document.CreatedAt,
                UpdatedAt = document.UpdatedAt
            };
        }
    }
}
