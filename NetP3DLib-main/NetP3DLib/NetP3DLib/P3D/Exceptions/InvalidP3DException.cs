using System;

namespace NetP3DLib.P3D.Exceptions;
public class InvalidP3DException : Exception
{
    public Chunk? Chunk { get; }

    public InvalidP3DException(Chunk? chunk) : base()
    {
        Chunk = chunk;
    }

    public InvalidP3DException(Chunk? chunk, string message) : base(message)
    {
        Chunk = chunk;
    }

    public InvalidP3DException(Chunk? chunk, string message, Exception innerException) : base(message, innerException)
    {
        Chunk = chunk;
    }
}
