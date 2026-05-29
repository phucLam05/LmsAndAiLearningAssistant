using System.Net.Http.Headers;
using System.Text;
using System.Text.Json;
using BLL.Interfaces;
using Microsoft.Extensions.Configuration;

namespace BLL.Services
{
    /// <summary>
    /// Uploads and deletes private files in Supabase Storage by using the backend-only service role key.
    /// </summary>
    public class SupabaseStorageService : ISupabaseStorageService
    {
        private readonly HttpClient _httpClient;
        private readonly SupabaseOptions _options;

        /// <summary>
        /// Creates a storage service and loads Supabase URL, service role key, and bucket name from configuration.
        /// </summary>
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

        /// <summary>
        /// Uploads a stream to a private Supabase Storage object path without overwriting existing files.
        /// </summary>
        public async Task UploadAsync(string storagePath, Stream content, string contentType, CancellationToken cancellationToken = default)
        {
            EnsureConfigured();

            using var request = new HttpRequestMessage(HttpMethod.Post, BuildObjectUri(storagePath));
            AddAuthHeaders(request);
            request.Headers.TryAddWithoutValidation("x-upsert", "false");
            request.Content = new StreamContent(content);
            request.Content.Headers.ContentType = new MediaTypeHeaderValue(NormalizeContentType(contentType));

            using var response = await _httpClient.SendAsync(request, cancellationToken);
            if (!response.IsSuccessStatusCode)
            {
                var body = await response.Content.ReadAsStringAsync(cancellationToken);
                throw new InvalidOperationException($"Supabase upload failed: {(int)response.StatusCode} {body}");
            }
        }

        /// <summary>
        /// Deletes a private Supabase Storage object by path. Missing objects are treated as storage API errors.
        /// </summary>
        public async Task DeleteAsync(string storagePath, CancellationToken cancellationToken = default)
        {
            EnsureConfigured();

            using var request = new HttpRequestMessage(HttpMethod.Delete, BuildBucketUri());
            AddAuthHeaders(request);
            request.Content = new StringContent(
                JsonSerializer.Serialize(new { prefixes = new[] { NormalizeStoragePath(storagePath) } }),
                Encoding.UTF8,
                "application/json");

            using var response = await _httpClient.SendAsync(request, cancellationToken);
            if (!response.IsSuccessStatusCode)
            {
                var body = await response.Content.ReadAsStringAsync(cancellationToken);
                throw new InvalidOperationException($"Supabase delete failed: {(int)response.StatusCode} {body}");
            }
        }

        /// <summary>
        /// Builds the Supabase upload URL and tolerates config values with or without /rest/v1 or /storage/v1 suffixes.
        /// </summary>
        private Uri BuildObjectUri(string storagePath)
        {
            var encodedPath = string.Join("/", NormalizeStoragePath(storagePath).Split('/').Select(Uri.EscapeDataString));
            return new Uri($"{GetSupabaseOrigin()}/storage/v1/object/{Uri.EscapeDataString(NormalizeBucket(_options.Bucket))}/{encodedPath}");
        }

        /// <summary>
        /// Builds the Supabase delete URL for the configured bucket.
        /// </summary>
        private Uri BuildBucketUri()
        {
            return new Uri($"{GetSupabaseOrigin()}/storage/v1/object/{Uri.EscapeDataString(NormalizeBucket(_options.Bucket))}");
        }

        /// <summary>
        /// Adds service-role authorization headers required by Supabase Storage private buckets.
        /// </summary>
        private void AddAuthHeaders(HttpRequestMessage request)
        {
            request.Headers.Authorization = new AuthenticationHeaderValue("Bearer", _options.ServiceRoleKey);
            request.Headers.TryAddWithoutValidation("apikey", _options.ServiceRoleKey);
        }

        /// <summary>
        /// Validates required Supabase settings before making HTTP calls.
        /// </summary>
        private void EnsureConfigured()
        {
            if (string.IsNullOrWhiteSpace(_options.Url) ||
                string.IsNullOrWhiteSpace(_options.ServiceRoleKey) ||
                string.IsNullOrWhiteSpace(_options.Bucket))
            {
                throw new InvalidOperationException("Supabase storage is not configured.");
            }
        }

        /// <summary>
        /// Converts any Supabase API URL to only scheme and host, preventing duplicated path segments.
        /// </summary>
        private string GetSupabaseOrigin()
        {
            var rawUrl = _options.Url.Trim().TrimEnd('/');
            if (!Uri.TryCreate(rawUrl, UriKind.Absolute, out var uri))
            {
                throw new InvalidOperationException("Supabase URL is invalid.");
            }

            return uri.GetLeftPart(UriPartial.Authority);
        }

        /// <summary>
        /// Ensures object paths never start with a slash, which Supabase rejects as an invalid storage path.
        /// </summary>
        private static string NormalizeStoragePath(string storagePath)
        {
            return storagePath.Replace('\\', '/').Trim('/');
        }

        /// <summary>
        /// Ensures the bucket name has no leading or trailing slashes.
        /// </summary>
        private static string NormalizeBucket(string bucket)
        {
            return bucket.Trim().Trim('/');
        }

        /// <summary>
        /// Falls back to application/octet-stream for archive and unknown browser MIME values.
        /// </summary>
        private static string NormalizeContentType(string contentType)
        {
            return string.IsNullOrWhiteSpace(contentType) ? "application/octet-stream" : contentType.Trim();
        }
    }
}
