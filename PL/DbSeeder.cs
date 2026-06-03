using BLL.Interfaces;
using Core.DTOs.Auth;
using Core.Entities;
using DAL.Data;

namespace PL
{
    public static class DbSeeder
    {
        public static async Task SeedAdminUserAsync(IServiceProvider serviceProvider)
        {
            var authService = serviceProvider.GetRequiredService<IAuthService>();
            var dbContext = serviceProvider.GetRequiredService<ApplicationDbContext>();

            var adminEmail = "admin@lmsai.com";
            var adminPassword = "Admin123!";

            var registerDto = new RegisterDto
            {
                Email = adminEmail,
                FullName = "System Admin",
                Password = adminPassword,
                ConfirmPassword = adminPassword
            };

            var (success, _) = await authService.RegisterAsync(registerDto);

            var loginDto = new LoginDto { Email = adminEmail, Password = adminPassword };
            var adminUser = await authService.LoginAsync(loginDto);

            if (adminUser != null)
            {
                // Ensure they have the admin role
                if (adminUser.Role != UserRole.Admin)
                {
                    // Update in DB
                    var dbUser = await dbContext.Users.FindAsync(adminUser.Id);
                    if (dbUser != null)
                    {
                        dbUser.Role = UserRole.Admin;
                        await dbContext.SaveChangesAsync();
                    }
                }
            }
        }
    }
}
