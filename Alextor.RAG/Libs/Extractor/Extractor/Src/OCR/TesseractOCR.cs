using System.Diagnostics;
using ImageMagick;
using Alextor.RAG.Extractor.Interface;

namespace Alextor.RAG.Extractor.OCR;

/// <summary>
/// This Extractor is not thread safe
/// </summary>
public class TesseractOCR : IExtractor
{
    private object _lock = new object();
    private string _GetTessDataPath()
    {

        var tessdata_uri = Environment.GetEnvironmentVariable("ALEXTOR_RAG_TESSERACT_TESSDATA_URI");
        if (tessdata_uri == null)
        {
            tessdata_uri = "https://raw.githubusercontent.com/tesseract-ocr/tessdata/refs/heads/main/eng.traineddata";
        }

        var uri = new Uri(tessdata_uri);
        var dir = Path.Join(AppContext.BaseDirectory, "tessdata");
        Directory.CreateDirectory(dir);
        var filename = Path.Join(dir, uri.Segments.Last());
        if (Path.Exists(filename))
        {
            return dir;
        }

        try
        {
            lock (_lock)
            {
                using (var client = new HttpClient())
                {
                    var b = client.GetStreamAsync(tessdata_uri)
                        .GetAwaiter()
                        .GetResult();
                    if (b == null)
                    {
                        return "";
                    }

                    using (var f = File.OpenWrite(filename))
                    {
                        b.CopyTo(f);
                    }
                    var fileInfo = new FileInfo(filename);
                    if (fileInfo.Length == 0)
                    {
                        return "";
                    }
                    return dir;
                }
            }
        }
        catch (Exception ex)
        {
            throw new UnexpectedErrorException(ex);
        }
    }

    private Stream _PreprocessImg(Stream stream)
    {
        // using var imageFromStream = new MagickImage(memStream);
        var magick = new MagickImage(stream);
        magick.Modulate(new Percentage(100), new Percentage(0), new Percentage(100));
        magick.BrightnessContrast(new Percentage(0), new Percentage(50));
        var s = new MemoryStream();
        magick.Write(s);
#if DEBUG
        try
        {
            var path = Path.Join(AppContext.BaseDirectory, "sample.png");
            if (Path.Exists(path))
            {
                File.Delete(path);
            }
            magick.Write(path);
        } catch {}
#endif
        s.Seek(0, SeekOrigin.Begin);
        return s;
    }

    public string _StartExtraction(Stream stream)
    {
        // not very effective on images with design
        // but should work on img documents
        var tessdataDir = _GetTessDataPath();
        var psi = new ProcessStartInfo
        {
            FileName = "tesseract",
            Arguments = string.Format("stdin stdout --oem 1 --psm 6 --tessdata-dir {0}", tessdataDir),
            RedirectStandardInput = true,
            RedirectStandardOutput = true,
            UseShellExecute = false,
        };

        using var process = Process.Start(psi)!;

        stream.CopyTo(process.StandardInput.BaseStream);
        process.StandardInput.Close();

        string result = process.StandardOutput.ReadToEnd();
        process.WaitForExit();

        return result.Trim();
    }

    public string Extract(Stream stream)
    {
        try
        {
            using (var copyStream = new MemoryStream())
            {
                stream.CopyTo(copyStream);
                stream.Seek(0, SeekOrigin.Begin);
                copyStream.Seek(0, SeekOrigin.Begin);

                var task = Task.Run(() => _StartExtraction(stream));
                var task2 = Task.Run(() =>
                {
                    using (var processedImg = _PreprocessImg(copyStream))
                    {
                        return _StartExtraction(processedImg);
                    }
                });
                var results = Task.WhenAll(task, task2).GetAwaiter().GetResult();
                return (results[0].Length > results[1].Length) ? results[0] : results[1];
            }
        }
        catch (Exception ex)
        {
            throw new UnexpectedErrorException(ex);
        }
    }
}