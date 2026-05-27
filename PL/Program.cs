using BLL.Interfaces;
using BLL.Services;
using DAL.Data;
using DAL.Interfaces;
using DAL.Repositories;
using Microsoft.AspNetCore.Authentication.Cookies;
using Microsoft.EntityFrameworkCore;
using Hangfire;
using Hangfire.PostgreSql;

namespace PL
{
    public class Program
    {
        public static void Main(string[] args)
        {
            var builder = WebApplication.CreateBuilder(args);

            // Add services to the container.
            builder.Services.AddControllersWithViews();
            builder.Services.AddDbContext<DAL.Data.ApplicationDbContext>(options =>
                options.UseNpgsql(builder.Configuration.GetConnectionString("DefaultConnection"), o => o.UseVector()));
            builder.Services.AddScoped<DAL.Interfaces.IUserRepository, DAL.Repositories.UserRepository>();
            builder.Services.AddScoped<DAL.Interfaces.IDocumentRepository, DAL.Repositories.DocumentRepository>();
            builder.Services.AddScoped<BLL.Interfaces.IAuthService, BLL.Services.AuthService>();
            builder.Services.AddScoped<BLL.Interfaces.IChunkingService, BLL.Services.ChunkingService>();
            
            // Register HttpClientFactory for BLL services (e.g. ChunkingService reading from Supabase)
            builder.Services.AddHttpClient("Supabase", client =>
            {
                var supabaseKey = builder.Configuration["Supabase:Key"];
                if (!string.IsNullOrEmpty(supabaseKey))
                {
                    client.DefaultRequestHeaders.Add("apikey", supabaseKey);
                    client.DefaultRequestHeaders.Add("Authorization", $"Bearer {supabaseKey}");
                }
            });

            builder.Services.AddAuthentication(CookieAuthenticationDefaults.AuthenticationScheme)
                .AddCookie(options =>
                {
                    options.LoginPath = "/Auth/Login";
                    options.LogoutPath = "/Auth/Logout";
                    options.AccessDeniedPath = "/Auth/Login";
                });

            builder.Services.AddHangfire(configuration => configuration
                .SetDataCompatibilityLevel(CompatibilityLevel.Version_180)
                .UseSimpleAssemblyNameTypeSerializer()
                .UseRecommendedSerializerSettings()
                .UsePostgreSqlStorage(o => o.UseNpgsqlConnection(builder.Configuration.GetConnectionString("DefaultConnection"))));

            builder.Services.AddHangfireServer();

            var app = builder.Build();

            // Configure the HTTP request pipeline.
            if (!app.Environment.IsDevelopment())
            {
                app.UseExceptionHandler("/Home/Error");
                // The default HSTS value is 30 days. You may want to change this for production scenarios, see https://aka.ms/aspnetcore-hsts.
                app.UseHsts();
            }

            app.UseHttpsRedirection();
            app.UseRouting();

            app.UseAuthentication();
            app.UseAuthorization();

            app.UseHangfireDashboard("/hangfire");

            app.MapStaticAssets();
            app.MapControllerRoute(
                name: "default",
                pattern: "{controller=Home}/{action=Index}/{id?}")
                .WithStaticAssets();

            app.Run();
        }
    }
}
