using NetP3DLib.IO;
using NetP3DLib.P3D.Attributes;
using NetP3DLib.P3D.Enums;
using NetP3DLib.P3D.Exceptions;
using System;
using System.Collections.Generic;

namespace NetP3DLib.P3D.Chunks;

[ChunkAttributes(ChunkID)]
public class CollisionSphereChunk : Chunk
{
    public const ChunkIdentifier ChunkID = ChunkIdentifier.Collision_Sphere;

    private float _radius;
    public float Radius
    {
        get => _radius;
        set
        {
            if (_radius == value)
                return;
    
            _radius = value;
            OnPropertyChanged(nameof(Radius));
        }
    }
    

    public override byte[] DataBytes
    {
        get
        {
            var data = new List<byte>((int)DataLength);

            data.AddRange(BitConverter.GetBytes(Radius));

            return [.. data];
        }
    }
    public override uint DataLength => sizeof(float);

    public CollisionSphereChunk(EndianAwareBinaryReader br) : this(br.ReadSingle())
    {
    }

    public CollisionSphereChunk(float radius) : base(ChunkID)
    {
        _radius = radius;
    }

    public override IEnumerable<InvalidP3DException> ValidateChunk()
    {
        foreach (var error in base.ValidateChunk())
            yield return error;

        if (Children.Count < 1)
            yield return new InvalidP3DException(this, $"First child must be {nameof(CollisionVectorChunk)}.");
        else if (Children[0].ID != (uint)ChunkIdentifier.Collision_Vector)
            yield return new InvalidP3DException(this, $"First child is not {nameof(CollisionVectorChunk)}.");
    }

    protected override void WriteData(EndianAwareBinaryWriter bw)
    {
        bw.Write(Radius);
    }

    protected override Chunk CloneSelf() => new CollisionSphereChunk(Radius);
}
