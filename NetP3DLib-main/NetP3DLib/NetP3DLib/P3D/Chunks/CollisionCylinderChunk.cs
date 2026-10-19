using NetP3DLib.IO;
using NetP3DLib.P3D.Attributes;
using NetP3DLib.P3D.Enums;
using NetP3DLib.P3D.Exceptions;
using System;
using System.Collections.Generic;

namespace NetP3DLib.P3D.Chunks;

[ChunkAttributes(ChunkID)]
public class CollisionCylinderChunk : Chunk
{
    public const ChunkIdentifier ChunkID = ChunkIdentifier.Collision_Cylinder;

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
    
    private float _halfLength;
    public float HalfLength
    {
        get => _halfLength;
        set
        {
            if (_halfLength == value)
                return;
    
            _halfLength = value;
            OnPropertyChanged(nameof(HalfLength));
        }
    }
    
    private ushort _flatEnd;
    public bool FlatEnd
    {
        get => _flatEnd != 0;
        set
        {
            if (FlatEnd == value)
                return;

            _flatEnd = (ushort)(value ? 1 : 0);
            OnPropertyChanged(nameof(FlatEnd));
        }
    }

    public override byte[] DataBytes
    {
        get
        {
            var data = new List<byte>((int)DataLength);

            data.AddRange(BitConverter.GetBytes(Radius));
            data.AddRange(BitConverter.GetBytes(HalfLength));
            data.AddRange(BitConverter.GetBytes(_flatEnd));

            return [.. data];
        }
    }
    public override uint DataLength => sizeof(float) + sizeof(float) + sizeof(ushort);

    public CollisionCylinderChunk(EndianAwareBinaryReader br) : this(br.ReadSingle(), br.ReadSingle(), br.ReadUInt16())
    {
    }

    public CollisionCylinderChunk(float radius, float halfLength, bool flatEnd) : this(radius, halfLength, (ushort)(flatEnd ? 1u : 0u))
    {
    }

    public CollisionCylinderChunk(float radius, float halfLength, ushort flatEnd) : base(ChunkID)
    {
        _radius = radius;
        _halfLength = halfLength;
        _flatEnd = flatEnd;
    }

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
    {
        bw.Write(Radius);
        bw.Write(HalfLength);
        bw.Write(_flatEnd);
    }

    protected override Chunk CloneSelf() => new CollisionCylinderChunk(Radius, HalfLength, FlatEnd);
}
