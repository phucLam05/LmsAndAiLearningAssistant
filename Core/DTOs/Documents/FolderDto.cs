using System;

namespace Core.DTOs.Documents
{
    /// <summary>
    /// Data Transfer Object for folder details.
    /// </summary>
    public class FolderDto
    {
        /// <summary>
        /// Unique identifier for the folder.
        /// </summary>
        public Guid Id { get; set; }

        /// <summary>
        /// Identifier of the user who owns the folder.
        /// </summary>
        public Guid UserId { get; set; }

        /// <summary>
        /// Optional parent folder identifier.
        /// </summary>
        public Guid? ParentFolderId { get; set; }

        /// <summary>
        /// Display name of the folder.
        /// </summary>
        public string Name { get; set; } = string.Empty;

        /// <summary>
        /// Optional icon identifier.
        /// </summary>
        public string? Icon { get; set; }

        /// <summary>
        /// Optional color value.
        /// </summary>
        public string? Color { get; set; }

        /// <summary>
        /// Date and time when the folder was created.
        /// </summary>
        public DateTime CreatedAt { get; set; }

        /// <summary>
        /// Date and time when the folder was last updated.
        /// </summary>
        public DateTime UpdatedAt { get; set; }
    }
}
