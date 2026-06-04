using System;
using System.ComponentModel.DataAnnotations;
using Microsoft.AspNetCore.Http;

namespace PL.Models.Documents
{
    /// <summary>
    /// Represents the MVC upload form fields for a learning document, including the subject mapping.
    /// </summary>
    public class DocumentUploadViewModel
    {
        [Required(ErrorMessage = "Please select a file to upload.")]
        [Display(Name = "File")]
        public IFormFile? File { get; set; }

        [Required]
        public Guid SubjectId { get; set; }
    }
}
