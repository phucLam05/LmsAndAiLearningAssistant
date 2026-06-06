using BLL.Interfaces;
using Core.DTOs.Documents;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using System.Security.Claims;

namespace PL.Controllers
{
    [Authorize(Roles = "Lecturer")]
    public class LecturerController : Controller
    {
        private readonly ISubjectService _subjectService;
        private readonly IDocumentService _documentService;

        public LecturerController(ISubjectService subjectService, IDocumentService documentService)
        {
            _subjectService = subjectService;
            _documentService = documentService;
        }

        public class MockSubject
        {
            public string Id { get; set; } = string.Empty;
            public string Code { get; set; } = string.Empty;
            public string Name { get; set; } = string.Empty;
            public int DocumentCount { get; set; }
        }

        public class MockLecturerDoc
        {
            public Guid Id { get; set; }
            public string SubjectId { get; set; } = string.Empty;
            public string FileName { get; set; } = string.Empty;
            public string FileSizeStr { get; set; } = string.Empty;
            public string Status { get; set; } = "Success"; // Pending, Processing, Success, Failed
            public string StoredBy { get; set; } = string.Empty;
            public DateTime CreatedAt { get; set; }
        }

        [HttpGet]
        public async Task<IActionResult> Portal(string? selectedSubjectId = null)
        {
            var userId = GetCurrentUserId();
            if (userId == null)
            {
                return Challenge();
            }

            var dbSubjects = await _subjectService.GetSubjectsByLecturerAsync(userId.Value);
            
            var assignedSubjects = dbSubjects.Select(s => new MockSubject
            {
                Id = s.Id.ToString(),
                Code = s.SubjectCode ?? "SUBJ",
                Name = s.Name,
                DocumentCount = 0 // Not tracked in current schema, we could compute it but keep 0 for now
            }).ToList();

            if (string.IsNullOrEmpty(selectedSubjectId) && assignedSubjects.Any())
            {
                selectedSubjectId = assignedSubjects.First().Id;
            }

            ViewBag.Subjects = assignedSubjects;
            ViewBag.SelectedSubjectId = selectedSubjectId;
            ViewBag.SelectedSubject = assignedSubjects.FirstOrDefault(s => s.Id == selectedSubjectId);

            var selectedGuid = Guid.TryParse(selectedSubjectId, out var folderGuid) ? folderGuid : Guid.Empty;
            var dbDocs = await _documentService.GetDocumentsBySubjectIdAsync(selectedGuid);

            var docs = dbDocs.Select(d => new MockLecturerDoc
            {
                Id = d.Id,
                SubjectId = d.SubjectId?.ToString() ?? string.Empty,
                FileName = d.FileName,
                FileSizeStr = FormatFileSize(d.FileSize),
                Status = d.Status.ToString(),
                StoredBy = d.UploaderName ?? "System",
                CreatedAt = d.CreatedAt
            }).ToList();

            return View(docs);
        }

        [HttpPost]
        [ValidateAntiForgeryToken]
        public async Task<IActionResult> UploadLecturerFile(IFormFile file, string subjectId)
        {
            var userId = GetCurrentUserId();
            if (userId == null)
            {
                return Json(new { success = false, message = "User is not authenticated." });
            }

            if (file == null || file.Length == 0)
            {
                return Json(new { success = false, message = "Please select a file." });
            }

            var subjectGuid = Guid.TryParse(subjectId, out var sg) ? sg : Guid.Empty;

            var dbSubjects = await _subjectService.GetSubjectsByLecturerAsync(userId.Value);
            if (!dbSubjects.Any(s => s.Id == subjectGuid))
            {
                return Json(new { success = false, message = "Unauthorized: You are not assigned to this subject." });
            }

            await using var stream = file.OpenReadStream();
            var uploadDto = new DocumentUploadDto
            {
                UploadedBy = userId.Value,
                SubjectId = subjectGuid,
                FileName = file.FileName,
                ContentType = file.ContentType,
                FileSize = file.Length,
                Content = stream
            };

            var result = await _documentService.UploadAsync(uploadDto);
            if (!result.IsSuccess || result.Data == null)
            {
                return Json(new { success = false, message = result.ErrorMessage });
            }

            var docDto = result.Data;

            var mappedDoc = new MockLecturerDoc
            {
                Id = docDto.Id,
                SubjectId = docDto.SubjectId?.ToString() ?? string.Empty,
                FileName = docDto.FileName,
                FileSizeStr = FormatFileSize(docDto.FileSize),
                Status = docDto.Status.ToString(),
                StoredBy = docDto.UploaderName ?? "Lecturer",
                CreatedAt = docDto.CreatedAt
            };

            return Json(new 
            { 
                success = true, 
                document = mappedDoc 
            });
        }

        [HttpPost]
        public async Task<IActionResult> UpdateDocumentStatus(Guid docId, string status)
        {
            // Placeholder: Processing logic moved to background services.
            return Json(new { success = true });
        }

        private Guid? GetCurrentUserId()
        {
            var value = User.FindFirstValue(ClaimTypes.NameIdentifier);
            return Guid.TryParse(value, out var userId) ? userId : null;
        }

        private static string FormatFileSize(long bytes)
        {
            if (bytes <= 0) return "0 Bytes";
            string[] suffixes = { "Bytes", "KB", "MB", "GB" };
            int counter = 0;
            double number = bytes;
            while (Math.Round(number / 1024) >= 1)
            {
                number = number / 1024;
                counter++;
                if (counter >= suffixes.Length - 1) break;
            }
            return $"{number:F1} {suffixes[counter]}";
        }
    }
}
