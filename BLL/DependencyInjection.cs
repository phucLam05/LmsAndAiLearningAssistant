using BLL.Interfaces;
using BLL.Services;
using BLL.Strategies.DocumentParsing;
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
            services.AddScoped<IDriveService, DriveService>();
            services.AddScoped<IAdminService, AdminService>();
            services.AddScoped<ISubjectService, SubjectService>();

            // Document parsers
            services.AddScoped<IDocumentParser, PdfParser>();
            services.AddScoped<IDocumentParser, WordParser>();
            services.AddScoped<IDocumentParser, PowerPointParser>();
            services.AddScoped<IDocumentParser, FallbackTextParser>();

            return services;
        }
    }
}
