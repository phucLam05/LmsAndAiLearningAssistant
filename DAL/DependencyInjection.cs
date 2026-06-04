using DAL.Data;
using DAL.Interfaces;
using DAL.Providers;
using DAL.Repositories;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.DependencyInjection;

namespace DAL
{
    public static class DependencyInjection
    {
        public static IServiceCollection AddDataAccessLayer(this IServiceCollection services, IConfiguration configuration)
        {
            // Required so AuditInterceptor can read the current user from HttpContext
            services.AddHttpContextAccessor();

            // Register AuditInterceptor as Scoped so it can use IHttpContextAccessor
            services.AddScoped<AuditInterceptor>();

            // Use the (IServiceProvider, DbContextOptionsBuilder) overload to resolve Scoped interceptor
            services.AddDbContext<ApplicationDbContext>((sp, options) =>
            {
                options.UseNpgsql(
                    configuration.GetConnectionString("DefaultConnection"),
                    o => o.UseVector());
                options.AddInterceptors(sp.GetRequiredService<AuditInterceptor>());
            });

            services.AddScoped<IUserRepository, UserRepository>();
            services.AddScoped<IDocumentRepository, DocumentRepository>();
            services.AddScoped<IFolderRepository, FolderRepository>();
            services.AddScoped<IDocumentChunkRepository, DocumentChunkRepository>();
            services.AddScoped<ISubjectRepository, SubjectRepository>();

            services.AddHttpClient<ISupabaseStorageProvider, SupabaseStorageProvider>();
            services.AddHttpClient<IGeminiEmbeddingProvider, GeminiEmbeddingProvider>();

            return services;
        }
    }
}

