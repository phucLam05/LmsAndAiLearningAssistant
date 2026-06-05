using BLL.Interfaces;
using Core.DTOs.Common;
using Core.DTOs.Documents;
using Core.Entities;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using Microsoft.AspNetCore.Mvc.Rendering;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Security.Claims;
using System.Threading.Tasks;

namespace PL.Controllers
{
    /// <summary>
    /// Controller for managing Subjects, listing documents, and interacting with the RAG Chatbot.
    /// </summary>
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

        public async Task<IActionResult> Index()
        {
            var role = GetUserRole();
            var userId = GetUserId();
            IEnumerable<Subject> subjects;

            if (role == UserRole.Admin)
            {
                subjects = await _subjectService.GetAllSubjectsAsync();
            }
            else if (role == UserRole.Lecturer)
            {
                subjects = await _subjectService.GetSubjectsByLecturerAsync(userId);
            }
            else // Student
            {
                subjects = await _subjectService.GetActiveSubjectsAsync();
            }

            ViewBag.UserRole = role;
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
        public async Task<IActionResult> Create(string subjectCode, string name, string? description, Guid? lecturerId)
        {
            if (string.IsNullOrWhiteSpace(subjectCode) || string.IsNullOrWhiteSpace(name))
            {
                ModelState.AddModelError(string.Empty, "Subject Code and Name are required.");
                var users = await _adminService.GetAllUsersAsync();
                var lecturers = users.Where(u => u.Role == UserRole.Lecturer).ToList();
                ViewBag.Lecturers = new SelectList(lecturers, "Id", "FullName", lecturerId);
                return View();
            }

            var result = await _subjectService.CreateSubjectAsync(subjectCode, name, description, lecturerId);
            if (!result.IsSuccess)
            {
                ModelState.AddModelError(string.Empty, result.ErrorMessage);
                var users = await _adminService.GetAllUsersAsync();
                var lecturers = users.Where(u => u.Role == UserRole.Lecturer).ToList();
                ViewBag.Lecturers = new SelectList(lecturers, "Id", "FullName", lecturerId);
                return View();
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
                return NotFound();
            }

            var users = await _adminService.GetAllUsersAsync();
            var lecturers = users.Where(u => u.Role == UserRole.Lecturer).ToList();
            ViewBag.Lecturers = new SelectList(lecturers, "Id", "FullName", subject.LecturerId);
            return View(subject);
        }

        [Authorize(Roles = "Admin")]
        [HttpPost]
        [ValidateAntiForgeryToken]
        public async Task<IActionResult> Edit(Guid id, string subjectCode, string name, string? description, Guid? lecturerId, SubjectStatus status)
        {
            if (string.IsNullOrWhiteSpace(subjectCode) || string.IsNullOrWhiteSpace(name))
            {
                ModelState.AddModelError(string.Empty, "Subject Code and Name are required.");
                var users = await _adminService.GetAllUsersAsync();
                var lecturers = users.Where(u => u.Role == UserRole.Lecturer).ToList();
                ViewBag.Lecturers = new SelectList(lecturers, "Id", "FullName", lecturerId);
                return View();
            }

            var userId = GetUserId();
            var result = await _subjectService.UpdateSubjectAsync(id, subjectCode, name, description, lecturerId, status, userId);
            if (!result.IsSuccess)
            {
                ModelState.AddModelError(string.Empty, result.ErrorMessage);
                var users = await _adminService.GetAllUsersAsync();
                var lecturers = users.Where(u => u.Role == UserRole.Lecturer).ToList();
                ViewBag.Lecturers = new SelectList(lecturers, "Id", "FullName", lecturerId);
                return View();
            }

            TempData["SuccessMessage"] = "Subject updated successfully.";
            return RedirectToAction(nameof(Index));
        }

        [Authorize(Roles = "Admin")]
        [HttpPost]
        [ValidateAntiForgeryToken]
        public async Task<IActionResult> Delete(Guid id)
        {
            var result = await _subjectService.DeleteSubjectAsync(id);
            if (!result.IsSuccess)
            {
                TempData["ErrorMessage"] = result.ErrorMessage;
            }
            else
            {
                TempData["SuccessMessage"] = "Subject deleted successfully.";
            }
            return RedirectToAction(nameof(Index));
        }

        public async Task<IActionResult> Details(Guid id)
        {
            var subject = await _subjectService.GetSubjectByIdAsync(id);
            if (subject == null)
            {
                return NotFound();
            }

            var role = GetUserRole();
            ViewBag.UserRole = role;

            if (role == UserRole.Student)
            {
                return RedirectToAction(nameof(Chat), new { subjectId = id });
            }

            var documents = await _documentService.GetDocumentsBySubjectIdAsync(id);
            ViewBag.Documents = documents;
            return View(subject);
        }

        [HttpGet]
        public async Task<IActionResult> Chat(Guid subjectId)
        {
            var subject = await _subjectService.GetSubjectByIdAsync(subjectId);
            if (subject == null)
            {
                return NotFound();
            }

            return View(subject);
        }

        [HttpPost]
        [ValidateAntiForgeryToken]
        public async Task<IActionResult> SendMessage(Guid subjectId, string message)
        {
            if (string.IsNullOrWhiteSpace(message))
            {
                return Json(new { success = false, message = "Message cannot be empty." });
            }

            var answer = await _chatService.ChatWithSubjectAsync(subjectId, message);
            return Json(new { success = true, reply = answer });
        }
    }
}
