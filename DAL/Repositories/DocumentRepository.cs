using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading.Tasks;
using Core.Entities;
using DAL.Data;
using DAL.Interfaces;
using Microsoft.EntityFrameworkCore;

namespace DAL.Repositories
{
    /// <summary>
    /// EF Core implementation for storing and querying uploaded document metadata.
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

        public async Task<IReadOnlyList<Document>> GetByUserIdAsync(Guid userId)
        {
            return await _context.Documents
                .AsNoTracking()
                .Where(document => document.UserId == userId)
                .OrderByDescending(document => document.CreatedAt)
                .ToListAsync();
        }

        public async Task<Document?> GetByIdForUserAsync(Guid documentId, Guid userId)
        {
            return await _context.Documents
                .FirstOrDefaultAsync(document => document.Id == documentId && document.UserId == userId);
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

        public async Task<Document> AddAsync(Document document)
        {
            await _context.Documents.AddAsync(document);
            await _context.SaveChangesAsync();
            return document;
        }

        public async Task DeleteAsync(Document document)
        {
            _context.Documents.Remove(document);
            await _context.SaveChangesAsync();
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

        public async Task<Document?> GetByIdWithOwnerAsync(Guid id, Guid userId)
        {
            return await _context.Documents
                .FirstOrDefaultAsync(d => d.Id == id && d.UserId == userId);
        }

        /// <summary>
        /// Retrieves all documents belonging to the specified user, including their folder and parent folder details.
        /// </summary>
        public async Task<System.Collections.Generic.List<Document>> GetAllWithOwnerAsync(Guid userId)
        {
            return await _context.Documents
                .Include(d => d.Folder)
                .ThenInclude(f => f.ParentFolder)
                .Where(d => d.UserId == userId)
                .OrderByDescending(d => d.UploadedAt)
                .ToListAsync();
        }

        /// <summary>
        /// Updates an existing document in the database.
        /// </summary>
        public async Task UpdateAsync(Document document)
        {
            document.UpdatedAt = DateTime.UtcNow;
            _context.Documents.Update(document);
            await _context.SaveChangesAsync();
        }

        public void ClearTracker()
        {
            _context.ChangeTracker.Clear();
        }
    }
}
