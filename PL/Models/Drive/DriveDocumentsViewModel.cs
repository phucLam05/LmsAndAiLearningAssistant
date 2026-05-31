using Core.Entities;
using System.Collections.Generic;

namespace PL.Models.Drive
{
    public class DriveDocumentsViewModel
    {
        public List<Document> Documents { get; set; } = new List<Document>();
        public int TotalDocuments { get; set; }
        public int IndexedDocuments { get; set; }
        public string TotalSizeFormatted { get; set; } = "0 B";
    }
}
