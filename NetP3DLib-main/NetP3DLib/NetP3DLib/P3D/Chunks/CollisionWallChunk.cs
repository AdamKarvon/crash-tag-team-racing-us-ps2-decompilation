using NetP3DLib.IO;
using NetP3DLib.P3D.Attributes;
using NetP3DLib.P3D.Enums;
using NetP3DLib.P3D.Exceptions;
using System.Collections.Generic;

namespace NetP3DLib.P3D.Chunks;

[ChunkAttributes(ChunkID)]
public class CollisionWallChunk : Chunk
{
    public const ChunkIdentifier ChunkID = ChunkIdentifier.Collision_Wall;

    public override byte[] DataBytes
    {
        get
        {
            return [];
        }
    }
    public override uint DataLength => 0;

    [System.Diagnostics.CodeAnalysis.SuppressMessage("Style", "IDE0060:Remove unused parameter", Justification = "Required for ChunkLoader.")]
    public CollisionWallChunk(EndianAwareBinaryReader br) : this()
    { }

    public CollisionWallChunk() : base(ChunkID)
    { }

    public override IEnumerable<InvalidP3DException> ValidateChunk()
    {
        foreach (var error in base.ValidateChunk())
            yield return error;

        if (Children.Count < 2)
            yield return new InvalidP3DException(this, $"First two children must be {nameof(CollisionVectorChunk)}s.");
        else
        {
            if (Children[0].ID != (uint)ChunkIdentifier.Collision_Vector)
                yield return new InvalidP3DException(this, $"First child is not {nameof(CollisionVectorChunk)}.");

            if (Children[1].ID != (uint)ChunkIdentifier.Collision_Vector)
                yield return new InvalidP3DException(this, $"Second child is not {nameof(CollisionVectorChunk)}.");
        }
    }

    protected override void WriteData(EndianAwareBinaryWriter bw)
    { }

    protected override Chunk CloneSelf() => new CollisionWallChunk();
}