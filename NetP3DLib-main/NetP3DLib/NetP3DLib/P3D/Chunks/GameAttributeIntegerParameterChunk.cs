using NetP3DLib.IO;
using NetP3DLib.P3D.Attributes;
using NetP3DLib.P3D.Enums;
using NetP3DLib.P3D.Extensions;
using System;
using System.Collections.Generic;

namespace NetP3DLib.P3D.Chunks;

[ChunkAttributes(ChunkID)]
public class GameAttributeIntegerParameterChunk : NamedChunk
{
    public const ChunkIdentifier ChunkID = ChunkIdentifier.Game_Attribute_Integer_Parameter;

    private uint _value;
    public uint Value
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
            data.AddRange(BitConverter.GetBytes(Value));

            return [.. data];
        }
    }
    public override uint DataLength => BinaryExtensions.GetP3DStringLength(Name) + sizeof(uint);

    public GameAttributeIntegerParameterChunk(EndianAwareBinaryReader br) : this(br.ReadP3DString(), br.ReadUInt32())
    {
    }

    public GameAttributeIntegerParameterChunk(string name, uint value) : base(ChunkID, name)
    {
        _value = value;
    }

    protected override void WriteData(EndianAwareBinaryWriter bw)
    {
        bw.WriteP3DString(Name);
        bw.Write(Value);
    }

    protected override Chunk CloneSelf() => new GameAttributeIntegerParameterChunk(Name, Value);
}
