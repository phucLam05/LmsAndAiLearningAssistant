using BLL.Interfaces;
using BLL.Services;

using Microsoft.Extensions.DependencyInjection;

namespace BLL
{
    public static class DependencyInjection
    {
        public static IServiceCollection AddBusinessLogicLayer(this IServiceCollection services)
        {
            services.AddScoped<IAuthService, AuthService>();
            services.AddScoped<IChunkingService, ChunkingService>();
            services.AddScoped<IDocumentService, DocumentService>();
            services.AddScoped<IEmbeddingService, DocumentEmbeddingService>();
            // services.AddScoped<IDriveService, DriveService>();
            services.AddScoped<IAdminService, AdminService>();

            return services;
        }
    }
}
