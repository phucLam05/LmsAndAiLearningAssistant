using Core.Entities;
using System;
using System.Collections.Generic;
using System.Threading.Tasks;

namespace DAL.Interfaces
{
    /// <summary>
    /// Provides data access operations for managing folders.
    /// </summary>
    public interface IFolderRepository
    {
        /// <summary>
        /// Retrieves a specific folder by its ID and ensures it belongs to the specified user.
        /// </summary>
        Task<Folder?> GetByIdWithOwnerAsync(Guid id, Guid userId);
        
        /// <summary>
        /// Gets the root folders and documents if folderId is null, 
        /// otherwise gets the subfolders and documents of the specified folder.
        /// </summary>
        Task<(List<Folder> Folders, List<Document> Documents)> GetFolderContentsAsync(Guid? folderId, Guid userId);
        
        /// <summary>
        /// Gets a list of folders representing the breadcrumb trail from the root down to the specified folder.
        /// </summary>
        Task<List<Folder>> GetBreadcrumbsAsync(Guid folderId, Guid userId);
        
        /// <summary>
        /// Checks if a given folder is a subfolder of another folder.
        /// </summary>
        Task<bool> IsSubfolderOfAsync(Guid parentId, Guid childId);
        
        /// <summary>
        /// Gets the document counts for a list of folder IDs (to display "X files" badges).
        /// </summary>
        Task<Dictionary<Guid, int>> GetDocumentCountsForFoldersAsync(IEnumerable<Guid> folderIds, Guid userId);

        /// <summary>
        /// Gets all child folders whose ParentFolderId is in the given set of parent IDs.
        /// Used to retrieve all chapters belonging to multiple subjects in one query.
        /// </summary>
        Task<List<Folder>> GetChildFoldersAsync(IEnumerable<Guid> parentIds, Guid userId);

        /// <summary>
        /// Adds a new folder to the database.
        /// </summary>
        Task<Folder> AddAsync(Folder folder);
        
        /// <summary>
        /// Updates an existing folder in the database.
        /// </summary>
        Task UpdateAsync(Folder folder);
        
        /// <summary>
        /// Deletes a folder from the database.
        /// </summary>
        Task DeleteAsync(Folder folder);
    }
}
