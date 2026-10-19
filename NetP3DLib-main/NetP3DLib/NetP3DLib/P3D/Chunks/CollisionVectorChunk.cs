using NetP3DLib.IO;
using NetP3DLib.P3D.Attributes;
using NetP3DLib.P3D.Enums;
using NetP3DLib.P3D.Extensions;
using System.Collections.Generic;
using System.Numerics;

namespace NetP3DLib.P3D.Chunks;

[ChunkAttributes(ChunkID)]
public class CollisionVectorChunk : Chunk
{
    public const ChunkIdentifier ChunkID = ChunkIdentifier.Collision_Vector;

    private Vector3 _vector;
    public Vector3 Vector
    {
        get => _vector;
        set
        {
            if (_vector == value)
                return;
    
            _vector = value;
            OnPropertyChanged(nameof(Vector));
        }
    }
    

    public override byte[] DataBytes
    {
        get
        {
            var data = new List<byte>((int)DataLength);

            data.AddRange(BinaryExtensions.GetBytes(Vector));

            return [.. data];
        }
    }
    public override uint DataLength => sizeof(float) * 3;

    public CollisionVectorChunk(EndianAwareBinaryReader br) : this(br.ReadVector3())
    {
    }

    public CollisionVectorChunk(Vector3 vector) : base(ChunkID)
    {
        _vector = vector;
    }

    protected override void WriteData(EndianAwareBinaryWriter bw)
    {
        bw.Write(Vector);
    }

    protected override Chunk CloneSelf() => new CollisionVectorChunk(Vector);

    public override string ToString()
    {
        return ParentChunk switch
        {
            CollisionSphereChunk => IndexInParent switch
            {
                0 => $"Centre ({GetChunkType(this)} (0x{ID:X}))",
                _ => base.ToString()
            },
            CollisionCylinderChunk => IndexInParent switch
            {
                0 => $"Centre ({GetChunkType(this)} (0x{ID:X}))",
                1 => $"Orientation ({GetChunkType(this)} (0x{ID:X}))",
                _ => base.ToString()
            },
            CollisionOrientedBoundingBoxChunk => IndexInParent switch
            {
                0 => $"Centre ({GetChunkType(this)} (0x{ID:X}))",
                1 => $"Rotation Matrix X ({GetChunkType(this)} (0x{ID:X}))",
                2 => $"Rotation Matrix Y ({GetChunkType(this)} (0x{ID:X}))",
                3 => $"Rotation Matrix Z ({GetChunkType(this)} (0x{ID:X}))",
                _ => base.ToString()
            },
            CollisionWallChunk => IndexInParent switch
            {
                0 => $"Position ({GetChunkType(this)} (0x{ID:X}))",
                1 => $"Normal Ref ({GetChunkType(this)} (0x{ID:X}))",
                _ => base.ToString()
            },
            _ => base.ToString(),
        };
    }
}
