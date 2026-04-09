using System.Text;
using Alextor.RAG.Extractor.Interface;
using DocumentFormat.OpenXml.Drawing;
using DocumentFormat.OpenXml.Packaging;
using OpenXmlParagraph = DocumentFormat.OpenXml.Wordprocessing.Paragraph;

using OpenXmlRun = DocumentFormat.OpenXml.Wordprocessing.Run;
using OpenXmlText = DocumentFormat.OpenXml.Wordprocessing.Text;
using OpenXmlDrawing = DocumentFormat.OpenXml.Wordprocessing.Drawing;
using OpenXmlTable = DocumentFormat.OpenXml.Wordprocessing.Table;
using OpenXmlTableRow = DocumentFormat.OpenXml.Wordprocessing.TableRow;
using OpenXmlTableCell = DocumentFormat.OpenXml.Wordprocessing.TableCell;

namespace Alextor.RAG.Extractor.OpenXML;

public class Docx : IExtractor
{
    private string _Process(OpenXmlParagraph p, WordprocessingDocument word)
    {
        var content = new StringBuilder();
        var isList = false;

        if (p.ParagraphProperties?.NumberingProperties != null)
        {
            // TODO: update to set nums if its ordered
            var numProps = p.ParagraphProperties?.NumberingProperties!;
            int ilvl = numProps.NumberingLevelReference?.Val?.Value ?? 0;
            // int numId = numProps.NumberingId?.Val?.Value ?? 0;

            var tab = new string('\t', ilvl);
            isList = true;
            content.Append($"{tab}- {p.InnerText.Trim()}{Environment.NewLine}");
        }

        foreach (var run in p.Elements<OpenXmlRun>())
        {
            var text = run.GetFirstChild<OpenXmlText>();
            if (text != null && !isList)
            {
                content.Append(text.Text + Environment.NewLine);
            }

            var drawing = run.GetFirstChild<OpenXmlDrawing>();
            if (drawing != null)
            {
                var blip = drawing.Descendants<Blip>().FirstOrDefault();
                if (blip?.Embed != null)
                {
                    var relId = blip.Embed.Value;
                    if (relId == null) continue;

                    try
                    {
                        var imagePart = (ImagePart)word.MainDocumentPart!.GetPartById(relId);

                        using var imgStream = imagePart.GetStream();
                        using (var ms = new MemoryStream())
                        {
                            imgStream.CopyTo(ms);
                            ms.Seek(0, SeekOrigin.Begin);
                            var ocr = ExtractorManager.Get(ExtractorManager.ExtractorType.OCR);
                            content.Append(ocr.Extract(ms) + Environment.NewLine);
                        }
                    }
                    catch (Exception ex)
                    {
#if DEBUG
                        // Ignore image errors when the format cannot be determined.
                        // Do not use file signature checks, as they are limited and may fail to identify formats such as TIFF.
                        Console.WriteLine($"{ex.Source}.{ex.Message}\nTrace:\n{ex.StackTrace}\n");
#endif
                    }
                }
            }
        }

        return content.ToString();
    }

    private string _Process(OpenXmlTable t, WordprocessingDocument word)
    {
        var content = new StringBuilder();
        var rowN = 0;
        foreach (var row in t.Elements<OpenXmlTableRow>())
        {
            var rows = new List<string>();
            foreach (var cell in row.Elements<OpenXmlTableCell>())
            {
                foreach (var p in cell.Elements<OpenXmlParagraph>())
                {
                    rows.Add(_Process(p, word).Trim());
                }
            }
            content.Append(string.Format("| {0} |", string.Join(" | ", rows)) + Environment.NewLine);
            if (rowN == 0)
            {
                content.Append(
                    string.Format("| {0} |", string.Join(" | ", rows.Select(x => "-"))) + Environment.NewLine
                );
            }
            rowN++;
        }
        return content.ToString();
    }

    public string Extract(Stream stream)
    {
        var content = new StringBuilder();
        var headers = new List<string>();
        var footers = new List<string>();
        var body = new List<string>();
        using (var word = WordprocessingDocument.Open(stream, false))
        {
            if (
                word.MainDocumentPart == null ||
                word.MainDocumentPart.Document == null ||
                word.MainDocumentPart.Document.Body == null
            ) throw new ContentEmptyException();

            foreach (var header in word.MainDocumentPart.HeaderParts)
            {
                if (header.Header == null) continue;
                headers.Add(header.Header.InnerText);
            }
            foreach (var footer in word.MainDocumentPart.FooterParts)
            {
                if (footer.Footer == null) continue;
                footers.Add(footer.Footer.InnerText);
            }

            foreach (var element in word.MainDocumentPart.Document.Body.Elements())
            {
                if (element is OpenXmlParagraph p)
                {
                    body.Add(_Process(p, word));
                }

                if (element is OpenXmlTable t)
                {
                    body.Add(Environment.NewLine + _Process(t, word) + Environment.NewLine);
                }
            }
            // word.MainDocumentPart.Document.Body.Elements
        }

        content.Append("# Header" + Environment.NewLine);
        content.Append(string.Join(Environment.NewLine, headers) + Environment.NewLine);

        content.Append("# Body" + Environment.NewLine);
        content.Append(string.Join("", body.Where(x => !string.IsNullOrEmpty(x))) + Environment.NewLine);

        content.Append("# Footer" + Environment.NewLine);
        content.Append(string.Join(Environment.NewLine, footers) + Environment.NewLine);

        return content.ToString();
    }
}