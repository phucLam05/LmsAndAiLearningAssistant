using System;
using System.Collections.Generic;
using System.Threading.Tasks;
using Core.Entities;

namespace DAL.Interfaces
{
    /// <summary>
    /// Repository interface for managing Document entities and related data.
    /// </summary>
    public interface IDocumentRepository
    {
        /// <summary>
        /// Retrieves a document by its unique identifier.
        /// </summary>
        /// <param name="id">The unique identifier of the document.</param>
        /// <returns>The Document if found; otherwise, null.</returns>
        Task<Document?> GetByIdAsync(Guid id);

        /// <summary>
        /// Updates the processing status of a specific document.
        /// </summary>
        /// <param name="id">The unique identifier of the document.</param>
        /// <param name="status">The new processing status.</param>
        /// <returns>A task that represents the asynchronous operation.</returns>
        Task UpdateStatusAsync(Guid id, DocumentProcessingStatus status);

        /// <summary>
        /// Performs a bulk insert of document chunks into the database.
        /// </summary>
        /// <param name="chunks">The collection of document chunks to insert.</param>
        /// <returns>A task that represents the asynchronous operation.</returns>
        Task BulkInsertChunksAsync(IEnumerable<DocumentChunk> chunks);
    }
}
