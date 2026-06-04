using Core.Entities;
using DAL.Data;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.DependencyInjection;
using System;
using System.Security.Cryptography;
using System.Text;
using System.Threading.Tasks;

namespace PL
{
    public static class DbSeeder
    {
        public static async Task SeedAdminUserAsync(IServiceProvider serviceProvider)
        {
            var dbContext = serviceProvider.GetRequiredService<ApplicationDbContext>();
            var configuration = serviceProvider.GetRequiredService<IConfiguration>();

            var adminEmail = "admin@lmsai.com";
            var adminPassword = "Admin123!";
            var encryptionKey = configuration["Security:EncryptionKey"] ?? "FallbackKeyForDevExactly32Bytes!";

            var emailHash = HashEmail(adminEmail);

            // Check if admin user already exists in DB
            var existingUser = await dbContext.Users.FirstOrDefaultAsync(u => u.EmailHash == emailHash);

            if (existingUser == null)
            {
                var passwordHash = BCrypt.Net.BCrypt.HashPassword(adminPassword);
                var emailEncrypt = EncryptEmail(adminEmail, encryptionKey);

                var adminUser = new User
                {
                    Id = Guid.NewGuid(),
                    UserCode = "ADMIN001",
                    FullName = "System Admin",
                    EmailHash = emailHash,
                    EmailEncrypt = emailEncrypt,
                    PasswordHash = passwordHash,
                    Role = UserRole.Admin,
                    Status = UserStatus.Active,
                    CreatedAt = DateTime.UtcNow,
                    UpdatedAt = DateTime.UtcNow
                };

                await dbContext.Users.AddAsync(adminUser);
                await dbContext.SaveChangesAsync();
            }
            else
            {
                // If user exists, verify they have the admin role and valid user code
                bool updated = false;

                if (existingUser.Role != UserRole.Admin)
                {
                    existingUser.Role = UserRole.Admin;
                    updated = true;
                }

                if (existingUser.Status != UserStatus.Active)
                {
                    existingUser.Status = UserStatus.Active;
                    updated = true;
                }

                if (string.IsNullOrWhiteSpace(existingUser.UserCode) || existingUser.UserCode == "string" || existingUser.UserCode == "")
                {
                    existingUser.UserCode = "ADMIN001";
                    updated = true;
                }

                if (updated)
                {
                    existingUser.UpdatedAt = DateTime.UtcNow;
                    dbContext.Users.Update(existingUser);
                    await dbContext.SaveChangesAsync();
                }
            }
        }

        private static string HashEmail(string email)
        {
            using (var sha256 = SHA256.Create())
            {
                var bytes = Encoding.UTF8.GetBytes(email.Trim().ToLowerInvariant());
                var hashBytes = sha256.ComputeHash(bytes);
                return Convert.ToHexString(hashBytes).ToLowerInvariant();
            }
        }

        private static string EncryptEmail(string email, string encryptionKey)
        {
            using (var aes = Aes.Create())
            {
                aes.Key = Encoding.UTF8.GetBytes(encryptionKey);
                aes.GenerateIV();
                
                var encryptor = aes.CreateEncryptor(aes.Key, aes.IV);
                var emailBytes = Encoding.UTF8.GetBytes(email.Trim().ToLowerInvariant());
                var encryptedBytes = encryptor.TransformFinalBlock(emailBytes, 0, emailBytes.Length);

                var resultBytes = new byte[aes.IV.Length + encryptedBytes.Length];
                Buffer.BlockCopy(aes.IV, 0, resultBytes, 0, aes.IV.Length);
                Buffer.BlockCopy(encryptedBytes, 0, resultBytes, aes.IV.Length, encryptedBytes.Length);

                return Convert.ToBase64String(resultBytes);
            }
        }
    }
}
