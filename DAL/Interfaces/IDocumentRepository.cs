using Core.Entities;

namespace DAL.Interfaces
{
    /// <summary>
    /// Provides database operations for document metadata while keeping EF Core access out of controllers and services.
    /// </summary>
    public interface IDocumentRepository
    {
        Task<IReadOnlyList<Document>> GetByUserIdAsync(Guid userId);

        Task<Document?> GetByIdForUserAsync(Guid documentId, Guid userId);

        Task<Document> AddAsync(Document document);

        Task DeleteAsync(Document document);
    }
}
