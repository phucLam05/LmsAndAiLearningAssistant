using Microsoft.AspNetCore.Mvc;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading.Tasks;
using BLL.Interfaces;
using Core.DTOs.Documents;
using Core.Entities;
using DAL.Data;
using Microsoft.EntityFrameworkCore;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Http;
using System.Security.Claims;
using System.IO;

namespace PL.Controllers
{
    [Authorize(Roles = "Lecturer")]
    public class LecturerController : Controller
    {
        private readonly ApplicationDbContext _context;
        private readonly IDocumentService _documentService;
        private readonly ISupabaseStorageService _storageService;

        public LecturerController(ApplicationDbContext context, IDocumentService documentService, ISupabaseStorageService storageService)
        {
            _context = context;
            _documentService = documentService;
            _storageService = storageService;
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

            var dbFolders = await _context.Folders.ToListAsync();
            
            // Seed a default folder if empty so the Lecturer view can load immediately
            if (!dbFolders.Any())
            {
                var defaultFolder = new Folder
                {
                    Id = Guid.NewGuid(),
                    UserId = userId.Value,
                    Name = "PRN211 - Basic Cross-Platform Application (C#)",
                    Icon = "bi-folder",
                    Color = "warning",
                    CreatedAt = DateTime.UtcNow,
                    UpdatedAt = DateTime.UtcNow
                };
                _context.Folders.Add(defaultFolder);
                await _context.SaveChangesAsync();
                dbFolders.Add(defaultFolder);
            }

            var assignedSubjects = dbFolders.Select(f => new MockSubject
            {
                Id = f.Id.ToString(),
                Code = f.Name.Split('-').FirstOrDefault()?.Trim() ?? "FLDR",
                Name = f.Name.Contains('-') ? f.Name.Substring(f.Name.IndexOf('-') + 1).Trim() : f.Name,
                DocumentCount = _context.Documents.Count(d => d.FolderId == f.Id)
            }).ToList();

            if (string.IsNullOrEmpty(selectedSubjectId) && assignedSubjects.Any())
            {
                selectedSubjectId = assignedSubjects.First().Id;
            }

            ViewBag.Subjects = assignedSubjects;
            ViewBag.SelectedSubjectId = selectedSubjectId;
            ViewBag.SelectedSubject = assignedSubjects.FirstOrDefault(s => s.Id == selectedSubjectId);

            var selectedGuid = Guid.TryParse(selectedSubjectId, out var folderGuid) ? folderGuid : Guid.Empty;
            var dbDocs = await _context.Documents
                .Include(d => d.User)
                .Where(d => d.FolderId == selectedGuid)
                .OrderByDescending(d => d.CreatedAt)
                .ToListAsync();

            var docs = dbDocs.Select(d => new MockLecturerDoc
            {
                Id = d.Id,
                SubjectId = d.FolderId.ToString(),
                FileName = d.OriginalFileName,
                FileSizeStr = (d.FileSize / 1024.0 / 1024.0).ToString("F2") + " MB",
                Status = d.ProcessingStatus switch
                {
                    DocumentProcessingStatus.Uploaded => "Pending",
                    DocumentProcessingStatus.Processing => "Processing",
                    DocumentProcessingStatus.Indexed => "Success",
                    DocumentProcessingStatus.Failed => "Failed",
                    _ => "Pending"
                },
                StoredBy = d.User?.FullName ?? "System",
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

            var folderGuid = Guid.TryParse(subjectId, out var fg) ? fg : Guid.Empty;
            var folder = await _context.Folders.FindAsync(folderGuid);
            if (folder == null)
            {
                return Json(new { success = false, message = "Folder not found in database." });
            }

            await using var stream = file.OpenReadStream();
            var uploadDto = new DocumentUploadDto
            {
                UserId = userId.Value,
                OriginalFileName = file.FileName,
                ContentType = file.ContentType,
                FileSize = file.Length,
                Content = stream
            };

            var result = await _documentService.UploadAsync(uploadDto);
            if (!result.Success || result.Document == null)
            {
                return Json(new { success = false, message = result.ErrorMessage });
            }

            // Move the document to the chosen lecturer folder/subject
            var doc = await _context.Documents.FindAsync(result.Document.Id);
            if (doc != null)
            {
                doc.FolderId = folderGuid;
                await _context.SaveChangesAsync();
            }

            var user = await _context.Users.FindAsync(userId.Value);

            var mappedDoc = new MockLecturerDoc
            {
                Id = result.Document.Id,
                SubjectId = folderGuid.ToString(),
                FileName = result.Document.OriginalFileName,
                FileSizeStr = (result.Document.FileSize / 1024.0 / 1024.0).ToString("F2") + " MB",
                Status = "Pending",
                StoredBy = user?.FullName ?? "Lecturer",
                CreatedAt = result.Document.CreatedAt
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
            var doc = await _context.Documents.FindAsync(docId);
            if (doc != null)
            {
                doc.ProcessingStatus = status switch
                {
                    "Pending" => DocumentProcessingStatus.Uploaded,
                    "Processing" => DocumentProcessingStatus.Processing,
                    "Success" => DocumentProcessingStatus.Indexed,
                    "Failed" => DocumentProcessingStatus.Failed,
                    _ => DocumentProcessingStatus.Uploaded
                };

                // Seed simulated chunks and embeddings when the status turns to Success (Indexed)
                if (doc.ProcessingStatus == DocumentProcessingStatus.Indexed)
                {
                    var existingChunks = _context.DocumentChunks.Where(c => c.DocumentId == doc.Id);
                    _context.DocumentChunks.RemoveRange(existingChunks);

                    var chunks = new List<DocumentChunk>
                    {
                        new DocumentChunk
                        {
                            Id = Guid.NewGuid(),
                            DocumentId = doc.Id,
                            ChunkIndex = 0,
                            Content = $"Welcome to LMS AI Learning Assistant! This is the first chunk of text extracted from your uploaded file: '{doc.OriginalFileName}'. We analyzed the syllabus and key learning structures.",
                            TokenCount = 35,
                            PageNumber = 1,
                            Embedding = new Pgvector.Vector(new float[768]),
                            CreatedAt = DateTime.UtcNow
                        },
                        new DocumentChunk
                        {
                            Id = Guid.NewGuid(),
                            DocumentId = doc.Id,
                            ChunkIndex = 1,
                            Content = $"In this course segment, we focus on design principles, data structures, and database interactions using Entity Framework Core. High-quality RAG performance relies on precise document extraction.",
                            TokenCount = 31,
                            PageNumber = 2,
                            Embedding = new Pgvector.Vector(new float[768]),
                            CreatedAt = DateTime.UtcNow
                        }
                    };
                    _context.DocumentChunks.AddRange(chunks);
                }

                await _context.SaveChangesAsync();
                return Json(new { success = true });
            }
            return Json(new { success = false, message = "Document not found." });
        }

        [HttpGet]
        public async Task<IActionResult> ViewOriginal(Guid id)
        {
            var userId = GetCurrentUserId();
            if (userId == null)
            {
                return Challenge();
            }

            var doc = await _context.Documents.FindAsync(id);
            if (doc == null)
            {
                return NotFound("Document not found.");
            }

            try
            {
                var signedUrl = await _storageService.GetSignedUrlAsync(doc.StoragePath);
                return Redirect(signedUrl);
            }
            catch (Exception ex)
            {
                return BadRequest($"Failed to generate view link: {ex.Message}");
            }
        }

        [HttpGet]
        public async Task<IActionResult> DocumentDetails(Guid id)
        {
            var doc = await _context.Documents
                .Include(d => d.Chunks)
                .FirstOrDefaultAsync(d => d.Id == id);
            
            if (doc == null)
            {
                return NotFound("Document not found.");
            }

            return View(doc);
        }

        private Guid? GetCurrentUserId()
        {
            var value = User.FindFirstValue(ClaimTypes.NameIdentifier);
            return Guid.TryParse(value, out var userId) ? userId : null;
        }
    }
}
