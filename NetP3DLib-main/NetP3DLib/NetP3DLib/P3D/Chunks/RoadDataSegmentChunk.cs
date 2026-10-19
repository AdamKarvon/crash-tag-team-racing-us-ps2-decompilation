using NetP3DLib.IO;
using NetP3DLib.P3D.Attributes;
using NetP3DLib.P3D.Enums;
using NetP3DLib.P3D.Extensions;
using System;
using System.Collections.Generic;
using System.Numerics;

namespace NetP3DLib.P3D.Chunks;

[ChunkAttributes(ChunkID)]
public class RoadDataSegmentChunk : NamedChunk
{
    public const ChunkIdentifier ChunkID = ChunkIdentifier.Road_Data_Segment;

    private uint _type;
    public uint Type
    {
        get => _type;
        set
        {
            if (_type == value)
                return;
    
            _type = value;
            OnPropertyChanged(nameof(Type));
        }
    }
    
    private uint _lanes;
    public uint Lanes
    {
        get => _lanes;
        set
        {
            if (_lanes == value)
                return;
    
            _lanes = value;
            OnPropertyChanged(nameof(Lanes));
        }
    }
    
    private uint _hasShoulder;
    public bool HasShoulder
    {
        get => _hasShoulder != 0;
        set
        {
            if (HasShoulder == value)
                return;

            _hasShoulder = value ? 1u : 0u;
            OnPropertyChanged(nameof(HasShoulder));
        }
    }
    
    private Vector3 _direction;
    public Vector3 Direction
    {
        get => _direction;
        set
        {
            if (_direction == value)
                return;
    
            _direction = value;
            OnPropertyChanged(nameof(Direction));
        }
    }
    
    private Vector3 _top;
    public Vector3 Top
    {
        get => _top;
        set
        {
            if (_top == value)
                return;
    
            _top = value;
            OnPropertyChanged(nameof(Top));
        }
    }
    
    private Vector3 _bottom;
    public Vector3 Bottom
    {
        get => _bottom;
        set
        {
            if (_bottom == value)
                return;
    
            _bottom = value;
            OnPropertyChanged(nameof(Bottom));
        }
    }
    

    public override byte[] DataBytes
    {
        get
        {
            var data = new List<byte>((int)DataLength);

            data.AddRange(BinaryExtensions.GetP3DStringBytes(Name));
            data.AddRange(BitConverter.GetBytes(Type));
            data.AddRange(BitConverter.GetBytes(Lanes));
            data.AddRange(BitConverter.GetBytes(_hasShoulder));
            data.AddRange(BinaryExtensions.GetBytes(Direction));
            data.AddRange(BinaryExtensions.GetBytes(Top));
            data.AddRange(BinaryExtensions.GetBytes(Bottom));

            return [.. data];
        }
    }
    public override uint DataLength => BinaryExtensions.GetP3DStringLength(Name) + sizeof(uint) + sizeof(uint) + sizeof(uint) + sizeof(float) * 3 + sizeof(float) * 3 + sizeof(float) * 3;

    public RoadDataSegmentChunk(EndianAwareBinaryReader br) : this(br.ReadP3DString(), br.ReadUInt32(), br.ReadUInt32(), br.ReadUInt32(), br.ReadVector3(), br.ReadVector3(), br.ReadVector3())
    {
    }

    public RoadDataSegmentChunk(string name, uint type, uint lanes, bool hasShoulder, Vector3 direction, Vector3 top, Vector3 bottom) : this(name, type, lanes, hasShoulder ? 1u : 0u, direction, top, bottom)
    {
    }

    public RoadDataSegmentChunk(string name, uint type, uint lanes, uint hasShoulder, Vector3 direction, Vector3 top, Vector3 bottom) : base(ChunkID, name)
    {
        _type = type;
        _lanes = lanes;
        _hasShoulder = hasShoulder;
        _direction = direction;
        _top = top;
        _bottom = bottom;
    }

    protected override void WriteData(EndianAwareBinaryWriter bw)
    {
        bw.WriteP3DString(Name);
        bw.Write(Type);
        bw.Write(Lanes);
        bw.Write(_hasShoulder);
        bw.Write(Direction);
        bw.Write(Top);
        bw.Write(Bottom);
    }

    protected override Chunk CloneSelf() => new RoadDataSegmentChunk(Name, Type, Lanes, HasShoulder, Direction, Top, Bottom);
}
