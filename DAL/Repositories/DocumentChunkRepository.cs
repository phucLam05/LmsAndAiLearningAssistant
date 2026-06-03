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
    /// EF Core implementation for storing and querying document chunks.
    /// </summary>
    public class DocumentChunkRepository : IDocumentChunkRepository
    {
        private readonly ApplicationDbContext _context;

        public DocumentChunkRepository(ApplicationDbContext context)
        {
            _context = context;
        }

        public async Task BulkInsertChunksAsync(IEnumerable<DocumentChunk> chunks)
        {
            await _context.DocumentChunks.AddRangeAsync(chunks);
            await _context.SaveChangesAsync();
        }

        public async Task<IReadOnlyList<DocumentChunk>> GetChunksByDocumentIdAsync(Guid documentId)
        {
            return await _context.DocumentChunks
                .Where(c => c.DocumentId == documentId)
                .OrderBy(c => c.ChunkIndex)
                .ToListAsync();
        }

        public async Task DeleteChunksByDocumentIdAsync(Guid documentId)
        {
            var chunks = await _context.DocumentChunks.Where(c => c.DocumentId == documentId).ToListAsync();
            if (chunks.Any())
            {
                _context.DocumentChunks.RemoveRange(chunks);
                await _context.SaveChangesAsync();
            }
        }

        public async Task UpdateChunksAsync(IEnumerable<DocumentChunk> chunks)
        {
            _context.DocumentChunks.UpdateRange(chunks);
            await _context.SaveChangesAsync();
        }

        public async Task<bool> HasChunksAsync(Guid documentId)
        {
            return await _context.DocumentChunks.AnyAsync(c => c.DocumentId == documentId);
        }
    }
}
