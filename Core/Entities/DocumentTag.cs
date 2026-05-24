using System;
using System.Collections.Generic;

namespace Core.Entities
{
    /// <summary>
    /// Represents a tag that can be assigned to documents.
    /// </summary>
    public class DocumentTag
    {
        /// <summary>
        /// Unique identifier for the tag.
        /// </summary>
        public Guid Id { get; set; }

        /// <summary>
        /// Identifier of the user who owns the tag.
        /// </summary>
        public Guid UserId { get; set; }

        /// <summary>
        /// Display name of the tag.
        /// </summary>
        public string Name { get; set; } = string.Empty;

        /// <summary>
        /// Optional color value for UI display.
        /// </summary>
        public string? Color { get; set; }

        /// <summary>
        /// Date and time when the tag was created.
        /// </summary>
        public DateTime CreatedAt { get; set; } = DateTime.UtcNow;

        /// <summary>
        /// Navigation to the owning user.
        /// </summary>
        public User User { get; set; } = null!;

        /// <summary>
        /// Navigation to document-tag mappings.
        /// </summary>
        public ICollection<DocumentTagMapping> TagMappings { get; set; } = new List<DocumentTagMapping>();
    }
}
