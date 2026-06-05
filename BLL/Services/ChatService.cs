using BLL.Interfaces;
using Core.Entities;
using DAL.Data;
using DAL.Interfaces;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.Logging;
using Pgvector.EntityFrameworkCore;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Net.Http;
using System.Text;
using System.Text.Json;
using System.Threading;
using System.Threading.Tasks;

namespace BLL.Services
{
    /// <summary>
    /// Service that coordinates RAG Chatbot operations using vector similarity search in pgvector and Gemini API for generation.
    /// </summary>
    public class ChatService : IChatService
    {
        private readonly ApplicationDbContext _dbContext;
        private readonly IGeminiEmbeddingProvider _embeddingProvider;
        private readonly HttpClient _httpClient;
        private readonly string _apiKey;
        private readonly string _baseUrl;
        private readonly ILogger<ChatService> _logger;

        public ChatService(
            ApplicationDbContext dbContext,
            IGeminiEmbeddingProvider embeddingProvider,
            HttpClient httpClient,
            IConfiguration configuration,
            ILogger<ChatService> logger)
        {
            _dbContext = dbContext;
            _embeddingProvider = embeddingProvider;
            _httpClient = httpClient;
            _apiKey = configuration["GeminiSettings:ApiKey"] ?? string.Empty;
            _baseUrl = configuration["GeminiSettings:BaseUrl"] ?? "https://generativelanguage.googleapis.com/v1beta/";
            _logger = logger;
        }

        public async Task<string> ChatWithSubjectAsync(Guid subjectId, string query, CancellationToken cancellationToken = default)
        {
            if (string.IsNullOrWhiteSpace(query))
            {
                return "Please enter a question.";
            }

            try
            {
                _logger.LogInformation("Generating embedding for student query: {Query}", query);
                var queryEmbedding = await _embeddingProvider.GetEmbeddingAsync(query, cancellationToken);
                var pgVector = new Pgvector.Vector(queryEmbedding);

                _logger.LogInformation("Performing similarity search for SubjectId: {SubjectId}", subjectId);

                // Perform vector search using cosine distance
                var matchedChunks = await _dbContext.DocumentChunks
                    .Where(c => c.SubjectId == subjectId && c.Embedding != null)
                    .OrderBy(c => c.Embedding!.CosineDistance(pgVector))
                    .Take(5)
                    .Include(c => c.Document)
                    .Select(c => new { c.Content, FileName = c.Document != null ? c.Document.FileName : "Unknown Document" })
                    .ToListAsync(cancellationToken);

                if (!matchedChunks.Any())
                {
                    return "Sorry, I could not find any documents or materials uploaded for this subject to answer your question. Please ask your lecturer to upload course materials.";
                }

                // Construct prompt
                var contextBuilder = new StringBuilder();
                foreach (var chunk in matchedChunks)
                {
                    contextBuilder.AppendLine($"[Source Document: {chunk.FileName}]");
                    contextBuilder.AppendLine(chunk.Content);
                    contextBuilder.AppendLine("---");
                }

                var systemPrompt = "You are a helpful University AI Learning Assistant. Answer the student's question based strictly on the provided course documents. If the documents do not contain enough information to answer, politely state that the answer is not in the course materials and ask the student to contact their lecturer for more details. Keep your response format clear, concise, and using markdown for formatting (such as bullet points or bold text) when helpful.";
                
                var prompt = $"System context:\n{systemPrompt}\n\nCourse materials context:\n{contextBuilder}\n\nStudent's Question: {query}\n\nAnswer:";

                _logger.LogInformation("Sending request to Gemini API for text generation.");
                var answer = await GenerateTextAsync(prompt, cancellationToken);
                return answer;
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error occurred during RAG chat with SubjectId: {SubjectId}", subjectId);
                return $"An error occurred while communicating with the AI Assistant: {ex.Message}";
            }
        }

        private async Task<string> GenerateTextAsync(string prompt, CancellationToken cancellationToken)
        {
            if (string.IsNullOrWhiteSpace(_apiKey))
            {
                throw new InvalidOperationException("Gemini API key is not configured.");
            }

            var model = "gemini-1.5-flash";
            var url = $"{_baseUrl.TrimEnd('/')}/models/{model}:generateContent?key={_apiKey}";

            var requestBody = new
            {
                contents = new[]
                {
                    new
                    {
                        parts = new[]
                        {
                            new { text = prompt }
                        }
                    }
                }
            };

            var json = JsonSerializer.Serialize(requestBody);
            var content = new StringContent(json, Encoding.UTF8, "application/json");

            var response = await _httpClient.PostAsync(url, content, cancellationToken);
            if (!response.IsSuccessStatusCode)
            {
                var error = await response.Content.ReadAsStringAsync(cancellationToken);
                throw new InvalidOperationException($"Gemini generation API error: {(int)response.StatusCode} {error}");
            }

            var responseString = await response.Content.ReadAsStringAsync(cancellationToken);
            using var doc = JsonDocument.Parse(responseString);
            
            try
            {
                var text = doc.RootElement
                    .GetProperty("candidates")[0]
                    .GetProperty("content")
                    .GetProperty("parts")[0]
                    .GetProperty("text")
                    .GetString();

                return text ?? "No response generated by the AI.";
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Failed to parse Gemini text generation response. Raw response: {Raw}", responseString);
                throw new InvalidOperationException("Failed to parse AI response.");
            }
        }
    }
}
