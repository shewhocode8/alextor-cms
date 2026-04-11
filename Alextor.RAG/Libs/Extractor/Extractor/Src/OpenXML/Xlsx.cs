using System.Text;
using Alextor.RAG.Extractor.Interface;
using DocumentFormat.OpenXml.Packaging;
using DocumentFormat.OpenXml.Spreadsheet;

namespace Alextor.RAG.Extractor.OpenXML;

public class Xlsx : IExtractor
{
    private string _Process(SpreadsheetDocument document, Worksheet worksheet, Cell cell)
    {
        if (cell.CellValue == null) return "";

        string value = cell.CellValue.InnerText;

        if (cell.DataType != null && cell.DataType == CellValues.SharedString)
        {
            var hyperlink = worksheet.Descendants<Hyperlink>().FirstOrDefault(x => x.Reference != null && x.Reference == cell.CellReference);
            var sstPart = document.WorkbookPart!.SharedStringTablePart;

            if (hyperlink != null)
            {
                var link = worksheet.WorksheetPart!.HyperlinkRelationships.FirstOrDefault(x => x.Id == hyperlink.Id)?
                            .Uri.ToString();
                if (!string.IsNullOrEmpty(link))
                    value = $"[{hyperlink.Display}]({link})";
            }
            else if (sstPart == null || sstPart!.SharedStringTable == null) 
                value = sstPart!.SharedStringTable!.ElementAt(int.Parse(value)).InnerText;
        }

        return value;
    }

    private string _Process(DrawingsPart drawingsPart)
    {
        // if (drawingsPart.WorksheetDrawing == null) return "";
        var content = new StringBuilder();
        foreach (var img in drawingsPart.ImageParts)
        {
            if (img == null) continue;
            try
            {
                using var imgStream = img.GetStream();
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

        return content.ToString();
    }

    private string _Process(SpreadsheetDocument document, Worksheet worksheet, SheetData sheetData)
    {
        var content = new StringBuilder();
        var rowN = 0;
        foreach (var row in sheetData.Elements<Row>())
        {
            var rows = new List<string>();
            foreach (var cell in row.Elements<Cell>())
            {
                rows.Add(_Process(document, worksheet, cell));
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
        using (var document = SpreadsheetDocument.Open(stream, false))
        {
            var workbookPart = document.WorkbookPart;
            if (
                workbookPart == null || 
                workbookPart.Workbook == null ||
                workbookPart.Workbook.Sheets == null
            ) throw new ContentEmptyException();

            foreach (var worksheetPart in workbookPart.WorksheetParts)
            {
                if (worksheetPart == null || worksheetPart.Worksheet == null) continue;
                worksheetPart.Worksheet.Elements<SheetData>();
                var relId = workbookPart.GetIdOfPart(worksheetPart);
                var sheet = workbookPart.Workbook.Sheets.Elements<Sheet>()
                    .FirstOrDefault((x) => x.Id == relId);

                if (sheet != null)
                {
                    content.Append($"# {sheet.Name}{Environment.NewLine}");
                }

                foreach (var element in worksheetPart.Worksheet.Elements())
                {
                    // if (element is Hyperlinks hyperlinks)
                    // {
                    //     foreach (var hyperlink in hyperlinks.Elements<Hyperlink>())
                    //     {
                    //         var link = worksheet.WorksheetPart!.HyperlinkRelationships
                    //             .FirstOrDefault(x => x.Id == hyperlink.Id)?
                    //             .Uri.ToString();
                    //         if (string.IsNullOrEmpty(link)) continue;
                    //         content.Append($" [{hyperlink.Display}]({link})");
                    //     }
                    // }
                    if (element is SheetData sheetData)
                    {
                        content.Append(Environment.NewLine + _Process(document, worksheetPart.Worksheet, sheetData) + Environment.NewLine);
                    }
                }

                if (worksheetPart.DrawingsPart == null)
                    continue;
                content.Append(_Process(worksheetPart.DrawingsPart) + Environment.NewLine);
            }
            return content.ToString();
        }
    }
}