using NetP3DLib.IO;
using NetP3DLib.P3D.Attributes;
using NetP3DLib.P3D.Enums;
using NetP3DLib.P3D.Exceptions;
using System;
using System.Collections.Generic;

namespace NetP3DLib.P3D.Chunks;

[ChunkAttributes(ChunkID)]
public class CollisionVolumeChunk : Chunk
{
    public const ChunkIdentifier ChunkID = ChunkIdentifier.Collision_Volume;
    private static readonly HashSet<ChunkIdentifier> AllowedCollisionTypes =
    [
        ChunkIdentifier.Collision_Sphere,
        ChunkIdentifier.Collision_Cylinder,
        ChunkIdentifier.Collision_Oriented_Bounding_Box,
        ChunkIdentifier.Collision_Wall,
        ChunkIdentifier.Collision_Axis_Aligned_Bounding_Box,
    ];

    private uint _objectReferenceIndex;
    public uint ObjectReferenceIndex
    {
        get => _objectReferenceIndex;
        set
        {
            if (_objectReferenceIndex == value)
                return;
    
            _objectReferenceIndex = value;
            OnPropertyChanged(nameof(ObjectReferenceIndex));
        }
    }
    
    private int _ownerIndex;
    public int OwnerIndex
    {
        get => _ownerIndex;
        set
        {
            if (_ownerIndex == value)
                return;
    
            _ownerIndex = value;
            OnPropertyChanged(nameof(OwnerIndex));
        }
    }
    
    public uint NumSubVolumes => GetChildCount(ChunkIdentifier.Collision_Volume);

    public override byte[] DataBytes
    {
        get
        {
            var data = new List<byte>((int)DataLength);

            data.AddRange(BitConverter.GetBytes(ObjectReferenceIndex));
            data.AddRange(BitConverter.GetBytes(OwnerIndex));
            data.AddRange(BitConverter.GetBytes(NumSubVolumes));

            return [.. data];
        }
    }
    public override uint DataLength => sizeof(uint) + sizeof(int) + sizeof(uint);

    [System.Diagnostics.CodeAnalysis.SuppressMessage("Style", "IDE0059:Unnecessary assignment of a value", Justification = "We want to read the value to progress the BinaryReader, but not set the value anywhere because it's calculated dynamically.")]
    public CollisionVolumeChunk(EndianAwareBinaryReader br) : this(br.ReadUInt32(), br.ReadInt32())
    {
        var numSubVolumes = br.ReadUInt32();
    }

    public CollisionVolumeChunk(uint objectReferenceIndex, int ownerIndex) : base(ChunkID)
    {
        _objectReferenceIndex = objectReferenceIndex;
        _ownerIndex = ownerIndex;
    }

    public override IEnumerable<InvalidP3DException> ValidateChunk()
    {
        foreach (var error in base.ValidateChunk())
            yield return error;

        if (Children.Count == 0 || !AllowedCollisionTypes.Contains((ChunkIdentifier)Children[0].ID))
            yield return new InvalidP3DException(this, $"First child must be one of: {string.Join(", ", AllowedCollisionTypes)}.");

        if (Children.Count - 1 != NumSubVolumes)
            yield return new InvalidP3DException(this, $"Remaining children must be {nameof(CollisionVolumeChunk)}");
    }

    protected override void WriteData(EndianAwareBinaryWriter bw)
    {
        bw.Write(ObjectReferenceIndex);
        bw.Write(OwnerIndex);
        bw.Write(NumSubVolumes);
    }

    protected override Chunk CloneSelf() => new CollisionVolumeChunk(ObjectReferenceIndex, OwnerIndex);
}
