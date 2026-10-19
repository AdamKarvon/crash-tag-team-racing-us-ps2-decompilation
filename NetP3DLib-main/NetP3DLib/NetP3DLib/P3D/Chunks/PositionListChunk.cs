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
public class PositionListChunk : Chunk
{
    public const ChunkIdentifier ChunkID = ChunkIdentifier.Position_List;

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

            data.AddRange(BitConverter.GetBytes(NumPositions));
            foreach (var pos in Positions)
                data.AddRange(BinaryExtensions.GetBytes(pos));

            return [.. data];
        }
    }
    public override uint DataLength => sizeof(uint) + sizeof(float) * 3 * NumPositions;

    public PositionListChunk(EndianAwareBinaryReader br) : this(br.ReadVector3Array(out _))
    {
    }

    public PositionListChunk(IList<Vector3> positions) : base(ChunkID)
    {
        Positions = CreateSizeAwareList(positions, Positions_CollectionChanged);
    }
    
    private void Positions_CollectionChanged(object sender, System.Collections.Specialized.NotifyCollectionChangedEventArgs e) => OnPropertyChanged(nameof(Positions));

    public override IEnumerable<InvalidP3DException> ValidateChunk()
    {
        foreach (var error in base.ValidateChunk())
            yield return error;

        if (ParentChunk is OldPrimitiveGroupChunk oldPrimitiveGroup && oldPrimitiveGroup.NumVertices != NumPositions)
            yield return new InvalidP3DException(this, $"Num Positions value {NumPositions} does not match parent Num Vertices value {oldPrimitiveGroup.NumVertices}.");
    }

    protected override void WriteData(EndianAwareBinaryWriter bw)
    {
        bw.Write(NumPositions);
        foreach (var pos in Positions)
            bw.Write(pos);
    }

    protected override Chunk CloneSelf() => new PositionListChunk(Positions);
}
