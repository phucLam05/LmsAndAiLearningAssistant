using System;
using System.Collections.Generic;
using System.Threading.Tasks;
using Core.Entities;
using DAL.Data;
using DAL.Interfaces;

namespace DAL.Repositories
{
    /// <summary>
    /// Implementation of the IDocumentRepository for PostgreSQL database interactions.
    /// </summary>
    public class DocumentRepository : IDocumentRepository
    {
        private readonly ApplicationDbContext _context;

        /// <summary>
        /// Initializes a new instance of the DocumentRepository.
        /// </summary>
        /// <param name="context">The application database context.</param>
        public DocumentRepository(ApplicationDbContext context)
        {
            _context = context;
        }

        /// <summary>
        /// Retrieves a document by its unique identifier.
        /// </summary>
        /// <param name="id">The unique identifier of the document.</param>
        /// <returns>The Document if found; otherwise, null.</returns>
        public async Task<Document?> GetByIdAsync(Guid id)
        {
            return await _context.Documents.FindAsync(id);
        }

        /// <summary>
        /// Updates the processing status of a specific document.
        /// </summary>
        /// <param name="id">The unique identifier of the document.</param>
        /// <param name="status">The new processing status.</param>
        /// <returns>A task that represents the asynchronous operation.</returns>
        public async Task UpdateStatusAsync(Guid id, DocumentProcessingStatus status)
        {
            var document = await _context.Documents.FindAsync(id);
            if (document != null)
            {
                document.ProcessingStatus = status;
                document.UpdatedAt = DateTime.UtcNow;
                await _context.SaveChangesAsync();
            }
        }

        /// <summary>
        /// Performs a bulk insert of document chunks into the database.
        /// </summary>
        /// <param name="chunks">The collection of document chunks to insert.</param>
        /// <returns>A task that represents the asynchronous operation.</returns>
        public async Task BulkInsertChunksAsync(IEnumerable<DocumentChunk> chunks)
        {
            await _context.DocumentChunks.AddRangeAsync(chunks);
            await _context.SaveChangesAsync();
        }
    }
}
