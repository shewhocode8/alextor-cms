using System.Collections.Immutable;
using System.Text;
using Alextor.RAG.Extractor.Interface;
using DocumentFormat.OpenXml.Drawing;
using DocumentFormat.OpenXml.Packaging;
using W = DocumentFormat.OpenXml.Wordprocessing;

namespace Alextor.RAG.Extractor.OpenXML;

public class Docx : IExtractor
{
    private string _Process(W.Run run, WordprocessingDocument word)
    {
        var content = new StringBuilder();
        foreach (var element in run.Elements())
        {
            if (element is W.Text text)
            {
                content.Append(text.InnerText.Trim());
            }
            else if (element is W.Drawing drawing)
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
        return content.ToString().Trim();
    }

    private string _Process(W.Paragraph p, WordprocessingDocument word)
    {
        var content = new StringBuilder();

        if (p.ParagraphProperties?.NumberingProperties != null)
        {
            // TODO: update to set nums if its ordered
            var numProps = p.ParagraphProperties?.NumberingProperties!;
            int ilvl = numProps.NumberingLevelReference?.Val?.Value ?? 0;
            // int numId = numProps.NumberingId?.Val?.Value ?? 0;

            var tab = new string('\t', ilvl);
            content.Append($"{tab}- ");
        }

        foreach (var element in p.Elements())
        {
            if (element is W.Hyperlink hyperlink)
            {
                var link = word.MainDocumentPart!.HyperlinkRelationships
                    .FirstOrDefault(x => x.Id == hyperlink.Id)?
                    .Uri.ToString();
                if (string.IsNullOrEmpty(link)) continue;
                var sp = (content.Length > 0)? " ":"";
                content.Append($"{sp}[{hyperlink.InnerText.Trim()}]({link})");
            }
            else if (element is W.Run run)
            {
                content.Append(_Process(run, word));
            }
        }

        return content.ToString();
    }

    private string _Process(W.Table t, WordprocessingDocument word)
    {
        var content = new StringBuilder();
        var rowN = 0;
        foreach (var row in t.Elements<W.TableRow>())
        {
            var rows = new List<string>();
            foreach (var cell in row.Elements<W.TableCell>())
            {
                foreach (var p in cell.Elements<W.Paragraph>())
                {
                    rows.Add(_Process(p, word));
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
                if (element is W.Paragraph p)
                {
                    var c = _Process(p, word);
                    if (!string.IsNullOrEmpty(c))
                        body.Add(c + Environment.NewLine);
                }
                else if (element is W.Table t)
                {
                    var c = _Process(t, word);
                    if (!string.IsNullOrEmpty(c))
                        body.Add(Environment.NewLine + c + Environment.NewLine);
                }
            }
            // word.MainDocumentPart.Document.Body.Elements
        }

        content.Append("# Header" + Environment.NewLine);
        content.Append(string.Join(" ", headers) + Environment.NewLine);

        content.Append("# Body" + Environment.NewLine);
        content.Append(string.Join("", body.Where(x => !string.IsNullOrEmpty(x))) + Environment.NewLine);

        content.Append("# Footer" + Environment.NewLine);
        content.Append(string.Join(Environment.NewLine, footers) + Environment.NewLine);

        return content.ToString();
    }
}