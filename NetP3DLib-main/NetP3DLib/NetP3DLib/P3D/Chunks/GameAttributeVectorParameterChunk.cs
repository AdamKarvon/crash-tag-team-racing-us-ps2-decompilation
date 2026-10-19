using NetP3DLib.IO;
using NetP3DLib.P3D.Attributes;
using NetP3DLib.P3D.Enums;
using NetP3DLib.P3D.Extensions;
using System.Collections.Generic;
using System.Numerics;

namespace NetP3DLib.P3D.Chunks;

[ChunkAttributes(ChunkID)]
public class GameAttributeVectorParameterChunk : NamedChunk
{
    public const ChunkIdentifier ChunkID = ChunkIdentifier.Game_Attribute_Vector_Parameter;

    private Vector3 _value;
    public Vector3 Value
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
    public override uint DataLength => BinaryExtensions.GetP3DStringLength(Name) + sizeof(float) * 3;

    public GameAttributeVectorParameterChunk(EndianAwareBinaryReader br) : this(br.ReadP3DString(), br.ReadVector3())
    {
    }

    public GameAttributeVectorParameterChunk(string name, Vector3 value) : base(ChunkID, name)
    {
        _value = value;
    }

    protected override void WriteData(EndianAwareBinaryWriter bw)
    {
        bw.WriteP3DString(Name);
        bw.Write(Value);
    }

    protected override Chunk CloneSelf() => new GameAttributeVectorParameterChunk(Name, Value);
}
