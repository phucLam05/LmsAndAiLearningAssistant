using Core.Entities;

namespace DAL.Interfaces
{
    /// <summary>
    /// Provides the minimal folder lookup needed by document upload because the current Document entity requires FolderId.
    /// </summary>
    public interface IFolderRepository
    {
        Task<Folder> GetOrCreateDefaultUploadFolderAsync(Guid userId);
    }
}
