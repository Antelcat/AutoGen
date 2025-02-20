using System;

namespace Antelcat.AutoGen.AssemblyWeavers.Exceptions;

public class WeaverException(string message, Exception innerException) : Exception(message, innerException)
{
    public required string WeaverName { get; init; }

    public WeaverException(Exception innerException)
        : this("Weaver throw an exception, see inner:", innerException)
    {
    }

    public override string ToString() => $"Weaver:[{WeaverName}] throws an exception: \n{InnerException}";
}