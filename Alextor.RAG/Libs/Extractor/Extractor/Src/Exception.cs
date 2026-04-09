using System;
namespace Alextor.RAG.Extractor;

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
    public UnexpectedErrorException(Exception ex) : base($"{ex.Source}.{ex.Message}\nTrace:{ex.StackTrace}\n") {}
}

public class ExtractorNotConfiguredException : ExtractorException
{
    public ExtractorNotConfiguredException(string exName) : base($"Extractor {exName} is not configured") {}
}