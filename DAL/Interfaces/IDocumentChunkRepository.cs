using System;
using System.Collections.Generic;
using System.Threading.Tasks;
using Core.Entities;

namespace DAL.Interfaces
{
    /// <summary>
    /// Defines repository operations for document chunks.
    /// </summary>
    public interface IDocumentChunkRepository
    {
        /// <summary>
        /// Performs a bulk insert of document chunks into the database.
        /// </summary>
        Task BulkInsertChunksAsync(IEnumerable<DocumentChunk> chunks);

        /// <summary>
        /// Retrieves all chunks for a specific document.
        /// </summary>
        Task<IReadOnlyList<DocumentChunk>> GetChunksByDocumentIdAsync(Guid documentId);

        /// <summary>
        /// Deletes all chunks for a specific document (useful for retries).
        /// </summary>
        Task DeleteChunksByDocumentIdAsync(Guid documentId);

        /// <summary>
        /// Updates multiple chunks, typically to save their generated embeddings.
        /// </summary>
        Task UpdateChunksAsync(IEnumerable<DocumentChunk> chunks);

        /// <summary>
        /// Checks if a document already has chunks.
        /// </summary>
        Task<bool> HasChunksAsync(Guid documentId);
    }
}
