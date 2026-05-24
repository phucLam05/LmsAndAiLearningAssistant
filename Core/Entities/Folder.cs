using System;
using System.Collections.Generic;

namespace Core.Entities
{
    /// <summary>
    /// Represents a folder that groups documents for a user.
    /// Supports hierarchical nesting through parent-child relationships.
    /// </summary>
    public class Folder
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
        /// Optional parent folder identifier for nested folders.
        /// </summary>
        public Guid? ParentFolderId { get; set; }

        /// <summary>
        /// Display name of the folder.
        /// </summary>
        public string Name { get; set; } = string.Empty;

        /// <summary>
        /// Optional icon identifier for UI display.
        /// </summary>
        public string? Icon { get; set; }

        /// <summary>
        /// Optional color value for UI display.
        /// </summary>
        public string? Color { get; set; }

        /// <summary>
        /// Date and time when the folder was created.
        /// </summary>
        public DateTime CreatedAt { get; set; } = DateTime.UtcNow;

        /// <summary>
        /// Date and time when the folder was last updated.
        /// </summary>
        public DateTime UpdatedAt { get; set; } = DateTime.UtcNow;

        /// <summary>
        /// Navigation to the owning user.
        /// </summary>
        public User User { get; set; } = null!;

        /// <summary>
        /// Navigation to the parent folder.
        /// </summary>
        public Folder? ParentFolder { get; set; }

        /// <summary>
        /// Navigation to child folders.
        /// </summary>
        public ICollection<Folder> SubFolders { get; set; } = new List<Folder>();

        /// <summary>
        /// Navigation to documents stored in this folder.
        /// </summary>
        public ICollection<Document> Documents { get; set; } = new List<Document>();
    }
}
