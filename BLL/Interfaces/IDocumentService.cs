using Core.DTOs.Documents;

namespace BLL.Interfaces
{
    /// <summary>
    /// Defines document upload, listing, and deletion use cases for the presentation layer.
    /// </summary>
    public interface IDocumentService
    {
        Task<IReadOnlyList<DocumentDto>> GetDocumentsForUserAsync(Guid userId);

        Task<(bool Success, DocumentDto? Document, string ErrorMessage)> UploadAsync(DocumentUploadDto uploadDto);

        Task<(bool Success, string ErrorMessage)> DeleteAsync(Guid documentId, Guid userId);
    }
}
