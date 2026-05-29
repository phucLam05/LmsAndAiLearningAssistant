using System.IO;
using System.Threading;
using System.Threading.Tasks;

namespace BLL.Strategies.DocumentParsing
{
    /// <summary>
    /// Defines a strategy for parsing text from a specific document format.
    /// </summary>
    public interface IDocumentParser
    {
        /// <summary>
        /// Determines if this parser can handle the given file extension.
        /// </summary>
        /// <param name="extension">The file extension (e.g., ".pdf").</param>
        bool CanParse(string extension);

        /// <summary>
        /// Parses the stream and extracts its textual content.
        /// </summary>
        /// <param name="stream">The document stream.</param>
        /// <param name="cancellationToken">Cancellation token.</param>
        /// <returns>Extracted text.</returns>
        Task<string> ParseAsync(Stream stream, CancellationToken cancellationToken);
    }
}
