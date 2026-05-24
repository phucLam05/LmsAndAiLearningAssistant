using System;
using System.Collections.Generic;

namespace Core.Entities
{
    /// <summary>
    /// Represents a user-uploaded document and its metadata.
    /// </summary>
    public class Document
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
        /// Original filename of the uploaded document.
        /// </summary>
        public string OriginalFileName { get; set; } = string.Empty;

        /// <summary>
        /// Storage URL of the document content.
        /// </summary>
        public string StorageUrl { get; set; } = string.Empty;

        /// <summary>
        /// MIME type of the document content.
        /// </summary>
        public string MimeType { get; set; } = string.Empty;

        /// <summary>
        /// Size of the file in bytes.
        /// </summary>
        public long FileSize { get; set; }

        /// <summary>
        /// Current processing status of the document.
        /// </summary>
        public DocumentProcessingStatus ProcessingStatus { get; set; } = DocumentProcessingStatus.Pending;

        /// <summary>
        /// Date and time when the document was uploaded.
        /// </summary>
        public DateTime UploadedAt { get; set; } = DateTime.UtcNow;

        /// <summary>
        /// Date and time when the document was last updated.
        /// </summary>
        public DateTime UpdatedAt { get; set; } = DateTime.UtcNow;

        /// <summary>
        /// Navigation to the owning user.
        /// </summary>
        public User User { get; set; } = null!;

        /// <summary>
        /// Navigation to the containing folder.
        /// </summary>
        public Folder Folder { get; set; } = null!;

        /// <summary>
        /// Navigation to tag mappings.
        /// </summary>
        public ICollection<DocumentTagMapping> TagMappings { get; set; } = new List<DocumentTagMapping>();

        /// <summary>
        /// Navigation to document chunks.
        /// </summary>
        public ICollection<DocumentChunk> Chunks { get; set; } = new List<DocumentChunk>();
    }

    /// <summary>
    /// Defines processing states for a document.
    /// </summary>
    public enum DocumentProcessingStatus
    {
        Pending = 0,
        Processing = 1,
        Indexed = 2,
        Failed = 3
    }
}
