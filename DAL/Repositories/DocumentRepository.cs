using Core.Entities;
using DAL.Data;
using DAL.Interfaces;
using Microsoft.EntityFrameworkCore;

namespace DAL.Repositories
{
    /// <summary>
    /// EF Core implementation for storing and querying uploaded document metadata.
    /// </summary>
    public class DocumentRepository : IDocumentRepository
    {
        private readonly ApplicationDbContext _context;

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
    }
}
