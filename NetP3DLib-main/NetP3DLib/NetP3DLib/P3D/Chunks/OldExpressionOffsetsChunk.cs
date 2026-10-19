using NetP3DLib.IO;
using NetP3DLib.P3D.Attributes;
using NetP3DLib.P3D.Collections;
using NetP3DLib.P3D.Enums;
using NetP3DLib.P3D.Extensions;
using System;
using System.Collections.Generic;

namespace NetP3DLib.P3D.Chunks;

[ChunkAttributes(ChunkID)]
public class OldExpressionOffsetsChunk : Chunk
{
    public const ChunkIdentifier ChunkID = ChunkIdentifier.Old_Expression_Offsets;

    public uint NumPrimitiveGroups
    {
        get => (uint)(PrimitiveGroupIndices?.Count ?? 0);
        set
        {
            if (value == NumPrimitiveGroups)
                return;

            if (value < NumPrimitiveGroups)
            {
                PrimitiveGroupIndices.RemoveRange((int)value, (int)(NumPrimitiveGroups - value));
            }
            else
            {
                int count = (int)(value - NumPrimitiveGroups);
                var newPrimitiveGroupIndices = new uint[count];

                for (var i = 0; i < count; i++)
                    newPrimitiveGroupIndices[i] = default;

                PrimitiveGroupIndices.AddRange(newPrimitiveGroupIndices);
            }
        }
    }
    public uint NumOffsetLists => GetChildCount(ChunkIdentifier.Old_Offset_List);
    public SizeAwareList<uint> PrimitiveGroupIndices { get; }

    public override byte[] DataBytes
    {
        get
        {
            var data = new List<byte>((int)DataLength);

            data.AddRange(BitConverter.GetBytes(NumPrimitiveGroups));
            data.AddRange(BitConverter.GetBytes(NumOffsetLists));
            foreach (var index in PrimitiveGroupIndices)
                data.AddRange(BitConverter.GetBytes(index));

            return [.. data];
        }
    }
    public override uint DataLength => sizeof(uint) + sizeof(uint) + sizeof(uint) * NumPrimitiveGroups;

    public OldExpressionOffsetsChunk(EndianAwareBinaryReader br) : this(ReadPrimitiveGroupIndices(br))
    {
    }

    [System.Diagnostics.CodeAnalysis.SuppressMessage("Style", "IDE0059:Unnecessary assignment of a value", Justification = "We want to read the value to progress the BinaryReader, but not set the value anywhere because it's calculated dynamically.")]
    private static uint[] ReadPrimitiveGroupIndices(EndianAwareBinaryReader br)
    {
        var numPrimitiveGroups = br.ReadInt32();
        var numOffsetLists = br.ReadUInt32();
        return br.ReadUInt32Array(numPrimitiveGroups);
    }

    public OldExpressionOffsetsChunk(IList<uint> primitiveGroupIndices) : base(ChunkID)
    {
        PrimitiveGroupIndices = CreateSizeAwareList(primitiveGroupIndices, PrimitiveGroupIndices_CollectionChanged);
    }
    
    private void PrimitiveGroupIndices_CollectionChanged(object sender, System.Collections.Specialized.NotifyCollectionChangedEventArgs e) => OnPropertyChanged(nameof(PrimitiveGroupIndices));

    protected override void WriteData(EndianAwareBinaryWriter bw)
    {
        bw.Write(NumPrimitiveGroups);
        bw.Write(NumOffsetLists);
        foreach (var index in PrimitiveGroupIndices)
            bw.Write(index);
    }

    protected override Chunk CloneSelf() => new OldExpressionOffsetsChunk(PrimitiveGroupIndices);
}
