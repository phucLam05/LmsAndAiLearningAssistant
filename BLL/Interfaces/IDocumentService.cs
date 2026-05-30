using Core.DTOs.Common;
using Core.DTOs.Documents;
using System;
using System.Collections.Generic;
using System.Threading.Tasks;

namespace BLL.Interfaces
{
    /// <summary>
    /// Defines document upload, listing, and deletion use cases for the presentation layer.
    /// </summary>
    public interface IDocumentService
    {
        Task<IReadOnlyList<DocumentDto>> GetDocumentsForUserAsync(Guid userId);

        Task<Result<DocumentDto>> UploadAsync(DocumentUploadDto uploadDto);

        Task<Result> DeleteAsync(Guid documentId, Guid userId);

        /// <summary>
        /// Restarts the processing pipeline (chunking and embedding) for a document.
        /// Validates that the document belongs to the requesting user before processing.
        /// </summary>
        /// <param name="documentId">The ID of the document to retry.</param>
        /// <param name="userId">The ID of the user requesting the retry.</param>
        /// <returns>A Result indicating success or failure.</returns>
        Task<Result> RetryProcessingAsync(Guid documentId, Guid userId);
    }
}
