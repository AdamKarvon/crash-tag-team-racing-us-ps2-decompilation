using NetP3DLib.IO;
using NetP3DLib.P3D.Attributes;
using NetP3DLib.P3D.Enums;
using NetP3DLib.P3D.Extensions;
using System.Collections.Generic;
using System.Numerics;

namespace NetP3DLib.P3D.Chunks;

[ChunkAttributes(ChunkID)]
public class GameAttributeMatrixParameterChunk : NamedChunk
{
    public const ChunkIdentifier ChunkID = ChunkIdentifier.Game_Attribute_Matrix_Parameter;

    private Matrix4x4 _value;
    public Matrix4x4 Value
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

            data.AddRange(BinaryExtensions.GetP3DStringBytes(Name));
            data.AddRange(BinaryExtensions.GetBytes(Value));

            return [.. data];
        }
    }
    public override uint DataLength => BinaryExtensions.GetP3DStringLength(Name) + sizeof(float) * 16;

    public GameAttributeMatrixParameterChunk(EndianAwareBinaryReader br) : this(br.ReadP3DString(), br.ReadMatrix4x4())
    {
    }

    public GameAttributeMatrixParameterChunk(string name, Matrix4x4 value) : base(ChunkID, name)
    {
        _value = value;
    }

    protected override void WriteData(EndianAwareBinaryWriter bw)
    {
        bw.WriteP3DString(Name);
        bw.Write(Value);
    }

    protected override Chunk CloneSelf() => new GameAttributeMatrixParameterChunk(Name, Value);
}
