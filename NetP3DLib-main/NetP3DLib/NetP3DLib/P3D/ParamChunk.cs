using NetP3DLib.P3D.Attributes;
using NetP3DLib.P3D.Enums;
using NetP3DLib.P3D.Exceptions;
using NetP3DLib.P3D.Extensions;
using NetP3DLib.P3D.Types;
using System.Collections.Generic;

namespace NetP3DLib.P3D;

public abstract class ParamChunk : Chunk
{
    internal readonly FourCC _param;
    [MaxLength(4)]
    public string Param
    {
        get => _param.Value ?? string.Empty;
        set => _param.Value = value;
    }

    public ParamChunk(uint ID, string param) : base(ID)
    {
        _param = new(this, param, nameof(Param));
    }

    public ParamChunk(ChunkIdentifier ID, string param) : this((uint)ID, param)
    {
    }

    public override IEnumerable<InvalidP3DException> ValidateChunk()
    {
        foreach (var error in base.ValidateChunk())
            yield return error;

        if (!Param.IsValidFourCC())
            yield return new InvalidP3DFourCCException(this, nameof(Param), Param);
    }

    public override string ToString() => $"\"{Param}\" ({GetChunkType(this)} (0x{ID:X}))";
}