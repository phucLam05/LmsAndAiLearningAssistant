using System;
using System.Collections.Generic;

namespace Core.Entities
{
    public class Document
    {
        public Guid Id { get; set; }
<<<<<<< Updated upstream
        public Guid? SubjectId { get; set; }
        public Guid? UploadedBy { get; set; }
        public string FileName { get; set; } = string.Empty;
        public string FileUrl { get; set; } = string.Empty;
        public DocumentStatus Status { get; set; } = DocumentStatus.Pending;
=======

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
        /// Unique filename used inside object storage to avoid collisions between uploaded files.
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
        /// Current workflow status. Automatically computed from ProcessingStatus.
        /// Ignored in EF Core database mapping.
        /// </summary>
        public string Status
        {
            get
            {
                return ProcessingStatus switch
                {
                    DocumentProcessingStatus.Uploaded => "uploaded",
                    DocumentProcessingStatus.Processing => "processing",
                    DocumentProcessingStatus.Indexed => "indexed",
                    DocumentProcessingStatus.Failed => "failed",
                    _ => "uploaded"
                };
            }
            set
            {
                ProcessingStatus = value?.ToLowerInvariant() switch
                {
                    "uploaded" => DocumentProcessingStatus.Uploaded,
                    "processing" => DocumentProcessingStatus.Processing,
                    "indexed" => DocumentProcessingStatus.Indexed,
                    "failed" => DocumentProcessingStatus.Failed,
                    _ => DocumentProcessingStatus.Uploaded
                };
            }
        }

        /// <summary>
        /// Current processing status of the document.
        /// </summary>
        public DocumentProcessingStatus ProcessingStatus { get; set; } = DocumentProcessingStatus.Uploaded;

        /// <summary>
        /// Date and time when the document was uploaded.
        /// </summary>
        public DateTime UploadedAt { get; set; } = DateTime.UtcNow;

        /// <summary>
        /// Date and time when the document metadata was created.
        /// </summary>
>>>>>>> Stashed changes
        public DateTime CreatedAt { get; set; } = DateTime.UtcNow;
        public DateTime UpdatedAt { get; set; } = DateTime.UtcNow;
        public Guid? UpdatedBy { get; set; }

<<<<<<< Updated upstream
        public Subject? Subject { get; set; }
        public User? Uploader { get; set; }
        public User? Updater { get; set; }
=======
        /// <summary>
        /// Optional identifier for the asynchronous chunking job.
        /// </summary>
        public string? ChunkingJobId { get; set; }

        /// <summary>
        /// Optional identifier for the asynchronous embedding job.
        /// </summary>
        public string? EmbeddingJobId { get; set; }

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
>>>>>>> Stashed changes
        public ICollection<DocumentChunk> Chunks { get; set; } = new List<DocumentChunk>();
    }
}
