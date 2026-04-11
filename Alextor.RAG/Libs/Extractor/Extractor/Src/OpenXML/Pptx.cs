using System.Text;
using Alextor.RAG.Extractor.Interface;
using DocumentFormat.OpenXml.Packaging;
using DocumentFormat.OpenXml.Presentation;
using P = DocumentFormat.OpenXml.Drawing;

namespace Alextor.RAG.Extractor.OpenXML;

public class Pptx : IExtractor
{
    private string _Process(Slide slide, P.Run run)
    {
        var hyperlinks = run.RunProperties?.Elements<P.HyperlinkOnClick>();
        if (hyperlinks == null || hyperlinks.Count() == 0)
        {
            return run.InnerText.Trim() ?? "";
        }
        else
        {
            foreach (var hl in hyperlinks)
            {
                var hyperlink = slide.SlidePart!.HyperlinkRelationships
                    .FirstOrDefault(h => h.Id == hl.Id);

                var link = hyperlink?.Uri.ToString();
                var text = run.Text?.Text.Trim();
                if (!string.IsNullOrEmpty(link))
                {
                    return $"[{text}]({link})";
                }
            }
        }
        return "";
    }

    private string _Process(Slide slide)
    {
        var content = new StringBuilder();
        var tables = slide.Descendants<P.Table>();
        var pInsideTables = slide
            .Descendants<P.Table>()
            .SelectMany(t => t.Descendants<P.Paragraph>())
            .ToHashSet();

        foreach (var p in slide.Descendants<P.Paragraph>())
        {
            if (pInsideTables.Contains(p)) continue;
            if (p.Ancestors<P.Table>().Any()) continue;
            var numbered = p.ParagraphProperties?.GetFirstChild<P.AutoNumberedBullet>();
            var bulleted = p.ParagraphProperties?.GetFirstChild<P.CharacterBullet>();
            var level = p.ParagraphProperties?.Level?.Value ?? 0;

            if (level > 0)
                content.Append($"{new string('\t', level)} ");
            if (bulleted != null)
                content.Append($"{bulleted.Char ?? "-"} ");
            if (numbered != null)
                content.Append("- ");
            
            foreach (var r in p.Elements<P.Run>())
            {
                content.Append(_Process(slide, r));
            }
            content.Append(Environment.NewLine);
        }

        foreach (var tbl in tables)
        {
            content.Append(Environment.NewLine + _Process(slide, tbl) + Environment.NewLine); }

        foreach (var pic in slide.Descendants<Picture>())
        {
            var relId = pic.BlipFill?.Blip?.Embed?.Value;

            if (string.IsNullOrEmpty(relId)) continue;
            var imagePart = (ImagePart)slide.SlidePart!.GetPartById(relId);

            try
            {
                using var stream = imagePart.GetStream();
                using (var ms = new MemoryStream())
                {
                    stream.CopyTo(ms);
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

        return content.ToString();
    }

    private string _Process(Slide slide, P.Table table)
    {
        var content = new StringBuilder();
        var rowN = 0;
        foreach (P.TableRow row in table.Elements<P.TableRow>())
        {
            var rows = new List<string>();
            foreach (P.TableCell cell in row.Elements<P.TableCell>())
            {
                foreach (var p in cell.Descendants<P.Paragraph>())
                {
                    var cc = new StringBuilder();
                    foreach (var r in p.Descendants<P.Run>())
                    {
                        var sp = (cc.Length > 0) ? " " : "";
                        cc.Append(sp+_Process(slide, r));
                    }
                    rows.Add(cc.ToString());
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
        using (var document = PresentationDocument.Open(stream, false))
        {
            if (
                document.PresentationPart == null ||
                document.PresentationPart.Presentation == null ||
                document.PresentationPart.Presentation.SlideIdList == null
            )
                throw new ContentEmptyException();

            var slideIdList = document.PresentationPart.Presentation.SlideIdList;

            var slideN = 0;
            foreach (SlideId slideId in slideIdList)
            {
                if (slideId.RelationshipId == null) continue;
                var part = document.PresentationPart.GetPartById(slideId.RelationshipId!);
                var slidePart = part as SlidePart;
                var slide = slidePart?.Slide;
                if (slide == null || slidePart == null) continue;

                content.Append($"# Slide {slideN+1}{Environment.NewLine}");
                var c = _Process(slide);
                if (string.IsNullOrEmpty(c)) continue;
                content.Append(c);

                slideN++;
            }
        }
        return content.ToString();
    }
}