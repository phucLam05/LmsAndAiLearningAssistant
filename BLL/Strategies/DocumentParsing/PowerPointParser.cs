using System;
using System.IO;
using System.Text;
using System.Threading;
using System.Threading.Tasks;
using BLL.Interfaces;
using DocumentFormat.OpenXml.Packaging;
using DocumentFormat.OpenXml.Drawing;

namespace BLL.Strategies.DocumentParsing
{
    /// <summary>
    /// Extracts text from PowerPoint presentations (.pptx) using OpenXml.
    /// </summary>
    public class PowerPointParser : IDocumentParser
    {
        public bool CanParse(string extension)
        {
            return string.Equals(extension, ".pptx", StringComparison.OrdinalIgnoreCase);
        }

        public Task<string> ParseAsync(Stream stream, CancellationToken cancellationToken)
        {
            using var pptDoc = PresentationDocument.Open(stream, false);
            var sb = new StringBuilder();
            var presentationPart = pptDoc.PresentationPart;
            if (presentationPart?.SlideParts != null)
            {
                foreach (var slidePart in presentationPart.SlideParts)
                {
                    cancellationToken.ThrowIfCancellationRequested();
                    if (slidePart.Slide != null)
                    {
                        foreach (var textElement in slidePart.Slide.Descendants<Text>())
                        {
                            sb.AppendLine(textElement.Text);
                        }
                    }
                }
            }
            return Task.FromResult(sb.ToString());
        }
    }
}
