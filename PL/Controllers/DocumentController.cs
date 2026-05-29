using System.Security.Claims;
using BLL.Interfaces;
using Core.DTOs.Documents;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Http.Features;
using Microsoft.AspNetCore.Mvc;
using PL.Models.Documents;

namespace PL.Controllers
{
    /// <summary>
    /// Handles MVC requests for listing, uploading, and deleting the current user's documents.
    /// </summary>
    [Authorize]
    public class DocumentController : Controller
    {
        private readonly IDocumentService _documentService;

        public DocumentController(IDocumentService documentService)
        {
            _documentService = documentService;
        }

        [HttpGet]
        public async Task<IActionResult> Index()
        {
            var userId = GetCurrentUserId();
            if (userId == null)
            {
                return Challenge();
            }

            var model = new DocumentIndexViewModel
            {
                Documents = await _documentService.GetDocumentsForUserAsync(userId.Value)
            };

            return View(model);
        }

        [HttpGet]
        public IActionResult Upload()
        {
            return View(new DocumentUploadViewModel());
        }

        [HttpPost]
        [ValidateAntiForgeryToken]
        [RequestSizeLimit(50L * 1024L * 1024L)]
        [RequestFormLimits(MultipartBodyLengthLimit = 50L * 1024L * 1024L)]
        public async Task<IActionResult> Upload(DocumentUploadViewModel model)
        {
            var userId = GetCurrentUserId();
            if (userId == null)
            {
                return Challenge();
            }

            if (!ModelState.IsValid || model.File == null)
            {
                return View(model);
            }

            await using var stream = model.File.OpenReadStream();
            var uploadDto = new DocumentUploadDto
            {
                UserId = userId.Value,
                OriginalFileName = model.File.FileName,
                ContentType = model.File.ContentType,
                FileSize = model.File.Length,
                Content = stream
            };

            var result = await _documentService.UploadAsync(uploadDto);
            if (!result.Success)
            {
                ModelState.AddModelError(string.Empty, result.ErrorMessage);
                return View(model);
            }

            TempData["SuccessMessage"] = "Document uploaded successfully.";
            return RedirectToAction(nameof(Index));
        }

        [HttpPost]
        [ValidateAntiForgeryToken]
        public async Task<IActionResult> Delete(Guid id)
        {
            var userId = GetCurrentUserId();
            if (userId == null)
            {
                return Challenge();
            }

            var result = await _documentService.DeleteAsync(id, userId.Value);
            TempData[result.Success ? "SuccessMessage" : "ErrorMessage"] =
                result.Success ? "Document deleted successfully." : result.ErrorMessage;

            return RedirectToAction(nameof(Index));
        }

        private Guid? GetCurrentUserId()
        {
            var value = User.FindFirstValue(ClaimTypes.NameIdentifier);
            return Guid.TryParse(value, out var userId) ? userId : null;
        }
    }
}
