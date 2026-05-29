using Core.Entities;
using System;

namespace Core.DTOs.Drive
{
    /// <summary>
    /// DTO carrying statistics for a root-level subject folder.
    /// </summary>
    public class SubjectStatsDto
    {
        /// <summary>The root folder (subject / môn học).</summary>
        public Folder Folder { get; set; } = null!;

        /// <summary>Number of direct sub-folders (chapters).</summary>
        public int ChapterCount { get; set; }

        /// <summary>Total number of documents across all chapters.</summary>
        public int DocumentCount { get; set; }
    }
}
