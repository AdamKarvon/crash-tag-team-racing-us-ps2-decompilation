using NetP3DLib.IO;
using NetP3DLib.P3D.Attributes;
using NetP3DLib.P3D.Enums;
using NetP3DLib.P3D.Exceptions;
using NetP3DLib.P3D.Extensions;
using System.Collections.Generic;
using System.Numerics;

namespace NetP3DLib.P3D.Chunks;

[ChunkAttributes(ChunkID)]
public class CollisionOrientedBoundingBoxChunk : Chunk
{
    public const ChunkIdentifier ChunkID = ChunkIdentifier.Collision_Oriented_Bounding_Box;

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
    

    public override byte[] DataBytes
    {
        get
        {
            var data = new List<byte>((int)DataLength);

            data.AddRange(BinaryExtensions.GetBytes(HalfExtents));

            return [.. data];
        }
    }
    public override uint DataLength => sizeof(float) * 3;

    public CollisionOrientedBoundingBoxChunk(EndianAwareBinaryReader br) : this(br.ReadVector3())
    {
    }

    public CollisionOrientedBoundingBoxChunk(Vector3 halfExtents) : base(ChunkID)
    {
        _halfExtents = halfExtents;
    }

    public override IEnumerable<InvalidP3DException> ValidateChunk()
    {
        foreach (var error in base.ValidateChunk())
            yield return error;

        if (Children.Count < 4)
            yield return new InvalidP3DException(this, $"First four children must be {nameof(CollisionVectorChunk)}s.");
        else
        {
            if (Children[0].ID != (uint)ChunkIdentifier.Collision_Vector)
                yield return new InvalidP3DException(this, $"First child is not {nameof(CollisionVectorChunk)}.");

            if (Children[1].ID != (uint)ChunkIdentifier.Collision_Vector)
                yield return new InvalidP3DException(this, $"Second child is not {nameof(CollisionVectorChunk)}.");

            if (Children[2].ID != (uint)ChunkIdentifier.Collision_Vector)
                yield return new InvalidP3DException(this, $"Third child is not {nameof(CollisionVectorChunk)}.");

            if (Children[3].ID != (uint)ChunkIdentifier.Collision_Vector)
                yield return new InvalidP3DException(this, $"Fourth child is not {nameof(CollisionVectorChunk)}.");
        }
    }

    protected override void WriteData(EndianAwareBinaryWriter bw)
    {
        bw.Write(HalfExtents);
    }

    protected override Chunk CloneSelf() => new CollisionOrientedBoundingBoxChunk(HalfExtents);
}
