using BLL.Interfaces;
using System;
using System.Collections.Generic;
using System.Text;
using System.Text.Json;
using Microsoft.Extensions.Configuration;

namespace BLL.Services
{
    public class GeminiEmbeddingService : IEmbeddingService
    {
        private readonly HttpClient _httpClient;

        // Tốt nhất bạn nên cấu hình ApiKey này trong appsettings.json thay vì hardcode
        private readonly string _apiKey;

        public GeminiEmbeddingService(HttpClient httpClient,IConfiguration configuration)
        {
            _httpClient = httpClient;
            _apiKey = configuration["GeminiSettings:ApiKey"];
        }

        public async Task<float[]> GetEmbeddingAsync(string text)
        {
            // Model chuẩn text-embedding-004 trả về vector 768 chiều
            var url = $"https://generativelanguage.googleapis.com/v1beta/models/text-embedding-004:embedContent?key={_apiKey}";

            var requestBody = new
            {
                model = "models/text-embedding-004",
                content = new { parts = new[] { new { text = text } } }
            };

            var content = new StringContent(JsonSerializer.Serialize(requestBody), Encoding.UTF8, "application/json");
            var response = await _httpClient.PostAsync(url, content);
            response.EnsureSuccessStatusCode();

            var responseString = await response.Content.ReadAsStringAsync();
            using var doc = JsonDocument.Parse(responseString);

            var values = doc.RootElement.GetProperty("embedding").GetProperty("values");
            return JsonSerializer.Deserialize<float[]>(values.GetRawText());
        }
    }
}
