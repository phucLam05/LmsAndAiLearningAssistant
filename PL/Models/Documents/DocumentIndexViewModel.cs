using Core.DTOs.Documents;

namespace PL.Models.Documents
{
    /// <summary>
    /// Provides uploaded document rows for the document management page.
    /// </summary>
    public class DocumentIndexViewModel
    {
        public IReadOnlyList<DocumentDto> Documents { get; set; } = Array.Empty<DocumentDto>();
    }
}
