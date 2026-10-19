using NetP3DLib.IO;
using NetP3DLib.P3D.Attributes;
using NetP3DLib.P3D.Enums;
using NetP3DLib.P3D.Exceptions;
using NetP3DLib.P3D.Extensions;
using NetP3DLib.P3D.Types;
using System.Collections.Generic;

namespace NetP3DLib.P3D.Chunks;

[ChunkAttributes(ChunkID)]
public class ShaderTextureParameterChunk : ParamChunk
{
    public const ChunkIdentifier ChunkID = ChunkIdentifier.Shader_Texture_Parameter;

    private readonly P3DString _value;
    public string Value
    {
        get => _value?.Value ?? string.Empty;
        set => _value.Value = value;
    }

    public override byte[] DataBytes
    {
        get
        {
            var data = new List<byte>((int)DataLength);

            data.AddRange(BinaryExtensions.GetFourCCBytes(Param));
            data.AddRange(BinaryExtensions.GetP3DStringBytes(Value));

            return [.. data];
        }
    }
    public override uint DataLength => 4 + BinaryExtensions.GetP3DStringLength(Value);

    public ShaderTextureParameterChunk(EndianAwareBinaryReader br) : this(br.ReadFourCC(), br.ReadP3DString())
    {
    }

    public ShaderTextureParameterChunk(string param, string value) : base(ChunkID, param)
    {
        _value = new(this, value, nameof(Value));
    }

    public override IEnumerable<InvalidP3DException> ValidateChunk()
    {
        foreach (var error in base.ValidateChunk())
            yield return error;

        if (!Value.IsValidP3DString())
            yield return new InvalidP3DStringException(this, nameof(Value), Value);
    }

    protected override void WriteData(EndianAwareBinaryWriter bw)
    {
        bw.WriteFourCC(Param);
        bw.WriteP3DString(Value);
    }

    protected override Chunk CloneSelf() => new ShaderTextureParameterChunk(Param, Value);
}