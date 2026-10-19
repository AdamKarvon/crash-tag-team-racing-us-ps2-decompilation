using NetP3DLib.IO;
using NetP3DLib.P3D.Attributes;
using NetP3DLib.P3D.Enums;
using System;
using System.Collections.Generic;

namespace NetP3DLib.P3D.Chunks;

[ChunkAttributes(ChunkID)]
public class CollisionVolumeOwnerChunk : Chunk
{
    public const ChunkIdentifier ChunkID = ChunkIdentifier.Collision_Volume_Owner;

    public uint NumNames => GetChildCount(ChunkIdentifier.Collision_Volume_Owner_Name);

    public override byte[] DataBytes
    {
        get
        {
            var data = new List<byte>((int)DataLength);

            data.AddRange(BitConverter.GetBytes(NumNames));

            return [.. data];
        }
    }
    public override uint DataLength => sizeof(uint);

    [System.Diagnostics.CodeAnalysis.SuppressMessage("Style", "IDE0059:Unnecessary assignment of a value", Justification = "We want to read the value to progress the BinaryReader, but not set the value anywhere because it's calculated dynamically.")]
    public CollisionVolumeOwnerChunk(EndianAwareBinaryReader br) : this()
    {
        var numNames = br.ReadUInt32();
    }

    public CollisionVolumeOwnerChunk() : base(ChunkID)
    { }

    protected override void WriteData(EndianAwareBinaryWriter bw)
    {
        bw.Write(NumNames);
    }

    protected override Chunk CloneSelf() => new CollisionVolumeOwnerChunk();
}