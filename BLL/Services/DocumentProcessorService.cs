using BLL.Interfaces;
using Core.Entities;
using DAL.Data;
using Pgvector;
using System;
using System.Collections.Generic;
using System.Text;

namespace BLL.Services
{
    public  class DocumentProcessorService
    {
        private readonly IEmbeddingService _embeddingService;
        private readonly ApplicationDbContext _dbContext;

        // Inject IEmbeddingService và ApplicationDbContext
        public DocumentProcessorService(IEmbeddingService embeddingService, ApplicationDbContext dbContext)
        {
            _embeddingService = embeddingService;
            _dbContext = dbContext;
        }

        public async Task ProcessAndEmbedDocumentAsync(Guid documentId, string fullText)
        {
            // 1. Chia nhỏ văn bản (mỗi chunk khoảng 300 từ để đảm bảo ngữ cảnh tốt)
            List<string> chunks = SplitTextIntoChunks(fullText, maxWords: 300);

            var documentChunksToSave = new List<DocumentChunk>();

            for (int i = 0; i < chunks.Count; i++)
            {
                string chunkText = chunks[i];

                // 2. Gọi API Gemini để lấy Vector 768 chiều
                float[] vectorArray = await _embeddingService.GetEmbeddingAsync(chunkText);

                // 3. Khởi tạo Entity DocumentChunk bám sát cấu trúc của nhóm
                var documentChunk = new DocumentChunk
                {
                    Id = Guid.NewGuid(),
                    DocumentId = documentId,
                    ChunkIndex = i,
                    Content = chunkText,
                    TokenCount = chunkText.Split(new[] { ' ', '\n', '\r' }, StringSplitOptions.RemoveEmptyEntries).Length,
                    Embedding = new Vector(vectorArray),
                    CreatedAt = DateTime.UtcNow // Có thể bỏ qua nếu DbContext đã cấu hình DefaultValueSql("NOW()")
                };

                documentChunksToSave.Add(documentChunk);
            }

            // 4. Lưu toàn bộ các chunk vào Database cùng một lúc (Tối ưu hiệu suất)
            if (documentChunksToSave.Count > 0)
            {
                await _dbContext.DocumentChunks.AddRangeAsync(documentChunksToSave);
                await _dbContext.SaveChangesAsync();
            }
        }

        // Hàm hỗ trợ chia nhỏ văn bản
        private List<string> SplitTextIntoChunks(string text, int maxWords)
        {
            var words = text.Split(new[] { ' ', '\n', '\r', '\t' }, StringSplitOptions.RemoveEmptyEntries);
            var chunks = new List<string>();
            var currentChunk = new List<string>();

            foreach (var word in words)
            {
                currentChunk.Add(word);
                if (currentChunk.Count >= maxWords)
                {
                    chunks.Add(string.Join(" ", currentChunk));
                    currentChunk.Clear();
                }
            }

            if (currentChunk.Count > 0)
            {
                chunks.Add(string.Join(" ", currentChunk));
            }

            return chunks;
        }
    }
}
