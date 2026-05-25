using System;
using Pgvector;

namespace Core.Entities
{
    /// <summary>
    /// Represents a chunk of a document used for indexing and embeddings.
    /// </summary>
    public class DocumentChunk
    {
        /// <summary>
        /// Unique identifier for the chunk.
        /// </summary>
        public Guid Id { get; set; }

        /// <summary>
        /// Identifier of the parent document.
        /// </summary>
        public Guid DocumentId { get; set; }

        /// <summary>
        /// Zero-based index of the chunk within the document.
        /// </summary>
        public int ChunkIndex { get; set; }

        /// <summary>
        /// Text content of the chunk.
        /// </summary>
        public string Content { get; set; } = string.Empty;

        /// <summary>
        /// Token count of the chunk content.
        /// </summary>
        public int TokenCount { get; set; }

        /// <summary>
        /// Optional page number the chunk originated from.
        /// </summary>
        public int? PageNumber { get; set; }

        /// <summary>
        /// Embedding vector for similarity search.
        /// </summary>
        public Vector? Embedding { get; set; }

        /// <summary>
        /// Date and time when the chunk was created.
        /// </summary>
        public DateTime CreatedAt { get; set; } = DateTime.UtcNow;

        /// <summary>
        /// Navigation to the parent document.
        /// </summary>
        public Document Document { get; set; } = null!;
    }
}
