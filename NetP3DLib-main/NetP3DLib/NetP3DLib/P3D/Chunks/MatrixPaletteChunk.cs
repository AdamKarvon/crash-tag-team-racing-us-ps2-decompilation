using NetP3DLib.IO;
using NetP3DLib.P3D.Attributes;
using NetP3DLib.P3D.Collections;
using NetP3DLib.P3D.Enums;
using NetP3DLib.P3D.Extensions;
using System;
using System.Collections.Generic;

namespace NetP3DLib.P3D.Chunks;

[ChunkAttributes(ChunkID)]
public class MatrixPaletteChunk : Chunk
{
    public const ChunkIdentifier ChunkID = ChunkIdentifier.Matrix_Palette;

    public uint NumMatrices
    {
        get => (uint)(Matrices?.Count ?? 0);
        set
        {
            if (value == NumMatrices)
                return;

            if (value < NumMatrices)
            {
                Matrices.RemoveRange((int)value, (int)(NumMatrices - value));
            }
            else
            {
                int count = (int)(value - NumMatrices);
                var newMatrices = new uint[count];

                for (var i = 0; i < count; i++)
                    newMatrices[i] = default;

                Matrices.AddRange(newMatrices);
            }
        }
    }
    public SizeAwareList<uint> Matrices { get; }

    public override byte[] DataBytes
    {
        get
        {
            var data = new List<byte>((int)DataLength);

            data.AddRange(BitConverter.GetBytes(NumMatrices));
            foreach (var mat in Matrices)
                data.AddRange(BitConverter.GetBytes(mat));

            return [.. data];
        }
    }
    public override uint DataLength => sizeof(uint) + sizeof(uint) * NumMatrices;

    public MatrixPaletteChunk(EndianAwareBinaryReader br) : this(br.ReadUInt32Array(out _))
    {
    }

    public MatrixPaletteChunk(IList<uint> matrices) : base(ChunkID)
    {
        Matrices = CreateSizeAwareList(matrices, Matrices_CollectionChanged);
    }
    
    private void Matrices_CollectionChanged(object sender, System.Collections.Specialized.NotifyCollectionChangedEventArgs e) => OnPropertyChanged(nameof(Matrices));

    protected override void WriteData(EndianAwareBinaryWriter bw)
    {
        bw.Write(NumMatrices);
        foreach (var mat in Matrices)
            bw.Write(mat);
    }

    protected override Chunk CloneSelf() => new MatrixPaletteChunk(Matrices);
}
