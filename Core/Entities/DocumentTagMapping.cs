using System;

namespace Core.Entities
{
    /// <summary>
    /// Represents the many-to-many link between documents and tags.
    /// </summary>
    public class DocumentTagMapping
    {
        /// <summary>
        /// Identifier of the document.
        /// </summary>
        public Guid DocumentId { get; set; }

        /// <summary>
        /// Identifier of the tag.
        /// </summary>
        public Guid TagId { get; set; }

        /// <summary>
        /// Navigation to the document.
        /// </summary>
        public Document Document { get; set; } = null!;

        /// <summary>
        /// Navigation to the tag.
        /// </summary>
        public DocumentTag Tag { get; set; } = null!;
    }
}
