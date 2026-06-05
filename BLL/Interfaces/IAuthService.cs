using Core.DTOs.Auth;
using Core.Entities;
using System.Threading.Tasks;

namespace BLL.Interfaces
{
    /// <summary>
    /// Service interface for authentication operations.
    /// Handles business logic for user registration and login.
    /// </summary>
    public interface IAuthService
    {
        /// <summary>
        /// Registers a new user based on the provided DTO.
        /// </summary>
        /// <param name="registerDto">The registration data.</param>
        /// <returns>A tuple containing a boolean success flag and an error message if any.</returns>
        Task<(bool Success, string ErrorMessage)> RegisterAsync(RegisterDto registerDto);

        Task<User?> LoginAsync(LoginDto loginDto);

        string DecryptEmail(string encryptedEmailBase64);
        Task<System.Collections.Generic.List<User>> GetAllUsersAsync();
        Task<(bool Success, string ErrorMessage)> UpdateUserAsync(Guid id, string fullName, string email, UserRole role);
        Task<bool> DeleteUserAsync(Guid id);
    }
}
