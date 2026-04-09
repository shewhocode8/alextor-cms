using Alextor.RAG.Extractor.Interface;
using Alextor.RAG.Extractor.OCR;
using Alextor.RAG.Extractor.OpenXML;
using Alextor.RAG.Extractor.PDF;

namespace Alextor.RAG.Extractor;

public class ExtractorManager
{
    public enum ExtractorType
    {
        OCR,
        PDF,
        DOCX,
        XLSX,
        PPTX
    }

    public static IExtractor Get(ExtractorType exType)
    {
        switch (exType)
        {
            case ExtractorType.OCR:
                return new TesseractOCR();
            case ExtractorType.PDF:
                return new PdfPig();
            case ExtractorType.DOCX:
                return new Docx();
        }

        throw new ExtractorNotConfiguredException(exType.ToString());
    }
}
