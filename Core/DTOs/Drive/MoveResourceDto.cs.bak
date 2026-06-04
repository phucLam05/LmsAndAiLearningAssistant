using System;
using System.ComponentModel.DataAnnotations;

namespace Core.DTOs.Drive
{
    /// <summary>
    /// Data transfer object for moving a resource (folder or document) to a new destination folder.
    /// </summary>
    public class MoveResourceDto
    {
        [Required]
        public Guid ResourceId { get; set; }

        public Guid? DestinationFolderId { get; set; }

        [Required]
        public bool IsFolder { get; set; }
    }
}
