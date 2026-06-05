using Core.DTOs.Common;
using Core.Entities;
using System;
using System.Collections.Generic;
using System.Threading.Tasks;

namespace BLL.Interfaces
{
    public interface ISubjectService
    {
        Task<IEnumerable<Subject>> GetAllSubjectsAsync();
        
        Task<IEnumerable<Subject>> GetSubjectsByLecturerAsync(Guid lecturerId);
        
        Task<IEnumerable<Subject>> GetActiveSubjectsAsync();
        
        Task<Subject?> GetSubjectByIdAsync(Guid id);
        
        Task<Result<Subject>> CreateSubjectAsync(string code, string name, string? description, Guid? lecturerId);
        
        Task<Result> UpdateSubjectAsync(Guid id, string code, string name, string? description, Guid? lecturerId, SubjectStatus status, Guid updatedBy);
        
        Task<Result> DeleteSubjectAsync(Guid id);
    }
}
