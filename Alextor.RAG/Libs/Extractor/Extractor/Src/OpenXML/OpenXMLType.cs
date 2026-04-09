using System.IO.Compression;
using System.Xml.Linq;

namespace Alextor.RAG.Extractor.OpenXML;


public class OpenXMLType
{
    private static readonly string CONTENT_TYPE_DOCX = @"/word/document.xml";
    private static readonly string CONTENT_TYPE_XLSX = @"/xl/workbook.xml";
    private static readonly string CONTENT_TYPE_PPTX = @"/ppt/presentation.xml";

    public enum Type
    {
        UNKNOWN,
        DOCX,
        XLSX,
        PPTX
    }

    private static OpenXMLType.Type _GetTypeFrom(XAttribute attr)
    {
        if (attr.Value.Contains(CONTENT_TYPE_DOCX))
        {
            return Type.DOCX;
        }
        if (attr.Value.Contains(CONTENT_TYPE_PPTX))
        {
            return Type.PPTX;
        }
        if (attr.Value.Contains(CONTENT_TYPE_XLSX))
        {
            return Type.XLSX;
        }

        return Type.UNKNOWN;
    }

    public static OpenXMLType.Type GetType(Stream stream)
    {
        using (var unzipped = new ZipArchive(stream, ZipArchiveMode.Read, leaveOpen: true))
        {
            var contentTypeXml = unzipped.GetEntry(@"[Content_Types].xml");
            if (contentTypeXml == null)
            {
                return Type.UNKNOWN;
            }
            using (var f = contentTypeXml.Open())
            {
                var xml = XDocument.Load(f);
                foreach (var node in xml.Descendants())
                {
                    foreach (var attr in node.Attributes())
                    {
                        if (attr == null) continue;
                        var t = _GetTypeFrom(attr);
                        if (t != Type.UNKNOWN)
                        {
                            return t;
                        }
                    }
                }
            }

        }

        return Type.UNKNOWN;
    }
}