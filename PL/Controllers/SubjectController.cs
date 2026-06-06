using BLL.Interfaces;
using Core.DTOs.Subject;
using Core.Entities;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using Microsoft.AspNetCore.Mvc.Rendering;
using System.Security.Claims;

namespace PL.Controllers
{
    [Authorize]
    public class SubjectController : Controller
    {
        private readonly ISubjectService _subjectService;
        private readonly IDocumentService _documentService;
        private readonly IAdminService _adminService;
        private readonly IChatService _chatService;

        public SubjectController(
            ISubjectService subjectService,
            IDocumentService documentService,
            IAdminService adminService,
            IChatService chatService)
        {
            _subjectService = subjectService;
            _documentService = documentService;
            _adminService = adminService;
            _chatService = chatService;
        }

        private Guid GetUserId()
        {
            var userIdString = User.FindFirstValue(ClaimTypes.NameIdentifier);
            return Guid.TryParse(userIdString, out var userId) ? userId : Guid.Empty;
        }

        private UserRole GetUserRole()
        {
            var roleString = User.FindFirstValue(ClaimTypes.Role);
            return Enum.TryParse<UserRole>(roleString, out var role) ? role : UserRole.Student;
        }

        [Authorize(Roles = "Admin")]
        public async Task<IActionResult> Index()
        {
            ViewBag.UserRole = GetUserRole();
            var subjects = await _subjectService.GetAllSubjectsAsync();
            
            var users = await _adminService.GetAllUsersAsync();
            var lecturers = users.Where(u => u.Role == UserRole.Lecturer).ToList();
            ViewBag.Lecturers = new SelectList(lecturers, "Id", "FullName");
            
            return View(subjects);
        }

        [Authorize(Roles = "Admin")]
        [HttpGet]
        public IActionResult Create()
        {
            return RedirectToAction(nameof(Index));
        }

        [Authorize(Roles = "Admin")]
        [HttpPost]
        [ValidateAntiForgeryToken]
        public async Task<IActionResult> Create(CreateSubjectDto dto)
        {
            var isAjax = Request.Headers["X-Requested-With"] == "XMLHttpRequest";

            if (!ModelState.IsValid)
            {
                var errors = string.Join(" ", ModelState.Values.SelectMany(v => v.Errors).Select(e => e.ErrorMessage));
                if (isAjax)
                {
                    return Json(new { success = false, error = errors });
                }

                var users = await _adminService.GetAllUsersAsync();
                var lecturers = users.Where(u => u.Role == UserRole.Lecturer).ToList();
                ViewBag.Lecturers = new SelectList(lecturers, "Id", "FullName", dto.LecturerId);
                return View(dto);
            }

            var (success, error) = await _subjectService.CreateSubjectAsync(dto);
            if (!success)
            {
                if (isAjax)
                {
                    return Json(new { success = false, error = error ?? "Failed to create subject." });
                }

                ModelState.AddModelError(string.Empty, error ?? "Failed to create subject.");
                var users = await _adminService.GetAllUsersAsync();
                var lecturers = users.Where(u => u.Role == UserRole.Lecturer).ToList();
                ViewBag.Lecturers = new SelectList(lecturers, "Id", "FullName", dto.LecturerId);
                return View(dto);
            }

            if (isAjax)
            {
                TempData["SuccessMessage"] = "Môn học đã được tạo thành công.";
                return Json(new { success = true });
            }

            TempData["SuccessMessage"] = "Subject created successfully.";
            return RedirectToAction(nameof(Index));
        }

        [Authorize(Roles = "Admin")]
        [HttpGet]
        public IActionResult Edit(Guid id)
        {
            return RedirectToAction(nameof(Index));
        }

        [Authorize(Roles = "Admin")]
        [HttpPost]
        [ValidateAntiForgeryToken]
        public async Task<IActionResult> Edit(UpdateSubjectDto dto)
        {
            var isAjax = Request.Headers["X-Requested-With"] == "XMLHttpRequest";

            if (!ModelState.IsValid)
            {
                var errors = string.Join(" ", ModelState.Values.SelectMany(v => v.Errors).Select(e => e.ErrorMessage));
                if (isAjax)
                {
                    return Json(new { success = false, error = errors });
                }

                var subject = await _subjectService.GetSubjectByIdAsync(dto.Id);
                if (subject != null)
                {
                    dto.SubjectCode = subject.SubjectCode;
                }

                var users = await _adminService.GetAllUsersAsync();
                var lecturers = users.Where(u => u.Role == UserRole.Lecturer).ToList();
                ViewBag.Lecturers = new SelectList(lecturers, "Id", "FullName", dto.LecturerId);
                return View(dto);
            }

            var (success, error) = await _subjectService.UpdateSubjectAsync(dto);
            if (!success)
            {
                if (isAjax)
                {
                    return Json(new { success = false, error = error ?? "Failed to update subject." });
                }

                var subject = await _subjectService.GetSubjectByIdAsync(dto.Id);
                if (subject != null)
                {
                    dto.SubjectCode = subject.SubjectCode;
                }

                ModelState.AddModelError(string.Empty, error ?? "Failed to update subject.");
                var users = await _adminService.GetAllUsersAsync();
                var lecturers = users.Where(u => u.Role == UserRole.Lecturer).ToList();
                ViewBag.Lecturers = new SelectList(lecturers, "Id", "FullName", dto.LecturerId);
                return View(dto);
            }

            if (isAjax)
            {
                TempData["SuccessMessage"] = "Môn học đã được cập nhật thành công.";
                return Json(new { success = true });
            }

            TempData["SuccessMessage"] = "Subject updated successfully.";
            return RedirectToAction(nameof(Index));
        }

        [Authorize(Roles = "Admin")]
        [HttpPost]
        [ValidateAntiForgeryToken]
        public async Task<IActionResult> AssignLecturer(AssignLecturerDto dto)
        {
            var (success, error) = await _subjectService.AssignLecturerAsync(dto);
            if (!success)
                TempData["ErrorMessage"] = error ?? "Failed to assign lecturer.";
            else
                TempData["SuccessMessage"] = dto.LecturerId.HasValue ? "Lecturer assigned successfully." : "Lecturer removed from subject.";
            return RedirectToAction(nameof(Index));
        }

        [Authorize(Roles = "Admin")]
        [HttpPost]
        [ValidateAntiForgeryToken]
        public async Task<IActionResult> Delete(Guid id)
        {
            var (success, error) = await _subjectService.DeleteSubjectAsync(id);
            if (!success)
                TempData["ErrorMessage"] = error ?? "Failed to delete subject.";
            else
                TempData["SuccessMessage"] = "Subject deleted successfully.";
            return RedirectToAction(nameof(Index));
        }

        [Authorize(Roles = "Lecturer")]
        public async Task<IActionResult> MySubjects()
        {
            var lecturerId = Guid.Parse(User.FindFirstValue(ClaimTypes.NameIdentifier)!);
            var subjects = await _subjectService.GetSubjectsByLecturerAsync(lecturerId);
            ViewBag.UserRole = GetUserRole();
            return View("Index", subjects);
        }

        [Authorize(Roles = "Student")]
        public async Task<IActionResult> Browse()
        {
            var subjects = await _subjectService.GetActiveSubjectsAsync();
            ViewBag.UserRole = GetUserRole();
            return View("Index", subjects);
        }

        public async Task<IActionResult> Details(Guid id)
        {
            var subject = await _subjectService.GetSubjectByIdAsync(id);
            if (subject == null)
                return NotFound();

            var role = GetUserRole();
            ViewBag.UserRole = role;

            if (role == UserRole.Student)
                return RedirectToAction(nameof(Chat), new { subjectId = id });

            var documents = await _documentService.GetDocumentsBySubjectIdAsync(id);
            ViewBag.Documents = documents;
            return View(subject); 
        }

        [HttpGet]
        public async Task<IActionResult> Chat(Guid subjectId)
        {
            var subject = await _subjectService.GetSubjectByIdAsync(subjectId);
            if (subject == null)
                return NotFound();
            return View(subject);
        }

        [HttpPost]
        [ValidateAntiForgeryToken]
        public async Task<IActionResult> SendMessage(Guid subjectId, string message, string? model = null, string? documentIds = null)
        {
            if (string.IsNullOrWhiteSpace(message))
                return Json(new { success = false, message = "Message cannot be empty." });

            // Parse comma-separated document IDs sent from the chat UI
            List<Guid>? selectedDocIds = null;
            if (!string.IsNullOrWhiteSpace(documentIds))
            {
                selectedDocIds = documentIds
                    .Split(',', StringSplitOptions.RemoveEmptyEntries | StringSplitOptions.TrimEntries)
                    .Select(s => Guid.TryParse(s, out var g) ? g : (Guid?)null)
                    .Where(g => g.HasValue)
                    .Select(g => g!.Value)
                    .ToList();
            }

            var answer = await _chatService.ChatWithSubjectAsync(subjectId, message, model, selectedDocIds);
            return Json(new { success = true, reply = answer });
        }
    }
}
