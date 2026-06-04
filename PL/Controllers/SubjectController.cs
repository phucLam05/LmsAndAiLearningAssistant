using BLL.Interfaces;
using Core.DTOs.Subject;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using System;
using System.Security.Claims;
using System.Threading.Tasks;

namespace PL.Controllers
{
    /// <summary>
    /// Controller for Subject management with strict RBAC enforcement.
    /// - Admin: Full CRUD + AssignLecturer
    /// - Lecturer: Read-only view of assigned subjects
    /// - Student: Browse active subjects to select for chat
    /// </summary>
    [Authorize]
    public class SubjectController : Controller
    {
        private readonly ISubjectService _subjectService;

        public SubjectController(ISubjectService subjectService)
        {
            _subjectService = subjectService;
        }

        // ── ADMIN ACTIONS ─────────────────────────────────────────────────────────

        /// <summary>Admin: List all subjects.</summary>
        [Authorize(Roles = "Admin")]
        public async Task<IActionResult> Index()
        {
            var subjects = await _subjectService.GetAllSubjectsAsync();
            return View(subjects);
        }

        /// <summary>Admin: Show create form.</summary>
        [Authorize(Roles = "Admin")]
        [HttpGet]
        public IActionResult Create()
        {
            return View();
        }

        /// <summary>Admin: Handle create form submission.</summary>
        [Authorize(Roles = "Admin")]
        [HttpPost]
        [ValidateAntiForgeryToken]
        public async Task<IActionResult> Create(CreateSubjectDto dto)
        {
            if (!ModelState.IsValid)
                return View(dto);

            var (success, error) = await _subjectService.CreateSubjectAsync(dto);

            if (!success)
            {
                ModelState.AddModelError(string.Empty, error ?? "Failed to create subject.");
                return View(dto);
            }

            TempData["SuccessMessage"] = "Subject created successfully.";
            return RedirectToAction(nameof(Index));
        }

        /// <summary>Admin: Show edit form.</summary>
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

            return View(dto);
        }

        /// <summary>Admin: Handle edit form submission.</summary>
        [Authorize(Roles = "Admin")]
        [HttpPost]
        [ValidateAntiForgeryToken]
        public async Task<IActionResult> Edit(UpdateSubjectDto dto)
        {
            if (!ModelState.IsValid)
                return View(dto);

            var (success, error) = await _subjectService.UpdateSubjectAsync(dto);

            if (!success)
            {
                ModelState.AddModelError(string.Empty, error ?? "Failed to update subject.");
                return View(dto);
            }

            TempData["SuccessMessage"] = "Subject updated successfully.";
            return RedirectToAction(nameof(Index));
        }

        /// <summary>Admin: Assign or remove a lecturer from a subject.</summary>
        [Authorize(Roles = "Admin")]
        [HttpPost]
        [ValidateAntiForgeryToken]
        public async Task<IActionResult> AssignLecturer(AssignLecturerDto dto)
        {
            var (success, error) = await _subjectService.AssignLecturerAsync(dto);

            if (!success)
                TempData["ErrorMessage"] = error ?? "Failed to assign lecturer.";
            else
                TempData["SuccessMessage"] = dto.LecturerId.HasValue
                    ? "Lecturer assigned successfully."
                    : "Lecturer removed from subject.";

            return RedirectToAction(nameof(Index));
        }

        /// <summary>Admin: Delete a subject.</summary>
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

        // ── LECTURER ACTIONS ──────────────────────────────────────────────────────

        /// <summary>Lecturer: View only the subjects assigned to the current lecturer.</summary>
        [Authorize(Roles = "Lecturer")]
        public async Task<IActionResult> MySubjects()
        {
            var lecturerId = Guid.Parse(User.FindFirstValue(ClaimTypes.NameIdentifier)!);
            var subjects = await _subjectService.GetSubjectsByLecturerAsync(lecturerId);
            return View(subjects);
        }

        // ── STUDENT ACTIONS ───────────────────────────────────────────────────────

        /// <summary>Student: Browse all active subjects to select one for AI chat.</summary>
        [Authorize(Roles = "Student")]
        public async Task<IActionResult> Browse()
        {
            var subjects = await _subjectService.GetActiveSubjectsAsync();
            return View(subjects);
        }
    }
}
