using System;
using System.Threading;
using System.Threading.Tasks;
using BLL.Interfaces;
using Hangfire;
using Microsoft.AspNetCore.Mvc;

namespace PL.Controllers
{
    /// <summary>
    /// API Controller for managing document uploads and processes.
    /// </summary>
    [ApiController]
    [Route("api/[controller]")]
    public class DocumentController : ControllerBase
    {
        private readonly IBackgroundJobClient _backgroundJobs;

        /// <summary>
        /// Initializes a new instance of the DocumentController.
        /// </summary>
        /// <param name="backgroundJobs">The Hangfire background job client.</param>
        public DocumentController(IBackgroundJobClient backgroundJobs)
        {
            _backgroundJobs = backgroundJobs;
        }

        /// <summary>
        /// Uploads a document and queues it for background chunking.
        /// </summary>
        /// <returns>An IActionResult indicating the status of the upload and processing trigger.</returns>
        [HttpPost("upload")]
        public async Task<IActionResult> UploadDocument()
        {
            // ... [Existing Logic] File upload and metadata save to DB ...
            // In a real scenario, the file would be saved and a new Document entity created.
            Guid documentId = Guid.NewGuid(); 

            // Enqueue the chunking job as a background Hangfire task
            _backgroundJobs.Enqueue<IChunkingService>(x => x.ProcessFileChunkingAsync(documentId, CancellationToken.None));

            return Ok(new { DocumentId = documentId, Message = "File uploaded successfully and chunking started." });
        }
    }
}
