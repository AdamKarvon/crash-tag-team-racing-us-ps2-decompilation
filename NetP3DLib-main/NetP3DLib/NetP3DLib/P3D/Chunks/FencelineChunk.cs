using NetP3DLib.IO;
using NetP3DLib.P3D.Attributes;
using NetP3DLib.P3D.Enums;
using System;
using System.Collections.Generic;

namespace NetP3DLib.P3D.Chunks;

[ChunkAttributes(ChunkID)]
public class FencelineChunk : Chunk
{
    public const ChunkIdentifier ChunkID = ChunkIdentifier.Fenceline;

    public uint NumWalls => GetChildCount(ChunkIdentifier.Wall);

    public override byte[] DataBytes
    {
        get
        {
            var data = new List<byte>((int)DataLength);

            data.AddRange(BitConverter.GetBytes(NumWalls));

            return [.. data];
        }
    }
    public override uint DataLength => sizeof(uint);

    [System.Diagnostics.CodeAnalysis.SuppressMessage("Style", "IDE0059:Unnecessary assignment of a value", Justification = "We want to read the value to progress the BinaryReader, but not set the value anywhere because it's calculated dynamically.")]
    public FencelineChunk(EndianAwareBinaryReader br) : this()
    {
        var numWalls = br.ReadUInt32();
    }

    public FencelineChunk() : base(ChunkID)
    { }

    protected override void WriteData(EndianAwareBinaryWriter bw)
    {
        bw.Write(NumWalls);
    }

    protected override Chunk CloneSelf() => new FencelineChunk();
}