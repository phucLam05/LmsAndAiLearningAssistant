using System;
using System.Collections.Generic;
using System.Threading;
using System.Threading.Tasks;
using BLL.Interfaces;
using Core.Entities;
using DAL.Interfaces;
using Microsoft.Extensions.Logging;
using System.IO;
using System.Linq;

namespace BLL.Services
{
    /// <summary>
    /// Implementation of IChunkingService that reads document text and chunks it for further processing.
    /// </summary>
    public class ChunkingService : IChunkingService
    {
        private readonly IDocumentRepository _documentRepository;
        private readonly ILogger<ChunkingService> _logger;
        private readonly ISupabaseStorageProvider _storageProvider;
        private readonly IEnumerable<IDocumentParser> _parsers;

        /// <summary>
        /// Initializes a new instance of the ChunkingService.
        /// </summary>
        /// <param name="documentRepository">The data access repository for documents.</param>
        /// <param name="logger">The logger instance.</param>
        /// <param name="storageProvider">Provider for downloading files from Supabase.</param>
        /// <param name="parsers">Collection of document parsers injected via DI.</param>
        public ChunkingService(
            IDocumentRepository documentRepository, 
            ILogger<ChunkingService> logger, 
            ISupabaseStorageProvider storageProvider,
            IEnumerable<IDocumentParser> parsers)
        {
            _documentRepository = documentRepository;
            _logger = logger;
            _storageProvider = storageProvider;
            _parsers = parsers;
        }

        /// <summary>
        /// Processes a file associated with the given document ID, chunking it into smaller text pieces.
        /// </summary>
        /// <param name="documentId">The unique identifier of the document.</param>
        /// <param name="cancellationToken">Token to monitor for cancellation requests.</param>
        /// <returns>A task that represents the asynchronous operation.</returns>
        public async Task ProcessFileChunkingAsync(Guid documentId, CancellationToken cancellationToken)
        {
            try
            {
                _logger.LogInformation("Starting chunking process for Document ID: {DocumentId}", documentId);
                await _documentRepository.UpdateStatusAsync(documentId, DocumentProcessingStatus.Chunking);

                var document = await _documentRepository.GetByIdAsync(documentId);
                if (document == null)
                {
                    _logger.LogWarning("Document ID: {DocumentId} not found.", documentId);
                    return;
                }

                string fileContent = await ReadFileContentAsync(document, cancellationToken);
                
                var chunks = ChunkText(fileContent, chunkSize: 500, overlap: 50, documentId);

                await _documentRepository.BulkInsertChunksAsync(chunks);
                
                await _documentRepository.UpdateStatusAsync(documentId, DocumentProcessingStatus.Chunked);

                _logger.LogInformation("Successfully completed chunking process for Document ID: {DocumentId}", documentId);
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error occurred while chunking Document ID: {DocumentId}", documentId);
                await _documentRepository.UpdateStatusAsync(documentId, DocumentProcessingStatus.Failed);
                throw;
            }
        }

        /// <summary>
        /// Reads the file content from Supabase storage and extracts text based on file format.
        /// </summary>
        /// <param name="document">The document entity containing URL and original file name.</param>
        /// <param name="cancellationToken">Token to monitor for cancellation requests.</param>
        /// <returns>The extracted text content of the file.</returns>
        private async Task<string> ReadFileContentAsync(Document document, CancellationToken cancellationToken)
        {
            try
            {
                _logger.LogInformation("Downloading file from Supabase Path: {StoragePath}", document.StoragePath);
                
                using var stream = await _storageProvider.DownloadAsync(document.StoragePath, cancellationToken);
                string extension = Path.GetExtension(document.OriginalFileName).ToLowerInvariant();

                // Select the first parser that can handle this extension (FallbackTextParser should be registered last)
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
                _logger.LogError(ex, "Failed to read file from Supabase Path: {StoragePath}", document.StoragePath);
                throw new InvalidOperationException($"Could not read file from storage: {document.StoragePath}", ex);
            }
        }

        /// <summary>
        /// Chunks the provided text based on chunk size and overlap size.
        /// </summary>
        /// <param name="text">The text to chunk.</param>
        /// <param name="chunkSize">The maximum size of each chunk.</param>
        /// <param name="overlap">The size of the overlap between consecutive chunks.</param>
        /// <param name="documentId">The ID of the document being chunked.</param>
        /// <returns>An enumerable of DocumentChunk entities.</returns>
        private IEnumerable<DocumentChunk> ChunkText(string text, int chunkSize, int overlap, Guid documentId)
        {
            if (string.IsNullOrEmpty(text)) yield break;

            int step = chunkSize - overlap;
            int index = 0;

            for (int i = 0; i < text.Length; i += step)
            {
                int length = Math.Min(chunkSize, text.Length - i);
                yield return new DocumentChunk
                {
                    Id = Guid.NewGuid(),
                    DocumentId = documentId,
                    ChunkIndex = index++,
                    Content = text.Substring(i, length),
                    TokenCount = length, // Simplistic token count
                    CreatedAt = DateTime.UtcNow
                };
                
                if (i + chunkSize >= text.Length) break;
            }
        }
    }
}
