using Core.Entities;
using System;
using System.Threading.Tasks;

namespace DAL.Interfaces
{
    public interface ISubjectRepository
    {
        Task<Subject?> GetByIdAsync(Guid id);
    }
}
