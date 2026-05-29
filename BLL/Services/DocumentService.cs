using BLL.Interfaces;
using Core.Constants;
using Core.DTOs.Documents;
using Core.Entities;
using DAL.Interfaces;

namespace BLL.Services
{
    /// <summary>
    /// Handles document upload business rules, Supabase storage coordination, and metadata persistence.
    /// </summary>
    public class DocumentService : IDocumentService
    {
        private const long MaxFileSize = 50L * 1024L * 1024L;

        private static readonly Dictionary<string, string> AllowedMimeTypes = new(StringComparer.OrdinalIgnoreCase)
        {
            [".pdf"] = "application/pdf",
            [".docx"] = "application/vnd.openxmlformats-officedocument.wordprocessingml.document",
            [".pptx"] = "application/vnd.openxmlformats-officedocument.presentationml.presentation"
        };

        private readonly IDocumentRepository _documentRepository;
        private readonly IFolderRepository _folderRepository;
        private readonly ISupabaseStorageService _storageService;

        public DocumentService(
            IDocumentRepository documentRepository,
            IFolderRepository folderRepository,
            ISupabaseStorageService storageService)
        {
            _documentRepository = documentRepository;
            _folderRepository = folderRepository;
            _storageService = storageService;
        }

        public async Task<IReadOnlyList<DocumentDto>> GetDocumentsForUserAsync(Guid userId)
        {
            var documents = await _documentRepository.GetByUserIdAsync(userId);
            return documents.Select(MapDocument).ToList();
        }

        public async Task<(bool Success, DocumentDto? Document, string ErrorMessage)> UploadAsync(DocumentUploadDto uploadDto)
        {
            var validationError = ValidateUpload(uploadDto);
            if (!string.IsNullOrEmpty(validationError))
            {
                return (false, null, validationError);
            }

            var extension = Path.GetExtension(uploadDto.OriginalFileName).ToLowerInvariant();
            var storedFileName = $"{Guid.NewGuid():N}{extension}";
            var storagePath = BuildStoragePath(uploadDto.UserId, storedFileName);
            var now = DateTime.UtcNow;

            try
            {
                await _storageService.UploadAsync(storagePath, uploadDto.Content, uploadDto.ContentType);
            }
            catch (Exception ex)
            {
                return (false, null, $"Supabase upload error: {ex.Message}");
            }

            try
            {
                var folder = await _folderRepository.GetOrCreateDefaultUploadFolderAsync(uploadDto.UserId);
                var document = new Document
                {
                    Id = Guid.NewGuid(),
                    UserId = uploadDto.UserId,
                    FolderId = folder.Id,
                    Title = Path.GetFileNameWithoutExtension(uploadDto.OriginalFileName),
                    OriginalFileName = Path.GetFileName(uploadDto.OriginalFileName),
                    StoredFileName = storedFileName,
                    StoragePath = storagePath,
                    StorageUrl = storagePath,
                    MimeType = uploadDto.ContentType,
                    FileType = extension.TrimStart('.'),
                    FileSize = uploadDto.FileSize,
                    Status = DocumentStatuses.Uploaded,
                    ProcessingStatus = DocumentProcessingStatus.Uploaded,
                    UploadedAt = now,
                    CreatedAt = now,
                    UpdatedAt = now
                };

                await _documentRepository.AddAsync(document);
                return (true, MapDocument(document), string.Empty);
            }
            catch (Exception ex)
            {
                // If metadata cannot be saved after storage upload, remove the object to avoid orphan files.
                try
                {
                    await _storageService.DeleteAsync(storagePath);
                }
                catch
                {
                    // Keep the original database error visible to the UI; storage cleanup can be retried manually.
                }

                return (false, null, $"Database save error: {ex.Message}");
            }
        }

        public async Task<(bool Success, string ErrorMessage)> DeleteAsync(Guid documentId, Guid userId)
        {
            var document = await _documentRepository.GetByIdForUserAsync(documentId, userId);
            if (document == null)
            {
                return (false, "Document not found or access denied.");
            }

            try
            {
                await _storageService.DeleteAsync(document.StoragePath);
                await _documentRepository.DeleteAsync(document);
                return (true, string.Empty);
            }
            catch (Exception ex)
            {
                return (false, $"Delete error: {ex.Message}");
            }
        }

        private static string ValidateUpload(DocumentUploadDto uploadDto)
        {
            if (uploadDto.UserId == Guid.Empty)
            {
                return "User is not authenticated.";
            }

            if (uploadDto.Content == Stream.Null)
            {
                return "Please choose a file.";
            }

            if (uploadDto.FileSize <= 0)
            {
                return "File is empty.";
            }

            if (uploadDto.FileSize > MaxFileSize)
            {
                return "File exceeds the 50MB limit.";
            }

            var extension = Path.GetExtension(uploadDto.OriginalFileName);
            if (string.IsNullOrWhiteSpace(extension) || !AllowedMimeTypes.TryGetValue(extension, out var expectedMimeType))
            {
                return "Only PDF, DOCX and PPTX files are allowed.";
            }

            // Extension and browser-provided MIME type must agree before the file is sent to Supabase.
            if (!string.Equals(uploadDto.ContentType, expectedMimeType, StringComparison.OrdinalIgnoreCase))
            {
                return "File MIME type does not match the selected document type.";
            }

            return string.Empty;
        }

        private static string BuildStoragePath(Guid userId, string storedFileName)
        {
            return $"{userId:N}/{DateTime.UtcNow:yyyy/MM}/{storedFileName}";
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
                Status = document.Status,
                ProcessingStatus = (DocumentProcessingStatusDto)document.ProcessingStatus,
                UploadedAt = document.UploadedAt,
                CreatedAt = document.CreatedAt,
                UpdatedAt = document.UpdatedAt
            };
        }
    }
}
