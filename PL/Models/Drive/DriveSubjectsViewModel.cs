using Core.Entities;
using System;
using System.Collections.Generic;
using System.Text.RegularExpressions;

namespace PL.Models.Drive
{
    /// <summary>
    /// Data for a single subject card on the Môn học page.
    /// Parsed from SubjectStatsDto with code/name extraction.
    /// </summary>
    public class SubjectCardData
    {
        public Folder Folder { get; set; } = null!;

        /// <summary>Parsed subject code, e.g. "CS401". Empty if not found in name.</summary>
        public string SubjectCode { get; set; } = string.Empty;

        /// <summary>Display name without the code prefix.</summary>
        public string DisplayName { get; set; } = string.Empty;

        public int ChapterCount { get; set; }
        public int DocumentCount { get; set; }

        /// <summary>
        /// Parse SubjectCode and DisplayName from a folder name.
        /// Supports formats: "CS401 - Trí tuệ nhân tạo" or "CS401: Trí tuệ nhân tạo" or plain name.
        /// </summary>
        public static (string code, string name) ParseName(string folderName)
        {
            var match = Regex.Match(folderName.Trim(), @"^([A-Za-z]{1,4}[0-9]{2,5})\s*[-:]\s*(.+)$");
            if (match.Success)
                return (match.Groups[1].Value.ToUpper(), match.Groups[2].Value.Trim());
            return (string.Empty, folderName.Trim());
        }
    }

    /// <summary>
    /// ViewModel for the Subjects (Môn học) page — lists all root-level folders with stats.
    /// </summary>
    public class DriveSubjectsViewModel
    {
        public List<SubjectCardData> Subjects { get; set; } = new();
    }
}
