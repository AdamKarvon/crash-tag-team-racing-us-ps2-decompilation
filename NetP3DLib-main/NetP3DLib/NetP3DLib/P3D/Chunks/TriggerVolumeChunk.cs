using NetP3DLib.IO;
using NetP3DLib.P3D.Attributes;
using NetP3DLib.P3D.Enums;
using NetP3DLib.P3D.Extensions;
using System;
using System.Collections.Generic;
using System.Numerics;

namespace NetP3DLib.P3D.Chunks;

[ChunkAttributes(ChunkID)]
public class TriggerVolumeChunk : NamedChunk
{
    public const ChunkIdentifier ChunkID = ChunkIdentifier.Trigger_Volume;

    public enum Types
    {
        Sphere,
        Rectangle
    }

    private Types _type;
    public Types Type
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
    
    private Vector3 _halfExtents;
    public Vector3 HalfExtents
    {
        get => _halfExtents;
        set
        {
            if (_halfExtents == value)
                return;
    
            _halfExtents = value;
            OnPropertyChanged(nameof(HalfExtents));
        }
    }
    
    private Matrix4x4 _matrix;
    public Matrix4x4 Matrix
    {
        get => _matrix;
        set
        {
            if (_matrix == value)
                return;
    
            _matrix = value;
            OnPropertyChanged(nameof(Matrix));
        }
    }
    

    public override byte[] DataBytes
    {
        get
        {
            var data = new List<byte>((int)DataLength);

            data.AddRange(BinaryExtensions.GetP3DStringBytes(Name));
            data.AddRange(BitConverter.GetBytes((uint)Type));
            data.AddRange(BinaryExtensions.GetBytes(HalfExtents));
            data.AddRange(BinaryExtensions.GetBytes(Matrix));

            return [.. data];
        }
    }
    public override uint DataLength => BinaryExtensions.GetP3DStringLength(Name) + sizeof(uint) + sizeof(float) * 3 + sizeof(float) * 16;

    public TriggerVolumeChunk(EndianAwareBinaryReader br) : this(br.ReadP3DString(), (Types)br.ReadUInt32(), br.ReadVector3(), br.ReadMatrix4x4())
    {
    }

    public TriggerVolumeChunk(string name, Types type, Vector3 halfExtents, Matrix4x4 matrix) : base(ChunkID, name)
    {
        _type = type;
        _halfExtents = halfExtents;
        _matrix = matrix;
    }

    protected override void WriteData(EndianAwareBinaryWriter bw)
    {
        bw.WriteP3DString(Name);
        bw.Write((uint)Type);
        bw.Write(HalfExtents);
        bw.Write(Matrix);
    }

    protected override Chunk CloneSelf() => new TriggerVolumeChunk(Name, Type, HalfExtents, Matrix);
}
