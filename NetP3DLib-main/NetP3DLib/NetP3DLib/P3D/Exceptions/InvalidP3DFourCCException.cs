using System.Text;

namespace NetP3DLib.P3D.Exceptions;
internal class InvalidP3DFourCCException : InvalidP3DException
{
    public string Variable { get; }
    public string? Value { get; }
    public int ByteCount { get; }

    public InvalidP3DFourCCException(Chunk? chunk, string variable, string? str) : base(chunk, $"Invalid FourCC for \"{variable}\". FourCCs cannot be null and must not exceed 4 bytes.")
    {
        Variable = variable;
        Value = str;
        ByteCount = Value is null ? 0 : Encoding.UTF8.GetByteCount(str);
    }
}
