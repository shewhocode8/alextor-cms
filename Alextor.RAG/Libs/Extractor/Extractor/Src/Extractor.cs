using System.Text;
using Alextor.RAG.Extractor.OpenXML;
namespace Alextor.RAG.Extractor;

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

    private static Tuple<string, FileType> _HandleZIP(Stream content)
    {
        var openXmlType = OpenXMLType.GetType(content);
        content.Seek(0, SeekOrigin.Begin);
        switch (openXmlType)
        {
            case OpenXMLType.Type.DOCX:
                var ex = ExtractorManager.Get(ExtractorManager.ExtractorType.DOCX);
                return Tuple.Create(
                    ex.Extract(content),
                    FileType.DOCX
                );
            case OpenXMLType.Type.PPTX:
                ex = ExtractorManager.Get(ExtractorManager.ExtractorType.PPTX);
                return Tuple.Create(
                    ex.Extract(content),
                    FileType.PPTX
                );
            case OpenXMLType.Type.XLSX:
                ex = ExtractorManager.Get(ExtractorManager.ExtractorType.XLSX);
                return Tuple.Create(
                    ex.Extract(content),
                    FileType.XLSX
                );
            default:
                break;
        }
        throw new FileNotSupportedException();
    }

    /// <summary>
    /// Does not guarantee the `content` is in a correct state.
    /// The caller must handle the disposal of stream content.
    /// </summary>
    /// <param name="content"></param>
    /// <returns>ExtractionResult</returns>
    /// <exception cref="StreamNotReadableException"></exception>
    /// <exception cref="ContentEmptyException"></exception>
    /// <exception cref="FileNotSupportedException"></exception>
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

        var fileType = Constants.FileSignatures.GetFileType(sigBuffer);
        var stContent = new StringBuilder();
        content.Seek(0, SeekOrigin.Begin);
        switch (fileType)
        {
            case FileType.PDF:
                var ex = ExtractorManager.Get(ExtractorManager.ExtractorType.PDF);
                stContent.Append(ex.Extract(content));
                break;
            case FileType.JPG:
            case FileType.PNG:
                var ocr = ExtractorManager.Get(ExtractorManager.ExtractorType.OCR);
                stContent.Append(ocr.Extract(content));
                break;
            case FileType.ZIP:
                var res = _HandleZIP(content);
                stContent.Append(res.Item1);
                fileType = res.Item2;
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