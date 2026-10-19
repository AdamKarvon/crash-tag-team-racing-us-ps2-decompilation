using NetP3DLib.IO;
using NetP3DLib.P3D.Attributes;
using NetP3DLib.P3D.Enums;
using System;
using System.Collections.Generic;

namespace NetP3DLib.P3D.Chunks;

[ChunkAttributes(ChunkID)]
public class CollisionAxisAlignedBoundingBoxChunk : Chunk
{
    public const ChunkIdentifier ChunkID = ChunkIdentifier.Collision_Axis_Aligned_Bounding_Box;

    private uint _dummy;
    public uint Dummy
    {
        get => _dummy;
        set
        {
            if (_dummy == value)
                return;
    
            _dummy = value;
            OnPropertyChanged(nameof(Dummy));
        }
    }
    

    public override byte[] DataBytes
    {
        get
        {
            var data = new List<byte>((int)DataLength);

            data.AddRange(BitConverter.GetBytes(Dummy));

            return [.. data];
        }
    }
    public override uint DataLength => sizeof(uint);

    public CollisionAxisAlignedBoundingBoxChunk(EndianAwareBinaryReader br) : this(br.ReadUInt32())
    {
    }

    public CollisionAxisAlignedBoundingBoxChunk() : this(0)
    {
    }

    public CollisionAxisAlignedBoundingBoxChunk(uint dummy) : base(ChunkID)
    {
        _dummy = dummy;
    }

    protected override void WriteData(EndianAwareBinaryWriter bw)
    {
        bw.Write(Dummy);
    }

    protected override Chunk CloneSelf() => new CollisionAxisAlignedBoundingBoxChunk();
}
