using BLL.Interfaces;
using Core.DTOs.Drive;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Http;
using Microsoft.AspNetCore.Mvc;
using PL.Models.Drive;
using System;
using System.Security.Claims;
using System.Threading.Tasks;

namespace PL.Controllers
{
    /// <summary>
    /// Controller for managing the user's drive (folders and documents).
    /// </summary>
    [Authorize]
    public class DriveController : Controller
    {
        private readonly IDriveService _driveService;
        private readonly IDocumentService _documentService;

        public DriveController(IDriveService driveService, IDocumentService documentService)
        {
            _driveService = driveService;
            _documentService = documentService;
        }

        /// <summary>
        /// Gets the current user ID from the claims.
        /// </summary>
        private Guid GetUserId()
        {
            var userIdString = User.FindFirstValue(ClaimTypes.NameIdentifier);
            return Guid.TryParse(userIdString, out var userId) ? userId : Guid.Empty;
        }

        /// <summary>
        /// Displays all root-level subjects (Môn học). Redirects here when folderId is null.
        /// </summary>
        public async Task<IActionResult> Subjects()
        {
            var userId = GetUserId();
            var statsList = await _driveService.GetSubjectsAsync(userId);

            var viewModel = new PL.Models.Drive.DriveSubjectsViewModel
            {
                Subjects = statsList.Select(s =>
                {
                    var (code, name) = PL.Models.Drive.SubjectCardData.ParseName(s.Folder.Name);
                    return new PL.Models.Drive.SubjectCardData
                    {
                        Folder = s.Folder,
                        SubjectCode = code,
                        DisplayName = name,
                        ChapterCount = s.ChapterCount,
                        DocumentCount = s.DocumentCount
                    };
                }).ToList()
            };

            return View(viewModel);
        }

        /// <summary>
        /// Displays the contents of the specified folder.
        /// Redirects to Subjects when no folderId is given (root level).
        /// </summary>
        public async Task<IActionResult> Index(Guid? folderId)
        {
            // Root level → redirect to rich Subjects page
            if (!folderId.HasValue)
                return RedirectToAction(nameof(Subjects));

            var userId = GetUserId();
            var contents = await _driveService.GetDriveContentsAsync(folderId, userId);
            var breadcrumbs = await _driveService.GetBreadcrumbsAsync(folderId, userId);

            var viewModel = new DriveIndexViewModel
            {
                CurrentFolderId = folderId,
                Folders = contents.Folders,
                Documents = contents.Documents,
                Breadcrumbs = breadcrumbs
            };

            return View(viewModel);
        }

        /// <summary>
        /// Displays the global document manager showing all files across all folders/subjects.
        /// Includes stats for total documents, indexed documents, and formatted storage usage.
        /// </summary>
        /// <returns>The documents overview layout view.</returns>
        [HttpGet]
        public async Task<IActionResult> Documents()
        {
            var userId = GetUserId();
            var documents = await _driveService.GetAllDocumentsAsync(userId);

            var totalDocs = documents.Count;
            var indexedDocs = documents.Count(d => d.ProcessingStatus == Core.Entities.DocumentProcessingStatus.Indexed);
            long totalBytes = documents.Sum(d => d.FileSize);

            // Format total size
            string totalSizeFormatted;
            if (totalBytes >= 1024L * 1024L * 1024L) // GB
                totalSizeFormatted = $"{(double)totalBytes / (1024 * 1024 * 1024):N1} GB";
            else if (totalBytes >= 1024L * 1024L) // MB
                totalSizeFormatted = $"{(double)totalBytes / (1024 * 1024):N1} MB";
            else if (totalBytes >= 1024L) // KB
                totalSizeFormatted = $"{(double)totalBytes / 1024:N1} KB";
            else
                totalSizeFormatted = $"{totalBytes} B";

            var viewModel = new DriveDocumentsViewModel
            {
                Documents = documents,
                TotalDocuments = totalDocs,
                IndexedDocuments = indexedDocs,
                TotalSizeFormatted = totalSizeFormatted
            };

            return View(viewModel);
        }

        /// <summary>
        /// Displays the chapter list for a subject folder (Môn học → Danh sách Chương).
        /// </summary>
        public async Task<IActionResult> Chapters(Guid subjectFolderId)
        {
            var userId = GetUserId();
            var (subjectFolder, chapters, documents, documentCounts) = await _driveService.GetChaptersAsync(subjectFolderId, userId);

            if (subjectFolder == null)
            {
                TempData["ErrorMessage"] = "Môn học không tồn tại hoặc bạn không có quyền truy cập.";
                return RedirectToAction(nameof(Index));
            }

            var breadcrumbs = await _driveService.GetBreadcrumbsAsync(subjectFolderId, userId);

            var viewModel = new PL.Models.Drive.DriveChaptersViewModel
            {
                SubjectFolderId = subjectFolderId,
                SubjectName = subjectFolder.Name,
                Chapters = chapters,
                Documents = documents,
                DocumentCounts = documentCounts,
                Breadcrumbs = breadcrumbs
            };

            return View(viewModel);
        }

        /// <summary>
        /// Creates a new folder.
        /// </summary>
        [HttpPost]
        public async Task<IActionResult> CreateFolder(FolderCreateDto model, string? returnUrl = null)
        {
            if (!ModelState.IsValid)
            {
                TempData["ErrorMessage"] = "Dữ liệu không hợp lệ.";
            }
            else
            {
                var userId = GetUserId();
                var (success, error) = await _driveService.CreateFolderAsync(model, userId);
                if (!success)
                    TempData["ErrorMessage"] = error;
                else
                    TempData["SuccessMessage"] = "Đã tạo thành công.";                
            }

            if (!string.IsNullOrEmpty(returnUrl)) return Redirect(returnUrl);

            // If this folder was created inside a subject folder, go back to Chapters view
            if (model.ParentFolderId.HasValue)
                return RedirectToAction(nameof(Chapters), new { subjectFolderId = model.ParentFolderId.Value });

            return RedirectToAction(nameof(Index));
        }

        /// <summary>
        /// Updates an existing folder (e.g. rename a chapter). Redirects to returnUrl if provided.
        /// </summary>
        [HttpPost]
        public async Task<IActionResult> UpdateFolder(Guid folderId, FolderUpdateDto dto, string? returnUrl = null)
        {
            var userId = GetUserId();
            var (success, error) = await _driveService.UpdateFolderAsync(folderId, dto, userId);

            if (!success)
                TempData["ErrorMessage"] = error;
            else
                TempData["SuccessMessage"] = "Cập nhật thành công.";

            if (!string.IsNullOrEmpty(returnUrl) && Url.IsLocalUrl(returnUrl))
                return Redirect(returnUrl);

            return RedirectToAction(nameof(Index));
        }

        /// <summary>
        /// Moves a resource to a new destination folder.
        /// </summary>
        [HttpPost]
        public async Task<IActionResult> MoveItem(MoveResourceDto model)
        {
            if (!ModelState.IsValid)
            {
                TempData["ErrorMessage"] = "Invalid move data.";
                // We don't have a specific page to redirect to except back to drive, maybe root or current folder.
                return RedirectToAction(nameof(Index));
            }

            var userId = GetUserId();
            var (success, error) = await _driveService.MoveResourceAsync(model, userId);

            if (!success)
            {
                TempData["ErrorMessage"] = error;
            }
            else
            {
                TempData["SuccessMessage"] = "Moved successfully.";
            }

            return RedirectToAction(nameof(Index), new { folderId = model.DestinationFolderId });
        }

        /// <summary>
        /// Deletes a folder (subject or chapter).
        /// - currentFolderId = null → deleted a subject → redirect to Subjects
        /// - currentFolderId = some GUID → deleted a chapter → redirect to Chapters of that subject
        /// </summary>
        [HttpPost]
        public async Task<IActionResult> DeleteFolder(Guid id, Guid? currentFolderId, string? returnUrl = null)
        {
            var userId = GetUserId();
            var (success, error) = await _driveService.DeleteFolderAsync(id, userId);

            if (!success) TempData["ErrorMessage"] = error;
            else TempData["SuccessMessage"] = "Đã xóa thành công.";

            if (!string.IsNullOrEmpty(returnUrl)) return Redirect(returnUrl);

            if (currentFolderId.HasValue)
                return RedirectToAction(nameof(Chapters), new { subjectFolderId = currentFolderId.Value });

            // No parent → was a root subject → go back to Subjects list
            return RedirectToAction(nameof(Subjects));
        }

        /// <summary>
        /// Deletes a document.
        /// </summary>
        [HttpPost]
        public async Task<IActionResult> DeleteFile(Guid id, Guid? currentFolderId, string? returnUrl = null)
        {
            var userId = GetUserId();
            var (success, error) = await _driveService.DeleteDocumentAsync(id, userId);
            
            if (!success) TempData["ErrorMessage"] = error;
            else TempData["SuccessMessage"] = "Xóa tài liệu thành công.";

            if (!string.IsNullOrEmpty(returnUrl)) return Redirect(returnUrl);
            return RedirectToAction(nameof(Index), new { folderId = currentFolderId });
        }

        /// <summary>
        /// Uploads a file directly into a specific folder (subject or chapter).
        /// </summary>
        [HttpPost]
        public async Task<IActionResult> UploadFile(IFormFile file, Guid folderId)
        {
            var userId = GetUserId();
            if (userId == Guid.Empty)
            {
                return Json(new { success = false, message = "Bạn chưa đăng nhập." });
            }

            if (file == null || file.Length == 0)
            {
                return Json(new { success = false, message = "Vui lòng chọn tệp tin." });
            }

            try
            {
                await using var stream = file.OpenReadStream();
                var uploadDto = new Core.DTOs.Documents.DocumentUploadDto
                {
                    UserId = userId,
                    FolderId = folderId,
                    OriginalFileName = file.FileName,
                    ContentType = file.ContentType,
                    FileSize = file.Length,
                    Content = stream
                };

                var result = await _documentService.UploadAsync(uploadDto);
                if (!result.IsSuccess)
                {
                    return Json(new { success = false, message = result.ErrorMessage });
                }

                return Json(new { success = true, message = "Tải tệp lên thành công và bắt đầu trích xuất.", data = result.Data });
            }
            catch (Exception ex)
            {
                return Json(new { success = false, message = $"Lỗi hệ thống khi tải tệp: {ex.Message}" });
            }
        }
    }
}
