using NetP3DLib.IO;
using NetP3DLib.P3D.Attributes;
using NetP3DLib.P3D.Enums;
using System;
using System.Collections.Generic;

namespace NetP3DLib.P3D.Chunks;

[ChunkAttributes(ChunkID)]
public class CompositeDrawableEffectListChunk : Chunk
{
    public const ChunkIdentifier ChunkID = ChunkIdentifier.Composite_Drawable_Effect_List;

    public uint NumElements => GetChildCount(ChunkIdentifier.Composite_Drawable_Effect);

    public override byte[] DataBytes
    {
        get
        {
            var data = new List<byte>((int)DataLength);

            data.AddRange(BitConverter.GetBytes(NumElements));

            return [.. data];
        }
    }
    public override uint DataLength => sizeof(uint);

    [System.Diagnostics.CodeAnalysis.SuppressMessage("Style", "IDE0059:Unnecessary assignment of a value", Justification = "We want to read the value to progress the BinaryReader, but not set the value anywhere because it's calculated dynamically.")]
    public CompositeDrawableEffectListChunk(EndianAwareBinaryReader br) : this()
    {
        var numElements = br.ReadUInt32();
    }

    public CompositeDrawableEffectListChunk() : base(ChunkID)
    { }

    protected override void WriteData(EndianAwareBinaryWriter bw)
    {
        bw.Write(NumElements);
    }

    protected override Chunk CloneSelf() => new CompositeDrawableEffectListChunk();
}