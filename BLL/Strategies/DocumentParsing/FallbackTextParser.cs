using System.IO;
using System.Threading;
using System.Threading.Tasks;
using BLL.Interfaces;
using Microsoft.Extensions.Logging;

namespace BLL.Strategies.DocumentParsing
{
    /// <summary>
    /// Fallback parser that reads any document as plain text. 
    /// This is used when no specialized parser is available for an extension.
    /// </summary>
    public class FallbackTextParser : IDocumentParser
    {
        private readonly ILogger<FallbackTextParser> _logger;

        public FallbackTextParser(ILogger<FallbackTextParser> logger)
        {
            _logger = logger;
        }

        /// <summary>
        /// Always returns true because this is the fallback parser.
        /// </summary>
        public bool CanParse(string extension)
        {
            return true;
        }

        public async Task<string> ParseAsync(Stream stream, CancellationToken cancellationToken)
        {
            _logger.LogWarning("Using fallback text parser. Complex formats like .doc or .ppt may result in binary characters being extracted.");
            using var reader = new StreamReader(stream);
            return await reader.ReadToEndAsync(cancellationToken);
        }
    }
}
