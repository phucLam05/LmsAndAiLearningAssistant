using Core.Entities;
using DAL.Data;
using DAL.Interfaces;
using Microsoft.EntityFrameworkCore;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading.Tasks;

namespace DAL.Repositories
{
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

        /// <summary>
        /// Retrieves a specific folder by its ID and ensures it belongs to the specified user.
        /// </summary>
        public async Task<Folder?> GetByIdWithOwnerAsync(Guid id, Guid userId)
        {
            return await _context.Folders
                .FirstOrDefaultAsync(f => f.Id == id && f.UserId == userId);
        }

        /// <summary>
        /// Gets the root folders and documents if folderId is null, 
        /// otherwise gets the subfolders and documents of the specified folder.
        /// </summary>
        public async Task<(List<Folder> Folders, List<Document> Documents)> GetFolderContentsAsync(Guid? folderId, Guid userId)
        {
            var foldersQuery = _context.Folders.Where(f => f.UserId == userId && f.ParentFolderId == folderId)
                                               .OrderBy(f => f.Name);
            var documentsQuery = _context.Documents.Where(d => d.UserId == userId && (folderId.HasValue ? d.FolderId == folderId.Value : false))
                                                   .OrderBy(d => d.Title);
            
            // Note: If folderId is null, there are NO root documents because Document.FolderId is required (Guid, not Guid?).
            // Documents must belong to a folder in this schema.

            var folders = await foldersQuery.ToListAsync();
            var documents = await documentsQuery.ToListAsync();

            return (folders, documents);
        }

        /// <summary>
        /// Gets a list of folders representing the breadcrumb trail from the root down to the specified folder.
        /// </summary>
        public async Task<List<Folder>> GetBreadcrumbsAsync(Guid folderId, Guid userId)
        {
            var breadcrumbs = new List<Folder>();
            var currentFolder = await _context.Folders.FirstOrDefaultAsync(f => f.Id == folderId && f.UserId == userId);
            
            while (currentFolder != null)
            {
                breadcrumbs.Insert(0, currentFolder); // Prepend to keep root -> current order
                if (currentFolder.ParentFolderId.HasValue)
                {
                    currentFolder = await _context.Folders.FirstOrDefaultAsync(f => f.Id == currentFolder.ParentFolderId.Value && f.UserId == userId);
                }
                else
                {
                    currentFolder = null;
                }
            }

            return breadcrumbs;
        }

        /// <summary>
        /// Checks if a given folder is a subfolder of another folder.
        /// </summary>
        public async Task<bool> IsSubfolderOfAsync(Guid parentId, Guid childId)
        {
            if (parentId == childId) return true;

            var current = await _context.Folders.FirstOrDefaultAsync(f => f.Id == childId);
            while (current != null && current.ParentFolderId.HasValue)
            {
                if (current.ParentFolderId.Value == parentId)
                    return true;
                
                current = await _context.Folders.FirstOrDefaultAsync(f => f.Id == current.ParentFolderId.Value);
            }

            return false;
        }

        /// <summary>
        /// Gets the document counts for a list of folder IDs (to display "X files" badges).
        /// </summary>
        public async Task<Dictionary<Guid, int>> GetDocumentCountsForFoldersAsync(IEnumerable<Guid> folderIds, Guid userId)
        {
            var ids = folderIds.ToList();
            var counts = await _context.Documents
                .Where(d => d.UserId == userId && ids.Contains(d.FolderId))
                .GroupBy(d => d.FolderId)
                .Select(g => new { FolderId = g.Key, Count = g.Count() })
                .ToListAsync();

            return counts.ToDictionary(x => x.FolderId, x => x.Count);
        }

        /// <summary>
        /// Gets all child folders whose ParentFolderId is in the given set of parent IDs.
        /// </summary>
        public async Task<List<Folder>> GetChildFoldersAsync(IEnumerable<Guid> parentIds, Guid userId)
        {
            var ids = parentIds.ToList();
            return await _context.Folders
                .Where(f => f.UserId == userId && f.ParentFolderId.HasValue && ids.Contains(f.ParentFolderId.Value))
                .OrderBy(f => f.Name)
                .ToListAsync();
        }

        /// <summary>
        /// Adds a new folder to the database.
        /// </summary>
        public async Task<Folder> AddAsync(Folder folder)
        {
            await _context.Folders.AddAsync(folder);
            await _context.SaveChangesAsync();
            return folder;
        }

        /// <summary>
        /// Updates an existing folder in the database.
        /// </summary>
        public async Task UpdateAsync(Folder folder)
        {
            folder.UpdatedAt = DateTime.UtcNow;
            _context.Folders.Update(folder);
            await _context.SaveChangesAsync();
        }

        /// <summary>
        /// Deletes a folder from the database.
        /// </summary>
        public async Task DeleteAsync(Folder folder)
        {
            _context.Folders.Remove(folder);
            await _context.SaveChangesAsync();
        }
    }
}
