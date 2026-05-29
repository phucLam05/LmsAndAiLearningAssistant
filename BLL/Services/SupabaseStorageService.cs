using System.Net.Http.Headers;
using System.Text;
using System.Text.Json;
using BLL.Interfaces;
using Microsoft.Extensions.Configuration;

namespace BLL.Services
{
    /// <summary>
    /// Uploads and deletes private document files in Supabase Storage by using the backend-only service role key.
    /// </summary>
    public class SupabaseStorageService : ISupabaseStorageService
    {
        private readonly HttpClient _httpClient;
        private readonly SupabaseOptions _options;

        public SupabaseStorageService(HttpClient httpClient, IConfiguration configuration)
        {
            _httpClient = httpClient;

            var section = configuration.GetSection("Supabase");
            _options = new SupabaseOptions
            {
                Url = section["Url"] ?? string.Empty,
                ServiceRoleKey = section["ServiceRoleKey"] ?? string.Empty,
                Bucket = string.IsNullOrWhiteSpace(section["Bucket"]) ? "documents" : section["Bucket"]!
            };
        }

        public async Task UploadAsync(string storagePath, Stream content, string contentType, CancellationToken cancellationToken = default)
        {
            EnsureConfigured();

            using var request = new HttpRequestMessage(HttpMethod.Put, BuildObjectUri(storagePath));
            AddAuthHeaders(request);
            request.Headers.TryAddWithoutValidation("x-upsert", "false");
            request.Content = new StreamContent(content);
            request.Content.Headers.ContentType = new MediaTypeHeaderValue(contentType);

            using var response = await _httpClient.SendAsync(request, cancellationToken);
            if (!response.IsSuccessStatusCode)
            {
                var body = await response.Content.ReadAsStringAsync(cancellationToken);
                throw new InvalidOperationException($"Supabase upload failed: {(int)response.StatusCode} {body}");
            }
        }

        public async Task DeleteAsync(string storagePath, CancellationToken cancellationToken = default)
        {
            EnsureConfigured();

            using var request = new HttpRequestMessage(HttpMethod.Delete, BuildBucketUri());
            AddAuthHeaders(request);
            request.Content = new StringContent(
                JsonSerializer.Serialize(new { prefixes = new[] { storagePath } }),
                Encoding.UTF8,
                "application/json");

            using var response = await _httpClient.SendAsync(request, cancellationToken);
            if (!response.IsSuccessStatusCode)
            {
                var body = await response.Content.ReadAsStringAsync(cancellationToken);
                throw new InvalidOperationException($"Supabase delete failed: {(int)response.StatusCode} {body}");
            }
        }

        private Uri BuildObjectUri(string storagePath)
        {
            var encodedPath = string.Join("/", storagePath.Split('/').Select(Uri.EscapeDataString));
            return new Uri($"{_options.Url.TrimEnd('/')}/storage/v1/object/{Uri.EscapeDataString(_options.Bucket)}/{encodedPath}");
        }

        private Uri BuildBucketUri()
        {
            return new Uri($"{_options.Url.TrimEnd('/')}/storage/v1/object/{Uri.EscapeDataString(_options.Bucket)}");
        }

        private void AddAuthHeaders(HttpRequestMessage request)
        {
            request.Headers.Authorization = new AuthenticationHeaderValue("Bearer", _options.ServiceRoleKey);
            request.Headers.TryAddWithoutValidation("apikey", _options.ServiceRoleKey);
        }

        private void EnsureConfigured()
        {
            if (string.IsNullOrWhiteSpace(_options.Url) ||
                string.IsNullOrWhiteSpace(_options.ServiceRoleKey) ||
                string.IsNullOrWhiteSpace(_options.Bucket))
            {
                throw new InvalidOperationException("Supabase storage is not configured.");
            }
        }
    }
}
