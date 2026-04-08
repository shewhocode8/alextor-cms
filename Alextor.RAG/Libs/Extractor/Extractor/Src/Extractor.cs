using System.Buffers.Binary;
using System.Diagnostics;
using System.Net;
using System.Text;
using Lib.Extractor.OCR;
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

    /// <summary>
    /// let the caller handle disposal of stream.
    /// </summary>
    /// <param name="content"></param>
    /// <param name="filename"></param>
    /// <returns></returns>
    /// <exception cref="StreamNotReadableException"></exception>
    /// <exception cref="ContentEmptyException"></exception>
    public static ExtractionResult Parse(Stream content, string? filename = null)
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
                var ocr = new TesseractOCR();
                stContent.Append(ocr.Extract(content));
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