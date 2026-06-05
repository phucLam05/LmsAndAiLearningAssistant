using BLL.Interfaces;
using Core.DTOs.Subject;
using Core.DTOs.Common;
using Core.Entities;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using Microsoft.AspNetCore.Mvc.Rendering;
using System;
using System.Linq;
using System.Security.Claims;
using System.Threading.Tasks;

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
            return View(subjects);
        }

        [Authorize(Roles = "Admin")]
        [HttpGet]
        public async Task<IActionResult> Create()
        {
            var users = await _adminService.GetAllUsersAsync();
            var lecturers = users.Where(u => u.Role == UserRole.Lecturer).ToList();
            ViewBag.Lecturers = new SelectList(lecturers, "Id", "FullName");
            return View();
        }

        [Authorize(Roles = "Admin")]
        [HttpPost]
        [ValidateAntiForgeryToken]
        public async Task<IActionResult> Create(CreateSubjectDto dto)
        {
            if (!ModelState.IsValid)
            {
                var users = await _adminService.GetAllUsersAsync();
                var lecturers = users.Where(u => u.Role == UserRole.Lecturer).ToList();
                ViewBag.Lecturers = new SelectList(lecturers, "Id", "FullName", dto.LecturerId);
                return View(dto);
            }

            var (success, error) = await _subjectService.CreateSubjectAsync(dto);
            if (!success)
            {
                ModelState.AddModelError(string.Empty, error ?? "Failed to create subject.");
                var users = await _adminService.GetAllUsersAsync();
                var lecturers = users.Where(u => u.Role == UserRole.Lecturer).ToList();
                ViewBag.Lecturers = new SelectList(lecturers, "Id", "FullName", dto.LecturerId);
                return View(dto);
            }

            TempData["SuccessMessage"] = "Subject created successfully.";
            return RedirectToAction(nameof(Index));
        }

        [Authorize(Roles = "Admin")]
        [HttpGet]
        public async Task<IActionResult> Edit(Guid id)
        {
            var subject = await _subjectService.GetSubjectByIdAsync(id);
            if (subject == null)
            {
                TempData["ErrorMessage"] = "Subject not found.";
                return RedirectToAction(nameof(Index));
            }

            var dto = new UpdateSubjectDto
            {
                Id = subject.Id,
                Name = subject.Name,
                Description = subject.Description,
                LecturerId = subject.LecturerId,
                Status = subject.Status
            };

            var users = await _adminService.GetAllUsersAsync();
            var lecturers = users.Where(u => u.Role == UserRole.Lecturer).ToList();
            ViewBag.Lecturers = new SelectList(lecturers, "Id", "FullName", dto.LecturerId);
            return View(dto);
        }

        [Authorize(Roles = "Admin")]
        [HttpPost]
        [ValidateAntiForgeryToken]
        public async Task<IActionResult> Edit(UpdateSubjectDto dto)
        {
            if (!ModelState.IsValid)
            {
                var users = await _adminService.GetAllUsersAsync();
                var lecturers = users.Where(u => u.Role == UserRole.Lecturer).ToList();
                ViewBag.Lecturers = new SelectList(lecturers, "Id", "FullName", dto.LecturerId);
                return View(dto);
            }

            var (success, error) = await _subjectService.UpdateSubjectAsync(dto);
            if (!success)
            {
                ModelState.AddModelError(string.Empty, error ?? "Failed to update subject.");
                var users = await _adminService.GetAllUsersAsync();
                var lecturers = users.Where(u => u.Role == UserRole.Lecturer).ToList();
                ViewBag.Lecturers = new SelectList(lecturers, "Id", "FullName", dto.LecturerId);
                return View(dto);
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
            return View(subjects);
        }

        [Authorize(Roles = "Student")]
        public async Task<IActionResult> Browse()
        {
            var subjects = await _subjectService.GetActiveSubjectsAsync();
            return View(subjects);
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
        public async Task<IActionResult> SendMessage(Guid subjectId, string message)
        {
            if (string.IsNullOrWhiteSpace(message))
                return Json(new { success = false, message = "Message cannot be empty." });

            var answer = await _chatService.ChatWithSubjectAsync(subjectId, message);
            return Json(new { success = true, reply = answer });
        }
    }
}
