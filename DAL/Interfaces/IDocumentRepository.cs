using Core.Entities;
using System;
using System.Collections.Generic;
using System.Threading.Tasks;

namespace DAL.Interfaces
{
    /// <summary>
    /// Provides database and data access operations for managing documents.
    /// </summary>
    public interface IDocumentRepository
    {
        /// <summary>
        /// Retrieves all documents uploaded by a specific user.
        /// </summary>
        /// <param name="userId">The unique identifier of the user.</param>
        /// <returns>A list of documents belonging to the user.</returns>
        Task<IReadOnlyList<Document>> GetByUserIdAsync(Guid userId);

        /// <summary>
        /// Retrieves a document by its unique identifier and user identifier to ensure ownership.
        /// </summary>
        /// <param name="documentId">The unique identifier of the document.</param>
        /// <param name="userId">The unique identifier of the user.</param>
        /// <returns>The Document if found and owned by the user; otherwise, null.</returns>
        Task<Document?> GetByIdForUserAsync(Guid documentId, Guid userId);

        /// <summary>
        /// Retrieves a document by its unique identifier.
        /// </summary>
        /// <param name="id">The unique identifier of the document.</param>
        /// <returns>The Document if found; otherwise, null.</returns>
        Task<Document?> GetByIdAsync(Guid id);

        /// <summary>
        /// Adds a new document record to the database.
        /// </summary>
        /// <param name="document">The document entity to add.</param>
        /// <returns>The added document.</returns>
        Task<Document> AddAsync(Document document);

        /// <summary>
        /// Deletes a document record from the database.
        /// </summary>
        /// <param name="document">The document entity to delete.</param>
        /// <returns>A task that represents the asynchronous operation.</returns>
        Task DeleteAsync(Document document);

        /// <summary>
        /// Updates the processing status of a specific document.
        /// </summary>
        /// <param name="id">The unique identifier of the document.</param>
        /// <param name="status">The new processing status.</param>
        /// <returns>A task that represents the asynchronous operation.</returns>
        Task UpdateStatusAsync(Guid id, DocumentProcessingStatus status);

        /// <summary>
        /// Retrieves a specific document by its ID and owner ID.
        /// </summary>
        Task<Document?> GetByIdWithOwnerAsync(Guid id, Guid userId);

        /// <summary>
        /// Retrieves all documents belonging to the specified user, including their folder and parent folder details.
        /// </summary>
        Task<System.Collections.Generic.List<Document>> GetAllWithOwnerAsync(Guid userId);
        
        /// <summary>
        /// Updates an existing document in the database.
        /// </summary>
        Task UpdateAsync(Document document);

        /// <summary>
        /// Clears the Entity Framework change tracker.
        /// </summary>
        void ClearTracker();
    }
}
