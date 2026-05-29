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
    /// Handles MVC requests for listing, uploading, and deleting the current user's files.
    /// </summary>
    [Authorize]
    public class DocumentController : Controller
    {
        private const long MaxUploadSize = 50L * 1024L * 1024L;

        private readonly IDocumentService _documentService;

        /// <summary>
        /// Creates a document controller that delegates upload and metadata behavior to the BLL service.
        /// </summary>
        public DocumentController(IDocumentService documentService)
        {
            _documentService = documentService;
        }

        /// <summary>
        /// Shows only the current user's uploaded files.
        /// </summary>
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

        /// <summary>
        /// Shows the upload form.
        /// </summary>
        [HttpGet]
        public IActionResult Upload()
        {
            return View(new DocumentUploadViewModel());
        }

        /// <summary>
        /// Receives the uploaded file and passes its stream to the BLL service for validation and storage.
        /// </summary>
        [HttpPost]
        [ValidateAntiForgeryToken]
        [RequestSizeLimit(MaxUploadSize)]
        [RequestFormLimits(MultipartBodyLengthLimit = MaxUploadSize)]
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

            TempData["SuccessMessage"] = "File uploaded successfully.";
            return RedirectToAction(nameof(Index));
        }

        /// <summary>
        /// Deletes a file only when the metadata record belongs to the current user.
        /// </summary>
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
                result.Success ? "File deleted successfully." : result.ErrorMessage;

            return RedirectToAction(nameof(Index));
        }

        /// <summary>
        /// Reads the authenticated user's Guid from the auth cookie claim.
        /// </summary>
        private Guid? GetCurrentUserId()
        {
            var value = User.FindFirstValue(ClaimTypes.NameIdentifier);
            return Guid.TryParse(value, out var userId) ? userId : null;
        }
    }
}
