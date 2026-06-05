using Core.DTOs.Drive;
using Core.Entities;
using System;
using System.Collections.Generic;

namespace PL.Models.Drive
{
    /// <summary>
    /// ViewModel for the Chapter List page.
    /// Represents a subject folder and its sub-folders (chapters).
    /// </summary>
    public class DriveChaptersViewModel
    {
        /// <summary>The ID of the current subject folder (Môn học).</summary>
        public Guid SubjectFolderId { get; set; }

        /// <summary>The name of the current subject folder.</summary>
        public string SubjectName { get; set; } = string.Empty;

        /// <summary>The list of sub-folders (Chương / chapters).</summary>
        public List<Folder> Chapters { get; set; } = new List<Folder>();

        /// <summary>Direct documents uploaded to this subject folder.</summary>
        public List<Document> Documents { get; set; } = new List<Document>();

        /// <summary>Document counts per chapter folder ID.</summary>
        public Dictionary<Guid, int> DocumentCounts { get; set; } = new Dictionary<Guid, int>();

        /// <summary>Breadcrumb trail for navigation.</summary>
        public List<BreadcrumbDto> Breadcrumbs { get; set; } = new List<BreadcrumbDto>();
    }
}
