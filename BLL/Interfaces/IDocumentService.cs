using Core.DTOs.Common;
using Core.DTOs.Documents;
using System;
using System.Collections.Generic;
using System.Threading.Tasks;

namespace BLL.Interfaces
{
    /// <summary>
    /// Defines document upload, listing, and deletion use cases.
    /// </summary>
    public interface IDocumentService
    {
        Task<IReadOnlyList<DocumentDto>> GetDocumentsBySubjectIdAsync(Guid subjectId);

        Task<Result<DocumentDto>> UploadAsync(DocumentUploadDto uploadDto);

        Task<Result> DeleteAsync(Guid documentId, Guid userId);

        Task<Result> RetryProcessingAsync(Guid documentId, Guid userId);
    }
}
