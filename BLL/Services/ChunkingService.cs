using System;
using System.Collections.Generic;
using System.Threading;
using System.Threading.Tasks;
using BLL.Interfaces;
using Core.Entities;
using DAL.Interfaces;
using Microsoft.Extensions.Logging;
using System.IO;
using System.Net.Http;
using System.Text;
using UglyToad.PdfPig;
using DocumentFormat.OpenXml.Packaging;
using DocumentFormat.OpenXml.Drawing;

namespace BLL.Services
{
    /// <summary>
    /// Implementation of IChunkingService that reads document text and chunks it for further processing.
    /// </summary>
    public class ChunkingService : IChunkingService
    {
        private readonly IDocumentRepository _documentRepository;
        private readonly ILogger<ChunkingService> _logger;
        private readonly IHttpClientFactory _httpClientFactory;

        /// <summary>
        /// Initializes a new instance of the ChunkingService.
        /// </summary>
        /// <param name="documentRepository">The data access repository for documents.</param>
        /// <param name="logger">The logger instance.</param>
        /// <param name="httpClientFactory">The HTTP client factory to create clients for downloading files.</param>
        public ChunkingService(IDocumentRepository documentRepository, ILogger<ChunkingService> logger, IHttpClientFactory httpClientFactory)
        {
            _documentRepository = documentRepository;
            _logger = logger;
            _httpClientFactory = httpClientFactory;
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
                await _documentRepository.UpdateStatusAsync(documentId, DocumentProcessingStatus.Processing);

                var document = await _documentRepository.GetByIdAsync(documentId);
                if (document == null)
                {
                    _logger.LogWarning("Document ID: {DocumentId} not found.", documentId);
                    return;
                }

                string fileContent = await ReadFileContentAsync(document, cancellationToken);
                
                var chunks = ChunkText(fileContent, chunkSize: 500, overlap: 50, documentId);

                await _documentRepository.BulkInsertChunksAsync(chunks);
                
                // Assuming "Indexed" or a new intermediate status prior to the vector embedding phase
                await _documentRepository.UpdateStatusAsync(documentId, DocumentProcessingStatus.Indexed);

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
                var client = _httpClientFactory.CreateClient("Supabase");
                _logger.LogInformation("Downloading file from Supabase URL: {StorageUrl}", document.StorageUrl);
                
                var response = await client.GetAsync(document.StorageUrl, cancellationToken);
                response.EnsureSuccessStatusCode();

                using var stream = await response.Content.ReadAsStreamAsync(cancellationToken);
                string extension = System.IO.Path.GetExtension(document.OriginalFileName).ToLowerInvariant();

                return extension switch
                {
                    ".pdf" => ExtractTextFromPdf(stream),
                    ".docx" => ExtractTextFromWord(stream),
                    ".pptx" => ExtractTextFromPowerPoint(stream),
                    _ => await ExtractTextFromPlainText(stream, cancellationToken)
                };
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Failed to read file from Supabase URL: {StorageUrl}", document.StorageUrl);
                throw new InvalidOperationException($"Could not read file from storage: {document.StorageUrl}", ex);
            }
        }

        private string ExtractTextFromPdf(Stream stream)
        {
            using var pdf = PdfDocument.Open(stream);
            var sb = new StringBuilder();
            foreach (var page in pdf.GetPages())
            {
                sb.AppendLine(page.Text);
            }
            return sb.ToString();
        }

        private string ExtractTextFromWord(Stream stream)
        {
            using var wordDoc = WordprocessingDocument.Open(stream, false);
            return wordDoc.MainDocumentPart?.Document?.Body?.InnerText ?? string.Empty;
        }

        private string ExtractTextFromPowerPoint(Stream stream)
        {
            using var pptDoc = PresentationDocument.Open(stream, false);
            var sb = new StringBuilder();
            var presentationPart = pptDoc.PresentationPart;
            if (presentationPart?.SlideParts != null)
            {
                foreach (var slidePart in presentationPart.SlideParts)
                {
                    if (slidePart.Slide != null)
                    {
                        foreach (var textElement in slidePart.Slide.Descendants<DocumentFormat.OpenXml.Drawing.Text>())
                        {
                            sb.AppendLine(textElement.Text);
                        }
                    }
                }
            }
            return sb.ToString();
        }

        private async Task<string> ExtractTextFromPlainText(Stream stream, CancellationToken cancellationToken)
        {
            using var reader = new StreamReader(stream);
            return await reader.ReadToEndAsync(cancellationToken);
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
