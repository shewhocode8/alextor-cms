using System;
namespace Lib.Extractor;

public class ExtractorException : Exception
{
    public ExtractorException(string message) : base(message) { }
}

public class ContentEmptyException : ExtractorException
{
    public ContentEmptyException() : base("The content is empty.") {}
}

public class StreamNotReadableException : ExtractorException
{
    public StreamNotReadableException() : base("The stream is not readable.") {}
}

public class StreamNotSeekableException : ExtractorException
{
    public StreamNotSeekableException() : base("The stream is not seekable.") {}
}

public class UnexpectedErrorException : ExtractorException
{
    public UnexpectedErrorException(Exception ex) : base(string.Format("{0}.{1}\nTrace:{2}\n", ex.Source, ex.Message, ex.StackTrace)) {}
}