using System;
using System.IO;
using System.Threading;
using System.Threading.Tasks;
using BLL.Interfaces;
using DocumentFormat.OpenXml.Packaging;

namespace BLL.Strategies.DocumentParsing
{
    /// <summary>
    /// Extracts text from Word documents (.docx) using OpenXml.
    /// </summary>
    public class WordParser : IDocumentParser
    {
        public bool CanParse(string extension)
        {
            return string.Equals(extension, ".docx", StringComparison.OrdinalIgnoreCase);
        }

        public Task<string> ParseAsync(Stream stream, CancellationToken cancellationToken)
        {
            cancellationToken.ThrowIfCancellationRequested();
            using var wordDoc = WordprocessingDocument.Open(stream, false);
            return Task.FromResult(wordDoc.MainDocumentPart?.Document?.Body?.InnerText ?? string.Empty);
        }
    }
}
