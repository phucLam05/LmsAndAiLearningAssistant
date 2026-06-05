using Core.Entities;
using System.Threading.Tasks;

namespace DAL.Interfaces
{
    /// <summary>
    /// Interface for the User repository.
    /// Provides abstractions for data access operations related to the User entity.
    /// </summary>
    public interface IUserRepository
    {
        /// <summary>
        /// Retrieves a user by their hashed email.
        /// </summary>
        /// <param name="emailHash">The hashed email address.</param>
        /// <returns>The User if found, otherwise null.</returns>
        Task<User?> GetUserByEmailHashAsync(string emailHash);

        /// <summary>
        /// Adds a new user to the database.
        /// </summary>
        /// <param name="user">The user entity to add.</param>
        /// <returns>The created user.</returns>
        Task<User> AddUserAsync(User user);

        /// <summary>
        /// Retrieves a user by their ID.
        /// </summary>
        Task<User?> GetUserByIdAsync(Guid id);

        /// <summary>
        /// Updates a user's details.
        /// </summary>
        Task UpdateUserAsync(User user);

        /// <summary>
        /// Deletes a user.
        /// </summary>
        Task DeleteUserAsync(User user);

        /// <summary>
        /// Retrieves all users in the system.
        /// </summary>
        Task<System.Collections.Generic.List<User>> GetAllUsersAsync();
    }
}
