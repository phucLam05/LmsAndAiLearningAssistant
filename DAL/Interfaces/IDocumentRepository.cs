using Core.Entities;
using System;
using System.Threading.Tasks;

namespace DAL.Interfaces
{
    /// <summary>
    /// Provides data access operations for managing documents.
    /// </summary>
    public interface IDocumentRepository
    {
        Task<Document?> GetByIdWithOwnerAsync(Guid id, Guid userId);

        /// <summary>
        /// Retrieves all documents belonging to the specified user, including their folder and parent folder details.
        /// </summary>
        Task<System.Collections.Generic.List<Document>> GetAllWithOwnerAsync(Guid userId);
        
        /// <summary>
        /// Adds a new document to the database.
        /// </summary>
        Task<Document> AddAsync(Document document);
        
        /// <summary>
        /// Updates an existing document in the database.
        /// </summary>
        Task UpdateAsync(Document document);
        
        /// <summary>
        /// Deletes a document from the database.
        /// </summary>
        Task DeleteAsync(Document document);
    }
}
