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
            services.AddDbContext<ApplicationDbContext>(options =>
                options.UseNpgsql(configuration.GetConnectionString("DefaultConnection"), o => o.UseVector()));

            services.AddScoped<IUserRepository, UserRepository>();
            services.AddScoped<IDocumentRepository, DocumentRepository>();
            services.AddScoped<IDocumentChunkRepository, DocumentChunkRepository>();

            services.AddHttpClient<ISupabaseStorageProvider, SupabaseStorageProvider>();
            services.AddHttpClient<IGeminiEmbeddingProvider, GeminiEmbeddingProvider>();

            return services;
        }
    }
}
