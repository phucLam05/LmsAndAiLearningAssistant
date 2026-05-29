using System;
using System.IO;
using System.Text;
using System.Threading;
using System.Threading.Tasks;
using BLL.Interfaces;
using UglyToad.PdfPig;

namespace BLL.Services.Parsers
{
    /// <summary>
    /// Extracts text from PDF documents using PdfPig.
    /// </summary>
    public class PdfParser : IDocumentParser
    {
        public bool CanParse(string extension)
        {
            return string.Equals(extension, ".pdf", StringComparison.OrdinalIgnoreCase);
        }

        public Task<string> ParseAsync(Stream stream, CancellationToken cancellationToken)
        {
            using var pdf = PdfDocument.Open(stream);
            var sb = new StringBuilder();
            foreach (var page in pdf.GetPages())
            {
                cancellationToken.ThrowIfCancellationRequested();
                sb.AppendLine(page.Text);
            }
            return Task.FromResult(sb.ToString());
        }
    }
}
