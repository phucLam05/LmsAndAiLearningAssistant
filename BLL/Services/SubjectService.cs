using BLL.Interfaces;
using Core.DTOs.Common;
using Core.Entities;
using DAL.Data;
using Microsoft.EntityFrameworkCore;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading.Tasks;

namespace BLL.Services
{
    public class SubjectService : ISubjectService
    {
        private readonly ApplicationDbContext _dbContext;

        public SubjectService(ApplicationDbContext dbContext)
        {
            _dbContext = dbContext;
        }

        public async Task<IEnumerable<Subject>> GetAllSubjectsAsync()
        {
            return await _dbContext.Subjects
                .Include(s => s.Lecturer)
                .Include(s => s.Documents)
                .OrderBy(s => s.SubjectCode)
                .ToListAsync();
        }

        public async Task<IEnumerable<Subject>> GetSubjectsByLecturerAsync(Guid lecturerId)
        {
            return await _dbContext.Subjects
                .Include(s => s.Documents)
                .Where(s => s.LecturerId == lecturerId)
                .OrderBy(s => s.SubjectCode)
                .ToListAsync();
        }

        public async Task<IEnumerable<Subject>> GetActiveSubjectsAsync()
        {
            return await _dbContext.Subjects
                .Include(s => s.Documents)
                .Where(s => s.Status == SubjectStatus.Active)
                .OrderBy(s => s.SubjectCode)
                .ToListAsync();
        }

        public async Task<Subject?> GetSubjectByIdAsync(Guid id)
        {
            return await _dbContext.Subjects
                .Include(s => s.Lecturer)
                .Include(s => s.Documents)
                .FirstOrDefaultAsync(s => s.Id == id);
        }

        public async Task<Result<Subject>> CreateSubjectAsync(string code, string name, string? description, Guid? lecturerId)
        {
            var normalizedCode = code.Trim().ToUpperInvariant();
            var exists = await _dbContext.Subjects.AnyAsync(s => s.SubjectCode == normalizedCode);
            if (exists)
            {
                return Result<Subject>.Failure("Subject code already exists.");
            }

            var subject = new Subject
            {
                Id = Guid.NewGuid(),
                SubjectCode = normalizedCode,
                Name = name.Trim(),
                Description = description?.Trim(),
                LecturerId = lecturerId,
                Status = SubjectStatus.Active,
                CreatedAt = DateTime.UtcNow,
                UpdatedAt = DateTime.UtcNow
            };

            await _dbContext.Subjects.AddAsync(subject);
            await _dbContext.SaveChangesAsync();

            return Result<Subject>.Success(subject);
        }

        public async Task<Result> UpdateSubjectAsync(Guid id, string code, string name, string? description, Guid? lecturerId, SubjectStatus status, Guid updatedBy)
        {
            var subject = await _dbContext.Subjects.FindAsync(id);
            if (subject == null)
            {
                return Result.Failure("Subject not found.");
            }

            var normalizedCode = code.Trim().ToUpperInvariant();
            var codeExists = await _dbContext.Subjects.AnyAsync(s => s.SubjectCode == normalizedCode && s.Id != id);
            if (codeExists)
            {
                return Result.Failure("Subject code already exists on another subject.");
            }

            subject.SubjectCode = normalizedCode;
            subject.Name = name.Trim();
            subject.Description = description?.Trim();
            subject.LecturerId = lecturerId;
            subject.Status = status;
            subject.UpdatedBy = updatedBy;
            subject.UpdatedAt = DateTime.UtcNow;

            _dbContext.Subjects.Update(subject);
            await _dbContext.SaveChangesAsync();

            return Result.Success();
        }

        public async Task<Result> DeleteSubjectAsync(Guid id)
        {
            var subject = await _dbContext.Subjects.FindAsync(id);
            if (subject == null)
            {
                return Result.Failure("Subject not found.");
            }

            _dbContext.Subjects.Remove(subject);
            await _dbContext.SaveChangesAsync();

            return Result.Success();
        }
    }
}
