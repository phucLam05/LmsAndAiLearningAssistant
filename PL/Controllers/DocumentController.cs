using BLL.Interfaces;
using Core.DTOs.Documents;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using PL.Models.Documents;
using System.Security.Claims;

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

            if (!ModelState.IsValid || model.Files == null || model.Files.Count == 0)
            {
                TempData["ErrorMessage"] = "Vui lòng chọn ít nhất một tệp tin hợp lệ.";
                return RedirectToAction("Details", "Subject", new { id = model.SubjectId });
            }

            int successCount = 0;
            int errorCount = 0;
            string lastError = "";

            foreach (var file in model.Files)
            {
                try
                {
                    await using var stream = file.OpenReadStream();
                    var uploadDto = new DocumentUploadDto
                    {
                        UploadedBy = userId.Value,
                        SubjectId = model.SubjectId,
                        FileName = file.FileName,
                        ContentType = file.ContentType,
                        FileSize = file.Length,
                        Content = stream
                    };

                    var result = await _documentService.UploadAsync(uploadDto);
                    if (result.IsSuccess)
                    {
                        successCount++;
                    }
                    else
                    {
                        errorCount++;
                        lastError = result.ErrorMessage;
                    }
                }
                catch (Exception ex)
                {
                    errorCount++;
                    lastError = ex.Message;
                }
            }

            if (errorCount > 0)
            {
                if (successCount > 0)
                {
                    TempData["SuccessMessage"] = $"Tải lên thành công {successCount} tài liệu. Có {errorCount} tài liệu gặp lỗi: {lastError}";
                }
                else
                {
                    TempData["ErrorMessage"] = $"Lỗi tải lên tài liệu: {lastError}";
                }
            }
            else
            {
                TempData["SuccessMessage"] = successCount > 1 
                    ? $"Đã tải lên thành công {successCount} tài liệu và bắt đầu phân tích AI."
                    : "Tài liệu đã được tải lên thành công và bắt đầu phân tích AI.";
            }

            return RedirectToAction("Details", "Subject", new { id = model.SubjectId });
        }

        [HttpPost]
        [ValidateAntiForgeryToken]
        public async Task<IActionResult> Delete(Guid id, Guid subjectId, string? redirectUrl = null)
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

            if (!string.IsNullOrEmpty(redirectUrl))
            {
                return Redirect(redirectUrl);
            }
            return RedirectToAction("Details", "Subject", new { id = subjectId });
        }

        [HttpPost]
        [ValidateAntiForgeryToken]
        public async Task<IActionResult> RetryProcessing(Guid id, Guid subjectId, string? redirectUrl = null)
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

            if (!string.IsNullOrEmpty(redirectUrl))
            {
                return Redirect(redirectUrl);
            }
            return RedirectToAction("Details", "Subject", new { id = subjectId });
        }

        [HttpGet]
        public async Task<IActionResult> ViewOriginal(Guid id)
        {
            var result = await _documentService.DownloadDocumentAsync(id);
            if (result == null)
                return NotFound("Document not found or could not be downloaded.");

            // Return the stream directly to the browser
            // To force download, you could add: return File(result.Value.Stream, result.Value.ContentType, result.Value.FileName);
            // To view in browser: return File(result.Value.Stream, result.Value.ContentType);
            return File(result.Value.Stream, result.Value.ContentType);
        }

        [HttpGet]
        public async Task<IActionResult> Details(Guid id)
        {
            var document = await _documentService.GetDocumentByIdAsync(id);
            if (document == null)
                return NotFound("Document not found.");

            var chunks = await _documentService.GetDocumentChunksAsync(id);
            ViewBag.Chunks = chunks;

            return View(document);
        }

        private Guid? GetCurrentUserId()
        {
            var value = User.FindFirstValue(ClaimTypes.NameIdentifier);
            return Guid.TryParse(value, out var userId) ? userId : null;
        }
    }
}
