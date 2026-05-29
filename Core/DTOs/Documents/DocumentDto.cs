using System;

namespace Core.DTOs.Documents
{
    /// <summary>
    /// Data Transfer Object for document metadata.
    /// </summary>
    public class DocumentDto
    {
        /// <summary>
        /// Unique identifier for the document.
        /// </summary>
        public Guid Id { get; set; }

        /// <summary>
        /// Identifier of the user who owns the document.
        /// </summary>
        public Guid UserId { get; set; }

        /// <summary>
        /// Identifier of the folder containing the document.
        /// </summary>
        public Guid FolderId { get; set; }

        /// <summary>
        /// Display title of the document.
        /// </summary>
        public string Title { get; set; } = string.Empty;

        /// <summary>
        /// Original file name of the uploaded document.
        /// </summary>
        public string OriginalFileName { get; set; } = string.Empty;

        /// <summary>
        /// Unique filename used by object storage.
        /// </summary>
        public string StoredFileName { get; set; } = string.Empty;

        /// <summary>
        /// Private object path inside Supabase Storage.
        /// </summary>
        public string StoragePath { get; set; } = string.Empty;

        /// <summary>
        /// Storage URL or object path of the document content.
        /// </summary>
        public string StorageUrl { get; set; } = string.Empty;

        /// <summary>
        /// MIME type of the document content.
        /// </summary>
        public string MimeType { get; set; } = string.Empty;

        /// <summary>
        /// Normalized file extension without the leading dot.
        /// </summary>
        public string FileType { get; set; } = string.Empty;

        /// <summary>
        /// Size of the file in bytes.
        /// </summary>
        public long FileSize { get; set; }

        /// <summary>
        /// Current processing status of the document.
        /// </summary>
        public DocumentProcessingStatusDto ProcessingStatus { get; set; }

        /// <summary>
        /// Date and time when the document was uploaded.
        /// </summary>
        public DateTime UploadedAt { get; set; }

        /// <summary>
        /// Date and time when the document metadata was created.
        /// </summary>
        public DateTime CreatedAt { get; set; }

        /// <summary>
        /// Date and time when the document was last updated.
        /// </summary>
        public DateTime UpdatedAt { get; set; }
    }

    /// <summary>
    /// Defines processing states for a document.
    /// </summary>
    public enum DocumentProcessingStatusDto
    {
        Uploaded = 0,
        Chunking = 1,
        Chunked = 2,
        Embedding = 3,
        Indexed = 4,
        Failed = 5
    }
}
