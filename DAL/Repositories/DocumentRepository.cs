using Core.Entities;
using DAL.Data;
using DAL.Interfaces;
using Microsoft.EntityFrameworkCore;
using System;
using System.Threading.Tasks;

namespace DAL.Repositories
{
    public class DocumentRepository : IDocumentRepository
    {
        private readonly ApplicationDbContext _context;

        public DocumentRepository(ApplicationDbContext context)
        {
            _context = context;
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
        /// Adds a new document to the database.
        /// </summary>
        public async Task<Document> AddAsync(Document document)
        {
            await _context.Documents.AddAsync(document);
            await _context.SaveChangesAsync();
            return document;
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

        /// <summary>
        /// Deletes a document from the database.
        /// </summary>
        public async Task DeleteAsync(Document document)
        {
            _context.Documents.Remove(document);
            await _context.SaveChangesAsync();
        }
    }
}
