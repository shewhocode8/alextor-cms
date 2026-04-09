using System.Buffers.Binary;

namespace Alextor.RAG.Extractor.Constants;

/// <summary>
/// See https://en.wikipedia.org/wiki/List_of_file_signatures for the file signatures
/// </summary>
public class FileSignatures
{
    public static readonly uint PNG = 0x89504E47;
    public static readonly uint[] JPG =
    [
        0xFFD8FFDB,
        0xFFD8FFEE,
        0xFFD8FFE0,
        0xFFD8FFE1
    ];
    public static readonly ulong PDF = 0x255044462D000000;

    public static readonly uint[] ZIP =
    {
        0x504B0506,
        0x504B0304,
        0x504B0708
    };

    public static FileType GetFileType(byte[] buffer)
    {
        if (buffer.Length >= sizeof(uint))
        {
            var fileSig = BinaryPrimitives.ReadUInt32BigEndian(buffer.Take(sizeof(uint)).ToArray());
            if (fileSig == PNG)
            {
                return FileType.PNG;
            }
            {
                foreach (var sig in JPG)
                {
                    if (fileSig == sig)
                    {
                        return FileType.JPG;
                    }
                }
            }
            {
                foreach (var sig in ZIP)
                {
                    if (sig == fileSig)
                    {
                        return FileType.ZIP;
                    }
                }
            }
        }
        if (buffer.Length >= sizeof(ulong))
        {
            // The pdf signature is 5bytes long
            // so we do an XNOR to the signature which is 8bytes long
            // and do a bitwise AND on the mask to check if all the first 5bytes matched
            ulong pdfMask = 0xFFFFFFFFFF000000;
            var fileSig = BinaryPrimitives.ReadUInt64BigEndian(buffer.Take(sizeof(ulong)).ToArray());
            if ((~(fileSig ^ PDF) & pdfMask) == pdfMask)
            {
                return FileType.PDF;
            }
        }
        return FileType.Txt;
    }
}