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
            builder.Services.Configure<Core.Configuration.UploadOptions>(builder.Configuration.GetSection("Upload"));
            builder.Services.AddControllersWithViews();
            builder.Services.AddDbContext<DAL.Data.ApplicationDbContext>(options =>
                options.UseNpgsql(builder.Configuration.GetConnectionString("DefaultConnection"), o => o.UseVector()));
            builder.Services.AddScoped<DAL.Interfaces.IUserRepository, DAL.Repositories.UserRepository>();
            builder.Services.AddScoped<DAL.Interfaces.IDocumentRepository, DAL.Repositories.DocumentRepository>();
            builder.Services.AddScoped<DAL.Interfaces.IFolderRepository, DAL.Repositories.FolderRepository>();
            builder.Services.AddScoped<BLL.Interfaces.IAuthService, BLL.Services.AuthService>();
            builder.Services.AddScoped<BLL.Interfaces.IChunkingService, BLL.Services.ChunkingService>();
            builder.Services.AddScoped<BLL.Interfaces.IDocumentService, BLL.Services.DocumentService>();
            builder.Services.AddHttpClient<DAL.Interfaces.ISupabaseStorageProvider, DAL.Providers.SupabaseStorageProvider>();
            
            // Register document parsers (FallbackTextParser must be registered last or used carefully; 
            // since we use FirstOrDefault in ChunkingService based on CanParse, order doesn't strictly matter for explicit extensions, 
            // but FallbackTextParser returns true for everything so it should be registered last)
            builder.Services.AddScoped<BLL.Interfaces.IDocumentParser, BLL.Services.Parsers.PdfParser>();
            builder.Services.AddScoped<BLL.Interfaces.IDocumentParser, BLL.Services.Parsers.WordParser>();
            builder.Services.AddScoped<BLL.Interfaces.IDocumentParser, BLL.Services.Parsers.PowerPointParser>();
            builder.Services.AddScoped<BLL.Interfaces.IDocumentParser, BLL.Services.Parsers.FallbackTextParser>();
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
