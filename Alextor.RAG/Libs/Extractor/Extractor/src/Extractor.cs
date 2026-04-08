using System.Buffers.Binary;
using System.Diagnostics;
using System.Net;
using System.Text;
namespace Lib.Extractor;

public struct ExtractionResult
{
    public string Content;
    public FileType FileType;
}

public static class Parser
{
    private static string _StreamToString(Stream content)
    {
        var st = new StringBuilder();
        var buffer = new byte[2048];
        int bytesRead;
        while ((bytesRead = content.Read(buffer, 0, buffer.Length)) > 0)
        {
            st.Append(Encoding.UTF8.GetString(buffer, 0, bytesRead));
            buffer.AsSpan().Clear();
        }

        return st.ToString();
    }

    private static string _GetTessDataPath()
    {
        var dir = Path.Join(AppContext.BaseDirectory, "tessdata");
        Directory.CreateDirectory(dir);
        var filename = Path.Join(dir, "eng.traineddata");
        if (Path.Exists(filename))
        {
            return dir;
        }

        try
        {
            using (var client = new HttpClient())
            {
                var b = client.GetByteArrayAsync("https://raw.githubusercontent.com/tesseract-ocr/tessdata/refs/heads/main/eng.traineddata")
                    .GetAwaiter()
                    .GetResult();
                if (b == null || b.Length == 0)
                {
                    return "";
                }


                using (var f = File.OpenWrite(filename))
                {
                    f.Write(b);
                }
                return dir;
            }
        }
        catch (Exception ex)
        {
            throw new UnexpectedErrorException(ex);
        }
    }

    private static string _OCRImage(Stream stream)
    {
        try
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
        catch (Exception ex)
        {
            throw new UnexpectedErrorException(ex);
        }
    }

    private static FileType _CheckFileSig(byte[] buffer)
    {
        if (buffer.Length >= 4)
        {
            uint fileSig = BinaryPrimitives.ReadUInt32BigEndian(buffer.Take(4).ToArray());
            if (fileSig == (uint)Constants.FileSignatures.PNG)
            {
                return FileType.PNG;
            }
            {
                var jpgSignatures = new uint[]
                {
                (uint)Constants.FileSignatures.JPG_1,
                (uint)Constants.FileSignatures.JPG_2,
                (uint)Constants.FileSignatures.JPG_EXIF,
                (uint)Constants.FileSignatures.JPG_JFIF
                };
                foreach (var sig in jpgSignatures)
                {
                    if (fileSig == sig)
                    {
                        return FileType.JPG;
                    }
                }
            }
        }
        return FileType.Txt;
    }

    // let the caller handle disposal of stream
    public static ExtractionResult Parse(Stream content)
    {
        if (!content.CanRead)
            throw new StreamNotReadableException();
        if (!content.CanSeek)
            throw new StreamNotReadableException();

        var sigBytes = new byte[10];

        int bytesRead = content.Read(sigBytes, 0, sigBytes.Length);
        if (bytesRead == 0)
            throw new ContentEmptyException();

        var sigBuffer = sigBytes.Take(bytesRead).ToArray();

        var fileType = _CheckFileSig(sigBuffer);
        var stContent = new StringBuilder();
        content.Seek(0, SeekOrigin.Begin);
        switch (fileType)
        {
            case FileType.JPG:
            case FileType.PNG:
                stContent.Append(_OCRImage(content));
                break;
            case FileType.Txt:
                stContent.Append(_StreamToString(content));
                break;
        }

        return new ExtractionResult()
        {
            Content = stContent.ToString(),
            FileType = fileType
        };
    }
}