using NetP3DLib.IO;
using NetP3DLib.P3D.Attributes;
using NetP3DLib.P3D.Enums;
using NetP3DLib.P3D.Extensions;
using System.Collections.Generic;
using System.Drawing;

namespace NetP3DLib.P3D.Chunks;

[ChunkAttributes(ChunkID)]
public class ShaderColourParameterChunk : ParamChunk
{
    public const ChunkIdentifier ChunkID = ChunkIdentifier.Shader_Colour_Parameter;

    private Color _value;
    public Color Value
    {
        get => _value;
        set
        {
            if (_value == value)
                return;
    
            _value = value;
            OnPropertyChanged(nameof(Value));
        }
    }
    

    public override byte[] DataBytes
    {
        get
        {
            var data = new List<byte>((int)DataLength);

            data.AddRange(BinaryExtensions.GetFourCCBytes(Param));
            data.AddRange(BinaryExtensions.GetBytes(Value));

            return [.. data];
        }
    }
    public override uint DataLength => 4 + sizeof(uint);

    public ShaderColourParameterChunk(EndianAwareBinaryReader br) : this(br.ReadFourCC(), br.ReadColor())
    {
    }

    public ShaderColourParameterChunk(string param, Color value) : base(ChunkID, param)
    {
        _value = value;
    }

    protected override void WriteData(EndianAwareBinaryWriter bw)
    {
        bw.WriteFourCC(Param);
        bw.Write(Value);
    }

    protected override Chunk CloneSelf() => new ShaderColourParameterChunk(Param, Value);
}
