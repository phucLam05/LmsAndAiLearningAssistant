using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading;
using System.Threading.Tasks;
using BLL.Interfaces;
using Core.DTOs.Common;
using Core.Entities;
using DAL.Interfaces;
using Microsoft.Extensions.Logging;
using Pgvector;

namespace BLL.Services
{
    /// <summary>
    /// Service that generates vector embeddings for chunks of text and saves them to the DB.
    /// </summary>
    public class DocumentEmbeddingService : IEmbeddingService
    {
        private readonly IDocumentRepository _documentRepository;
        private readonly IDocumentChunkRepository _documentChunkRepository;
        private readonly IGeminiEmbeddingProvider _geminiProvider;
        private readonly ILogger<DocumentEmbeddingService> _logger;

        public DocumentEmbeddingService(
            IDocumentRepository documentRepository,
            IDocumentChunkRepository documentChunkRepository,
            IGeminiEmbeddingProvider geminiProvider,
            ILogger<DocumentEmbeddingService> logger)
        {
            _documentRepository = documentRepository;
            _documentChunkRepository = documentChunkRepository;
            _geminiProvider = geminiProvider;
            _logger = logger;
        }

        public async Task<Result> ProcessEmbeddingsAsync(Guid documentId, CancellationToken cancellationToken = default)
        {
            try
            {
                _logger.LogInformation("Starting embedding process for DocumentId: {DocumentId}", documentId);

                var document = await _documentRepository.GetByIdAsync(documentId);
                if (document == null)
                {
                    return Result.Failure("Document not found.");
                }

                // If already success, skip
                if (document.Status == DocumentStatus.Success)
                {
                    _logger.LogInformation("Document {DocumentId} is already at status Success. Skipping embedding.", documentId);
                    return Result.Success();
                }

                // Update status to Processing
                await _documentRepository.UpdateStatusAsync(documentId, DocumentStatus.Processing);

                // Fetch chunks
                var chunks = await _documentChunkRepository.GetChunksByDocumentIdAsync(documentId);
                if (chunks == null || !chunks.Any())
                {
                    _logger.LogWarning("No chunks found for DocumentId: {DocumentId}. Marking as Success anyway.", documentId);
                    await _documentRepository.UpdateStatusAsync(documentId, DocumentStatus.Success);
                    return Result.Success();
                }

                var chunksToProcess = chunks.Where(c => c.Embedding == null).ToList();
                _logger.LogInformation("Found {TotalChunks} chunks, {PendingChunks} need embeddings.", chunks.Count, chunksToProcess.Count);

                const int batchSize = 10;
                var batch = new List<DocumentChunk>();

                foreach (var chunk in chunksToProcess)
                {
                    cancellationToken.ThrowIfCancellationRequested();

                    var vectorArray = await _geminiProvider.GetEmbeddingAsync(chunk.Content, cancellationToken);
                    chunk.Embedding = new Vector(vectorArray);
                    
                    batch.Add(chunk);

                    if (batch.Count >= batchSize)
                    {
                        await _documentChunkRepository.UpdateChunksAsync(batch);
                        batch.Clear();
                    }
                }

                if (batch.Any())
                {
                    await _documentChunkRepository.UpdateChunksAsync(batch);
                }

                // Update status to Success
                await _documentRepository.UpdateStatusAsync(documentId, DocumentStatus.Success);
                _logger.LogInformation("Successfully completed embedding process for DocumentId: {DocumentId}", documentId);
                
                return Result.Success();
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Failed to process embeddings for DocumentId: {DocumentId}", documentId);
                
                _documentRepository.ClearTracker();
                await _documentRepository.UpdateStatusAsync(documentId, DocumentStatus.Failed);
                return Result.Failure($"Embedding error: {ex.Message}");
            }
        }
    }
}
