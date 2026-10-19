using NetP3DLib.IO;
using NetP3DLib.P3D.Attributes;
using NetP3DLib.P3D.Enums;
using System;
using System.Collections.Generic;

namespace NetP3DLib.P3D.Chunks;

[ChunkAttributes(ChunkID)]
public class IntersectMesh2Chunk : Chunk
{
    public const ChunkIdentifier ChunkID = ChunkIdentifier.Intersect_Mesh_2;

    private uint _surfaceType;
    public uint SurfaceType
    {
        get => _surfaceType;
        set
        {
            if (_surfaceType == value)
                return;
    
            _surfaceType = value;
            OnPropertyChanged(nameof(SurfaceType));
        }
    }
    

    public override byte[] DataBytes
    {
        get
        {
            var data = new List<byte>((int)DataLength);

            data.AddRange(BitConverter.GetBytes(SurfaceType));

            return [.. data];
        }
    }
    public override uint DataLength => sizeof(uint);

    public IntersectMesh2Chunk(EndianAwareBinaryReader br) : this(br.ReadUInt32())
    {
    }

    public IntersectMesh2Chunk(uint surfaceType) : base(ChunkID)
    {
        _surfaceType = surfaceType;
    }

    protected override void WriteData(EndianAwareBinaryWriter bw)
    {
        bw.Write(SurfaceType);
    }

    protected override Chunk CloneSelf() => new IntersectMesh2Chunk(SurfaceType);
}
