using System;
using System.Collections.Generic;
using System.Threading;
using System.Threading.Tasks;
using BLL.Interfaces;
using BLL.Strategies.DocumentParsing;
using Core.DTOs.Common;
using Core.Entities;
using DAL.Interfaces;
using Microsoft.Extensions.Logging;
using System.IO;
using System.Linq;

namespace BLL.Services
{
    /// <summary>
    /// Reads document text and chunks it, saving to PostgreSQL.
    /// </summary>
    public class ChunkingService : IChunkingService
    {
        private readonly IDocumentRepository _documentRepository;
        private readonly IDocumentChunkRepository _documentChunkRepository;
        private readonly ILogger<ChunkingService> _logger;
        private readonly ISupabaseStorageProvider _storageProvider;
        private readonly IEnumerable<IDocumentParser> _parsers;

        public ChunkingService(
            IDocumentRepository documentRepository,
            IDocumentChunkRepository documentChunkRepository,
            ILogger<ChunkingService> logger, 
            ISupabaseStorageProvider storageProvider,
            IEnumerable<IDocumentParser> parsers)
        {
            _documentRepository = documentRepository;
            _documentChunkRepository = documentChunkRepository;
            _logger = logger;
            _storageProvider = storageProvider;
            _parsers = parsers;
        }

        public async Task<Result> ProcessFileChunkingAsync(Guid documentId, CancellationToken cancellationToken)
        {
            try
            {
                _logger.LogInformation("Starting chunking process for document {DocumentId}", documentId);

                var document = await _documentRepository.GetByIdAsync(documentId);
                if (document == null)
                {
                    _logger.LogWarning("Document {DocumentId} not found. Skipping chunking.", documentId);
                    return Result.Failure($"Document {documentId} not found.");
                }

                // If already processed, skip to return Success so Hangfire continues to Embedding
                if (document.Status == DocumentStatus.Success)
                {
                    _logger.LogInformation("Document {DocumentId} is already at status Success. Skipping chunking.", documentId);
                    return Result.Success();
                }

                // Update status to Processing
                await _documentRepository.UpdateStatusAsync(documentId, DocumentStatus.Processing);

                // Download and extract text
                string content = await ReadFileContentAsync(document, cancellationToken);
                
                if (string.IsNullOrWhiteSpace(content))
                {
                    _logger.LogWarning("No content extracted from document {DocumentId}. Skipping chunk creation.", documentId);
                    return Result.Success();
                }

                // Chunk the text
                var chunks = ChunkText(content, chunkSize: 500, overlap: 50, document).ToList();

                if (chunks.Any())
                {
                    await _documentChunkRepository.BulkInsertChunksAsync(chunks);
                    _logger.LogInformation("Successfully inserted {ChunkCount} chunks for document {DocumentId}", chunks.Count, documentId);
                }
                
                return Result.Success();
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "An error occurred while chunking document {DocumentId}", documentId);
                _documentRepository.ClearTracker();
                await _documentRepository.UpdateStatusAsync(documentId, DocumentStatus.Failed);
                return Result.Failure($"Chunking error: {ex.Message}");
            }
        }

        private async Task<string> ReadFileContentAsync(Document document, CancellationToken cancellationToken)
        {
            try
            {
                _logger.LogInformation("Downloading file from Supabase Path: {StoragePath}", document.FileUrl);
                
                using var stream = await _storageProvider.DownloadAsync(document.FileUrl, cancellationToken);
                string extension = Path.GetExtension(document.FileName).ToLowerInvariant();

                var parser = _parsers.FirstOrDefault(p => p.CanParse(extension));
                if (parser == null)
                {
                    throw new InvalidOperationException($"No parser found for extension: {extension}");
                }

                _logger.LogInformation("Extracting text using {ParserName}", parser.GetType().Name);
                return await parser.ParseAsync(stream, cancellationToken);
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Failed to read file from Supabase Path: {StoragePath}", document.FileUrl);
                throw new InvalidOperationException($"Could not read file from storage: {document.FileUrl}", ex);
            }
        }

        private IEnumerable<DocumentChunk> ChunkText(string text, int chunkSize, int overlap, Document document)
        {
            if (string.IsNullOrEmpty(text)) yield break;

            int step = chunkSize - overlap;
            int index = 0;

            for (int i = 0; i < text.Length; i += step)
            {
                if (i > 0 && i < text.Length && char.IsLowSurrogate(text[i]))
                {
                    i--;
                }

                int length = Math.Min(chunkSize, text.Length - i);
                
                if (char.IsHighSurrogate(text[i + length - 1]))
                {
                    if (i + length < text.Length)
                        length++;
                    else
                        length--;
                }

                yield return new DocumentChunk
                {
                    Id = Guid.NewGuid(),
                    DocumentId = document.Id,
                    SubjectId = document.SubjectId,
                    ChunkIndex = index++,
                    Content = text.Substring(i, length),
                    CreatedAt = DateTime.UtcNow
                };
                
                if (i + chunkSize >= text.Length) break;
            }
        }
    }
}
