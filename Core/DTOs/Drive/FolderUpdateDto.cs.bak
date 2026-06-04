using System;
using System.ComponentModel.DataAnnotations;

namespace Core.DTOs.Drive
{
    /// <summary>
    /// Data transfer object for updating an existing folder in the drive.
    /// </summary>
    public class FolderUpdateDto
    {
        [Required(ErrorMessage = "Folder name is required.")]
        [StringLength(200, ErrorMessage = "Folder name cannot exceed 200 characters.")]
        public string Name { get; set; } = string.Empty;

        public string? Icon { get; set; }

        public string? Color { get; set; }
    }
}
