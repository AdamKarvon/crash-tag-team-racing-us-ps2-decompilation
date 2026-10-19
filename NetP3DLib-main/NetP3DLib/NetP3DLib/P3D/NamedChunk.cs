using NetP3DLib.P3D.Enums;
using NetP3DLib.P3D.Exceptions;
using NetP3DLib.P3D.Extensions;
using NetP3DLib.P3D.Types;
using System.Collections.Generic;

namespace NetP3DLib.P3D;

public abstract class NamedChunk : Chunk
{
    internal readonly P3DString _name;
    public string Name
    {
        get => _name?.Value ?? string.Empty;
        set => _name.Value = value;
    }

    protected NamedChunk(uint ID, string name) : base(ID)
    {
        _name = new(this, name, nameof(Name));
    }

    protected NamedChunk(ChunkIdentifier ID, string name) : this((uint)ID, name)
    {
    }

    public override IEnumerable<InvalidP3DException> ValidateChunk()
    {
        foreach (var error in base.ValidateChunk())
            yield return error;

        if (!Name.IsValidP3DString())
            yield return new InvalidP3DStringException(this, nameof(Name), Name);
    }

    public override string ToString() => $"\"{Name}\" ({GetChunkType(this)} (0x{ID:X}))";
}