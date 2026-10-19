using NetP3DLib.IO;
using NetP3DLib.P3D.Attributes;
using NetP3DLib.P3D.Enums;
using NetP3DLib.P3D.Extensions;
using System;
using System.Collections.Generic;
using System.Numerics;

namespace NetP3DLib.P3D.Chunks;

[ChunkAttributes(ChunkID)]
public class BillboardTransformChunk : Chunk
{
    public const ChunkIdentifier ChunkID = ChunkIdentifier.Billboard_Transform;

    private uint _version;
    public uint Version
    {
        get => _version;
        set
        {
            if (_version == value)
                return;
    
            _version = value;
            OnPropertyChanged(nameof(Version));
        }
    }
    
    private Quaternion _rotation;
    public Quaternion Rotation
    {
        get => _rotation;
        set
        {
            if (_rotation == value)
                return;
    
            _rotation = value;
            OnPropertyChanged(nameof(Rotation));
        }
    }
    
    private Vector3 _translation;
    public Vector3 Translation
    {
        get => _translation;
        set
        {
            if (_translation == value)
                return;
    
            _translation = value;
            OnPropertyChanged(nameof(Translation));
        }
    }
    

    public override byte[] DataBytes
    {
        get
        {
            var data = new List<byte>((int)DataLength);

            data.AddRange(BitConverter.GetBytes(Version));
            data.AddRange(BinaryExtensions.GetBytes(Rotation));
            data.AddRange(BinaryExtensions.GetBytes(Translation));

            return [.. data];
        }
    }
    public override uint DataLength => sizeof(uint) + sizeof(float) * 4 + sizeof(float) * 3;

    public BillboardTransformChunk(EndianAwareBinaryReader br) : this(br.ReadUInt32(), br.ReadQuaternion(), br.ReadVector3())
    {
    }

    public BillboardTransformChunk(uint version, Quaternion rotation, Vector3 translation) : base(ChunkID)
    {
        _version = version;
        _rotation = rotation;
        _translation = translation;
    }

    protected override void WriteData(EndianAwareBinaryWriter bw)
    {
        bw.Write(Version);
        bw.Write(Rotation);
        bw.Write(Translation);
    }

    protected override Chunk CloneSelf() => new BillboardTransformChunk(Version, Rotation, Translation);
}
