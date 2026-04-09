using System.Collections.Concurrent;
using System.Text;
using Alextor.RAG.Extractor.Interface;
using UglyToad.PdfPig;
using UglyToad.PdfPig.Content;
using UglyToad.PdfPig.Core;
using UglyToad.PdfPig.DocumentLayoutAnalysis;
using UglyToad.PdfPig.DocumentLayoutAnalysis.PageSegmenter;
using UglyToad.PdfPig.DocumentLayoutAnalysis.ReadingOrderDetector;
using UglyToad.PdfPig.DocumentLayoutAnalysis.WordExtractor;

namespace Alextor.RAG.Extractor.PDF;

public class PdfPig : IExtractor
{
    public string Extract(Stream stream)
    {
        try
        {
            var content = new StringBuilder();
            using (PdfDocument document = PdfDocument.Open(stream))
            {
                var outputs = new ConcurrentDictionary<int, string>();

                var _lock = new object();
                Parallel.ForEach(document.GetPages(), (page) =>
                {
                    var blocks = new List<(PdfRectangle box, TextBlock? tb, IPdfImage? img)>();

                    {
                        // from example in PdfPig wiki
                        var letters = page.Letters;
                        // 1. Extract words
                        var wordExtractor = NearestNeighbourWordExtractor.Instance;
                        var words = wordExtractor.GetWords(letters);
                        // 2. Segment page
                        var pageSegmenter = DocstrumBoundingBoxes.Instance;
                        var textBlocks = pageSegmenter.GetBlocks(words);
                        // 3. Postprocessing
                        var readingOrder = UnsupervisedReadingOrderDetector.Instance;
                        var orderedTextBlocks = readingOrder.Get(textBlocks);
                        foreach (var block in orderedTextBlocks)
                        {
                            blocks.Add((block.BoundingBox, block, null));
                        }
                    }

                    {
                        foreach (var img in page.GetImages())
                        {
                            blocks.Add((img.BoundingBox, null, img));
                        }
                    }

                    blocks.Sort((left, right) =>
                    {
                        // pdfpig 0,0 xy pos is in bottom left
                        int y = right.box.Top.CompareTo(left.box.Top);
                        if (y != 0) return y;
                        return left.box.Left.CompareTo(right.box.Left);
                    });

                    var c = new StringBuilder();
                    foreach (var block in blocks)
                    {
                        if (block.tb != null)
                        {
                            c.Append(block.tb.Text.Trim() + block.tb.Separator ?? Environment.NewLine);
                        }
                        if (block.img != null)
                        {
                            if (block.img.TryGetPng(out var bytes))
                            {
                                using (var m = new MemoryStream(bytes))
                                {
                                    var ex = ExtractorManager.Get(ExtractorManager.ExtractorType.OCR);
                                    lock (_lock)
                                    {
                                        var cc = ex.Extract(m);
                                        if (!string.IsNullOrEmpty(cc))
                                            c.Append(cc);
                                    }
                                }
                            }
                        }
                    }
                    outputs[page.Number] = c.ToString();
                });

                foreach (var outp in outputs.OrderBy(x => x.Key))
                {
                    content.Append($"# Page {outp.Key}{Environment.NewLine}");
                    content.Append(outp.Value.Trim() + Environment.NewLine);
                }
            }
            return content.ToString();
        }
        catch (Exception ex)
        {
            throw new UnexpectedErrorException(ex);
        }
    }
}