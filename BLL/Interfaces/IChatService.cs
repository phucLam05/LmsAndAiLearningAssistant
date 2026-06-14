using System;
using System.Collections.Generic;
using System.Threading;
using System.Threading.Tasks;
using Core.DTOs.Subject;

namespace BLL.Interfaces
{
    /// <summary>
    /// Service interface for RAG Chatbot operations.
    /// </summary>
    public interface IChatService
    {
        Task<ChatResponseDto> ChatWithSubjectAsync(Guid subjectId, string query, string? model = null, List<Guid>? documentIds = null, CancellationToken cancellationToken = default);
    }
}
