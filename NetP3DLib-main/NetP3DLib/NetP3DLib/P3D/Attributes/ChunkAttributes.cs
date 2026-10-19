using NetP3DLib.P3D.Enums;
using System;

namespace NetP3DLib.P3D.Attributes;

[AttributeUsage(AttributeTargets.Class)]
public class ChunkAttributes : Attribute
{
    public uint Identifier { get; }

    public ChunkAttributes(uint identifier)
    {
        Identifier = identifier;
    }

    public ChunkAttributes(ChunkIdentifier identifier) : this((uint)identifier) { }
}
