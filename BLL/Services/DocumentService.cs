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

        private static readonly Dictionary<string, string[]> AllowedMimeTypes = new(StringComparer.OrdinalIgnoreCase)
        {
            [".pdf"] = new[] { "application/pdf" },
            [".doc"] = new[] { "application/msword" },
            [".docx"] = new[] { "application/vnd.openxmlformats-officedocument.wordprocessingml.document" },
            [".ppt"] = new[] { "application/vnd.ms-powerpoint" },
            [".pptx"] = new[] { "application/vnd.openxmlformats-officedocument.presentationml.presentation" },
            [".xls"] = new[] { "application/vnd.ms-excel" },
            [".xlsx"] = new[] { "application/vnd.openxmlformats-officedocument.spreadsheetml.sheet" },
            [".txt"] = new[] { "text/plain" },
            [".csv"] = new[] { "text/csv", "application/csv", "application/vnd.ms-excel" },
            [".md"] = new[] { "text/markdown", "text/plain" },
            [".rtf"] = new[] { "application/rtf", "text/rtf" },
            [".json"] = new[] { "application/json", "text/json" },
            [".xml"] = new[] { "application/xml", "text/xml" },
            [".jpg"] = new[] { "image/jpeg" },
            [".jpeg"] = new[] { "image/jpeg" },
            [".png"] = new[] { "image/png" },
            [".gif"] = new[] { "image/gif" },
            [".webp"] = new[] { "image/webp" },
            [".bmp"] = new[] { "image/bmp", "image/x-ms-bmp" },
            [".svg"] = new[] { "image/svg+xml" },
            [".mp3"] = new[] { "audio/mpeg" },
            [".wav"] = new[] { "audio/wav", "audio/x-wav" },
            [".mp4"] = new[] { "video/mp4" },
            [".mov"] = new[] { "video/quicktime" },
            [".avi"] = new[] { "video/x-msvideo" },
            [".mkv"] = new[] { "video/x-matroska" },
            [".webm"] = new[] { "video/webm" },
            [".zip"] = new[] { "application/zip", "application/x-zip-compressed", "multipart/x-zip" },
            [".rar"] = new[] { "application/vnd.rar", "application/x-rar-compressed", "application/octet-stream" },
            [".7z"] = new[] { "application/x-7z-compressed", "application/octet-stream" },
            [".tar"] = new[] { "application/x-tar", "application/octet-stream" },
            [".gz"] = new[] { "application/gzip", "application/x-gzip", "application/octet-stream" }
        };

        private readonly IDocumentRepository _documentRepository;
        private readonly IFolderRepository _folderRepository;
        private readonly ISupabaseStorageService _storageService;

        /// <summary>
        /// Creates a document service with repositories for metadata/folders and storage for file objects.
        /// </summary>
        public DocumentService(
            IDocumentRepository documentRepository,
            IFolderRepository folderRepository,
            ISupabaseStorageService storageService)
        {
            _documentRepository = documentRepository;
            _folderRepository = folderRepository;
            _storageService = storageService;
        }

        /// <summary>
        /// Returns only documents owned by the current user.
        /// </summary>
        public async Task<IReadOnlyList<DocumentDto>> GetDocumentsForUserAsync(Guid userId)
        {
            var documents = await _documentRepository.GetByUserIdAsync(userId);
            return documents.Select(MapDocument).ToList();
        }

        /// <summary>
        /// Validates, uploads a file to Supabase Storage, and saves metadata for the current user.
        /// </summary>
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
                await _storageService.UploadAsync(storagePath, uploadDto.Content, NormalizeContentType(uploadDto.ContentType));
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
                    MimeType = NormalizeContentType(uploadDto.ContentType),
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

        /// <summary>
        /// Deletes a user's document from Supabase Storage and then removes its metadata row.
        /// </summary>
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

        /// <summary>
        /// Checks ownership input, file size, extension, and browser-provided MIME type before upload.
        /// </summary>
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
            if (string.IsNullOrWhiteSpace(extension) || !AllowedMimeTypes.TryGetValue(extension, out var expectedMimeTypes))
            {
                return "This file type is not allowed for upload.";
            }

            var contentType = NormalizeContentType(uploadDto.ContentType);
            if (contentType == "application/octet-stream")
            {
                return string.Empty;
            }

            if (!expectedMimeTypes.Any(expected => string.Equals(expected, contentType, StringComparison.OrdinalIgnoreCase)))
            {
                return "File MIME type does not match the selected file extension.";
            }

            return string.Empty;
        }

        /// <summary>
        /// Builds a stable private object path grouped by user and month for Supabase Storage.
        /// </summary>
        private static string BuildStoragePath(Guid userId, string storedFileName)
        {
            return $"{userId:N}/{DateTime.UtcNow:yyyy/MM}/{storedFileName}";
        }

        /// <summary>
        /// Falls back to application/octet-stream when a browser does not provide a usable MIME type.
        /// </summary>
        private static string NormalizeContentType(string contentType)
        {
            return string.IsNullOrWhiteSpace(contentType) ? "application/octet-stream" : contentType.Trim();
        }

        /// <summary>
        /// Converts the entity stored in DAL into the DTO used by MVC views.
        /// </summary>
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
