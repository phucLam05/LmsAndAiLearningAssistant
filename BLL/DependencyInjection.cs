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
            services.AddScoped<ISubjectService, SubjectService>();
            services.AddHttpClient<IChatService, ChatService>();
            services.AddScoped<IAdminService, AdminService>();
            services.AddScoped<IEmailService, EmailService>();
            services.AddScoped<IUserService, UserService>();

            // Document parsers
            services.AddScoped<IDocumentParser, PdfParser>();
            services.AddScoped<IDocumentParser, WordParser>();
            services.AddScoped<IDocumentParser, PowerPointParser>();
            services.AddScoped<IDocumentParser, FallbackTextParser>();

            return services;
        }
    }
}
