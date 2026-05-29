using Core.Entities;
using DAL.Data;
using DAL.Interfaces;
using Microsoft.EntityFrameworkCore;

namespace DAL.Repositories
{
    /// <summary>
    /// EF Core implementation for the minimal folder behavior required by upload metadata.
    /// </summary>
    public class FolderRepository : IFolderRepository
    {
        private const string DefaultUploadFolderName = "Uploaded Documents";

        private readonly ApplicationDbContext _context;

        public FolderRepository(ApplicationDbContext context)
        {
            _context = context;
        }

        public async Task<Folder> GetOrCreateDefaultUploadFolderAsync(Guid userId)
        {
            var folder = await _context.Folders
                .FirstOrDefaultAsync(item => item.UserId == userId && item.Name == DefaultUploadFolderName && item.ParentFolderId == null);

            if (folder != null)
            {
                return folder;
            }

            folder = new Folder
            {
                Id = Guid.NewGuid(),
                UserId = userId,
                Name = DefaultUploadFolderName,
                Icon = "file",
                Color = "#0d6efd",
                CreatedAt = DateTime.UtcNow,
                UpdatedAt = DateTime.UtcNow
            };

            await _context.Folders.AddAsync(folder);
            await _context.SaveChangesAsync();
            return folder;
        }
    }
}
