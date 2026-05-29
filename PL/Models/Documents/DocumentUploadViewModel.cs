using System.ComponentModel.DataAnnotations;

namespace PL.Models.Documents
{
    /// <summary>
    /// Represents the MVC upload form fields for a learning document.
    /// </summary>
    public class DocumentUploadViewModel
    {
        [Required]
        [Display(Name = "File")]
        public IFormFile? File { get; set; }
    }
}
