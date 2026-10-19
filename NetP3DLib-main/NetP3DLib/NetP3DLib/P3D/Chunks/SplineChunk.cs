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
public class SplineChunk : NamedChunk
{
    public const ChunkIdentifier ChunkID = ChunkIdentifier.Spline;

    public uint NumPositions
    {
        get => (uint)(Positions?.Count ?? 0);
        set
        {
            if (value == NumPositions)
                return;

            if (value < NumPositions)
            {
                Positions.RemoveRange((int)value, (int)(NumPositions - value));
            }
            else
            {
                int count = (int)(value - NumPositions);
                var newPositions = new Vector3[count];

                for (var i = 0; i < count; i++)
                    newPositions[i] = default;

                Positions.AddRange(newPositions);
            }
        }
    }
    public SizeAwareList<Vector3> Positions { get; }

    public override byte[] DataBytes
    {
        get
        {
            var data = new List<byte>((int)DataLength);

            data.AddRange(BinaryExtensions.GetP3DStringBytes(Name));
            data.AddRange(BitConverter.GetBytes(NumPositions));
            foreach (var pos in Positions)
                data.AddRange(BinaryExtensions.GetBytes(pos));

            return [.. data];
        }
    }
    public override uint DataLength => BinaryExtensions.GetP3DStringLength(Name) + sizeof(uint) + sizeof(float) * 3 * NumPositions;

    public SplineChunk(EndianAwareBinaryReader br) : this(br.ReadP3DString(), br.ReadVector3Array(out _))
    {
    }

    public SplineChunk(string name, IList<Vector3> positions) : base(ChunkID, name)
    {
        Positions = CreateSizeAwareList(positions, Positions_CollectionChanged);
    }
    
    private void Positions_CollectionChanged(object sender, System.Collections.Specialized.NotifyCollectionChangedEventArgs e) => OnPropertyChanged(nameof(Positions));

    public override IEnumerable<InvalidP3DException> ValidateChunk()
    {
        foreach (var error in base.ValidateChunk())
            yield return error;

        if (Children.Count != 1 || Children[0].ID != (uint)ChunkIdentifier.Rail_Cam)
            yield return new InvalidP3DException(this, $"Children must be one Rail Cam child chunk.");
    }

    protected override void WriteData(EndianAwareBinaryWriter bw)
    {
        bw.WriteP3DString(Name);
        bw.Write(NumPositions);
        foreach (var pos in Positions)
            bw.Write(pos);
    }

    protected override Chunk CloneSelf() => new SplineChunk(Name, Positions);
}
