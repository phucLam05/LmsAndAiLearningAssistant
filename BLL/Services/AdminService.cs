using BLL.Interfaces;
using Core.DTOs.Admin;
using Core.Entities;
using DAL.Data;
using Microsoft.EntityFrameworkCore;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading.Tasks;

namespace BLL.Services
{
    public class AdminService : IAdminService
    {
        private readonly ApplicationDbContext _dbContext;

        public AdminService(ApplicationDbContext dbContext)
        {
            _dbContext = dbContext;
        }

        public async Task<bool> ChangeUserRoleAsync(Guid userId, UserRole newRole)
        {
            var user = await _dbContext.Users.FindAsync(userId);
            if (user == null)
            {
                return false;
            }

            user.Role = newRole;
            await _dbContext.SaveChangesAsync();
            return true;
        }

        public async Task<IEnumerable<User>> GetAllUsersAsync()
        {
            return await _dbContext.Users
                .OrderByDescending(u => u.CreatedAt)
                .ToListAsync();
        }

        public async Task<DashboardStatsDto> GetDashboardStatsAsync()
        {
            var totalUsers = await _dbContext.Users.CountAsync();
            var totalDocuments = await _dbContext.Documents.CountAsync();
            var totalStorage = 0L;
            var totalChunks = await _dbContext.DocumentChunks.CountAsync();

            return new DashboardStatsDto
            {
                TotalUsers = totalUsers,
                TotalDocuments = totalDocuments,
                TotalStorageUsedBytes = totalStorage,
                TotalDocumentChunks = totalChunks
            };
        }
    }
}
