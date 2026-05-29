using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Threading.Tasks;
using BLL.Interfaces;
using Core.Configuration;
using Core.DTOs.Documents;
using Core.Entities;
using DAL.Interfaces;
using Microsoft.Extensions.Options;

namespace BLL.Services
{
    /// <summary>
    /// Handles document upload business rules, Supabase storage coordination, and metadata persistence.
    /// </summary>
    public class DocumentService : IDocumentService
    {
        private readonly IDocumentRepository _documentRepository;
        private readonly IFolderRepository _folderRepository;
        private readonly ISupabaseStorageProvider _storageService;
        private readonly UploadOptions _uploadOptions;

        /// <summary>
        /// Creates a document service with repositories for metadata/folders, storage for file objects, and upload configurations.
        /// </summary>
        public DocumentService(
            IDocumentRepository documentRepository,
            IFolderRepository folderRepository,
            ISupabaseStorageProvider storageService,
            IOptions<UploadOptions> uploadOptions)
        {
            _documentRepository = documentRepository;
            _folderRepository = folderRepository;
            _storageService = storageService;
            _uploadOptions = uploadOptions.Value;
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
        /// Checks ownership input, file size, extension, and browser-provided MIME type before upload
        /// against values loaded from application configuration.
        /// </summary>
        private string ValidateUpload(DocumentUploadDto uploadDto)
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

            if (uploadDto.FileSize > _uploadOptions.MaxFileSize)
            {
                var maxSizeMb = _uploadOptions.MaxFileSize / (1024 * 1024);
                return $"File exceeds the {maxSizeMb}MB limit.";
            }

            var extension = Path.GetExtension(uploadDto.OriginalFileName);
            if (string.IsNullOrWhiteSpace(extension) || !_uploadOptions.AllowedMimeTypes.TryGetValue(extension, out var expectedMimeTypes))
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
                ProcessingStatus = (DocumentProcessingStatusDto)document.ProcessingStatus,
                UploadedAt = document.UploadedAt,
                CreatedAt = document.CreatedAt,
                UpdatedAt = document.UpdatedAt
            };
        }
    }
}
