using NetP3DLib.IO;
using NetP3DLib.P3D.Attributes;
using NetP3DLib.P3D.Collections;
using NetP3DLib.P3D.Enums;
using NetP3DLib.P3D.Exceptions;
using NetP3DLib.P3D.Extensions;
using System;
using System.Collections.Generic;
using System.Numerics;

namespace NetP3DLib.P3D.Chunks;

[ChunkAttributes(ChunkID)]
public class BinormalListChunk : Chunk
{
    public const ChunkIdentifier ChunkID = ChunkIdentifier.Binormal_List;

    public uint NumBinormals
    {
        get => (uint)(Binormals?.Count ?? 0);
        set
        {
            if (value == NumBinormals)
                return;

            if (value < NumBinormals)
            {
                Binormals.RemoveRange((int)value, (int)(NumBinormals - value));
            }
            else
            {
                int count = (int)(value - NumBinormals);
                var newBinormals = new Vector3[count];

                for (var i = 0; i < count; i++)
                    newBinormals[i] = default;

                Binormals.AddRange(newBinormals);
            }
        }
    }
    public SizeAwareList<Vector3> Binormals { get; }

    public override byte[] DataBytes
    {
        get
        {
            var data = new List<byte>((int)DataLength);

            data.AddRange(BitConverter.GetBytes(NumBinormals));
            foreach (var normal in Binormals)
                data.AddRange(BinaryExtensions.GetBytes(normal));

            return [.. data];
        }
    }
    public override uint DataLength => sizeof(uint) + sizeof(float) * 3 * NumBinormals;

    public BinormalListChunk(EndianAwareBinaryReader br) : this(br.ReadVector3Array(out _))
    {
    }

    public BinormalListChunk(IList<Vector3> binormals) : base(ChunkID)
    {
        Binormals = CreateSizeAwareList(binormals, Binormals_CollectionChanged);
    }
    
    private void Binormals_CollectionChanged(object sender, System.Collections.Specialized.NotifyCollectionChangedEventArgs e) => OnPropertyChanged(nameof(Binormals));

    public override IEnumerable<InvalidP3DException> ValidateChunk()
    {
        foreach (var error in base.ValidateChunk())
            yield return error;

        if (ParentChunk is OldPrimitiveGroupChunk oldPrimitiveGroup && oldPrimitiveGroup.NumVertices != NumBinormals)
            yield return new InvalidP3DException(this, $"Num Binormals value {NumBinormals} does not match parent Num Vertices value {oldPrimitiveGroup.NumVertices}.");
    }

    protected override void WriteData(EndianAwareBinaryWriter bw)
    {
        bw.Write(NumBinormals);
        foreach (var pos in Binormals)
            bw.Write(pos);
    }

    protected override Chunk CloneSelf() => new NormalListChunk(Binormals);
}
