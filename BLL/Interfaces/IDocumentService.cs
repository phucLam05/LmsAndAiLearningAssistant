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
    }
}
