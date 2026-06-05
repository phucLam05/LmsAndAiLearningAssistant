using BLL.Interfaces;
using Core.DTOs.Documents;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using PL.Models.Documents;
using System;
using System.Security.Claims;
using System.Threading.Tasks;

namespace PL.Controllers
{
    /// <summary>
    /// Handles document mutation endpoints (upload, delete, retry) within a Subject context.
    /// </summary>
    [Authorize]
    public class DocumentController : Controller
    {
        private readonly IDocumentService _documentService;

        public DocumentController(IDocumentService documentService)
        {
            _documentService = documentService;
        }

        [HttpPost]
        [ValidateAntiForgeryToken]
        public async Task<IActionResult> Upload(DocumentUploadViewModel model)
        {
            var userId = GetCurrentUserId();
            if (userId == null)
            {
                return Challenge();
            }

            if (!ModelState.IsValid || model.File == null)
            {
                TempData["ErrorMessage"] = "Invalid file or form submission.";
                return RedirectToAction("Details", "Subject", new { id = model.SubjectId });
            }

            await using var stream = model.File.OpenReadStream();
            var uploadDto = new DocumentUploadDto
            {
                UploadedBy = userId.Value,
                SubjectId = model.SubjectId,
                FileName = model.File.FileName,
                ContentType = model.File.ContentType,
                FileSize = model.File.Length,
                Content = stream
            };

            var result = await _documentService.UploadAsync(uploadDto);
            if (!result.IsSuccess)
            {
                TempData["ErrorMessage"] = result.ErrorMessage;
            }
            else
            {
                TempData["SuccessMessage"] = "File uploaded successfully and AI indexing started.";
            }

            return RedirectToAction("Details", "Subject", new { id = model.SubjectId });
        }

        [HttpPost]
        [ValidateAntiForgeryToken]
        public async Task<IActionResult> Delete(Guid id, Guid subjectId)
        {
            var userId = GetCurrentUserId();
            if (userId == null)
            {
                return Challenge();
            }

            var result = await _documentService.DeleteAsync(id, userId.Value);

            if (result.IsSuccess)
            {
                TempData["SuccessMessage"] = "Document deleted successfully.";
            }
            else
            {
                TempData["ErrorMessage"] = result.ErrorMessage;
            }

            return RedirectToAction("Details", "Subject", new { id = subjectId });
        }

        [HttpPost]
        [ValidateAntiForgeryToken]
        public async Task<IActionResult> RetryProcessing(Guid id, Guid subjectId)
        {
            var userId = GetCurrentUserId();
            if (userId == null)
            {
                return Challenge();
            }

            var result = await _documentService.RetryProcessingAsync(id, userId.Value);
            if (result.IsSuccess)
            {
                TempData["SuccessMessage"] = "AI processing restarted successfully.";
            }
            else
            {
                TempData["ErrorMessage"] = result.ErrorMessage;
            }

            return RedirectToAction("Details", "Subject", new { id = subjectId });
        }

        private Guid? GetCurrentUserId()
        {
            var value = User.FindFirstValue(ClaimTypes.NameIdentifier);
            return Guid.TryParse(value, out var userId) ? userId : null;
        }
    }
}
