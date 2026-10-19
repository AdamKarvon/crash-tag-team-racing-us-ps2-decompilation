using NetP3DLib.IO;
using NetP3DLib.P3D.Attributes;
using NetP3DLib.P3D.Enums;
using NetP3DLib.P3D.Extensions;
using System;
using System.Collections.Generic;

namespace NetP3DLib.P3D.Chunks;

[ChunkAttributes(ChunkID)]
public class ShaderFloatParameterChunk : ParamChunk
{
    public const ChunkIdentifier ChunkID = ChunkIdentifier.Shader_Float_Parameter;

    private float _value;
    public float Value
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
            data.AddRange(BitConverter.GetBytes(Value));

            return [.. data];
        }
    }
    public override uint DataLength => 4 + sizeof(float);

    public ShaderFloatParameterChunk(EndianAwareBinaryReader br) : this(br.ReadFourCC(), br.ReadSingle())
    {
    }

    public ShaderFloatParameterChunk(string param, float value) : base(ChunkID, param)
    {
        _value = value;
    }

    protected override void WriteData(EndianAwareBinaryWriter bw)
    {
        bw.WriteFourCC(Param);
        bw.Write(Value);
    }

    protected override Chunk CloneSelf() => new ShaderFloatParameterChunk(Param, Value);
}
