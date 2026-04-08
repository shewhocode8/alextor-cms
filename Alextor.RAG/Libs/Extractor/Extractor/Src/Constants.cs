namespace Lib.Extractor.Constants;

/// <summary>
/// See https://en.wikipedia.org/wiki/List_of_file_signatures for the file signatures
/// </summary>
public enum FileSignatures : uint
{
    PNG = 0x89504E47,
    JPG_1 = 0xFFD8FFDB,
    JPG_2 = 0xFFD8FFEE,
    JPG_JFIF = 0xFFD8FFE0,
    JPG_EXIF = 0xFFD8FFE1,
}