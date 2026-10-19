using NetP3DLib.IO;
using NetP3DLib.Numerics;
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
public class WeightListChunk : Chunk
{
    public const ChunkIdentifier ChunkID = ChunkIdentifier.Weight_List;

    public uint NumWeights
    {
        get => (uint)(Weights?.Count ?? 0);
        set
        {
            if (value == NumWeights)
                return;

            if (value < NumWeights)
            {
                Weights.RemoveRange((int)value, (int)(NumWeights - value));
            }
            else
            {
                int count = (int)(value - NumWeights);
                var newWeights = new Vector3[count];

                for (var i = 0; i < count; i++)
                    newWeights[i] = default;

                Weights.AddRange(newWeights);
            }
        }
    }
    public SizeAwareList<Vector3> Weights { get; }

    public override byte[] DataBytes
    {
        get
        {
            var data = new List<byte>((int)DataLength);

            data.AddRange(BitConverter.GetBytes(NumWeights));
            foreach (var weight in Weights)
                data.AddRange(BinaryExtensions.GetBytes(weight));

            return [.. data];
        }
    }
    public override uint DataLength => sizeof(uint) + sizeof(float) * 3 * NumWeights;

    public WeightListChunk(EndianAwareBinaryReader br) : this(br.ReadVector3Array(out _))
    {
    }

    public WeightListChunk(IList<Vector3> weights) : base(ChunkID)
    {
        Weights = CreateSizeAwareList(weights, Weights_CollectionChanged);
    }
    
    private void Weights_CollectionChanged(object sender, System.Collections.Specialized.NotifyCollectionChangedEventArgs e) => OnPropertyChanged(nameof(Weights));

    public override IEnumerable<InvalidP3DException> ValidateChunk()
    {
        foreach (var error in base.ValidateChunk())
            yield return error;

        if (ParentChunk is OldPrimitiveGroupChunk oldPrimitiveGroup && oldPrimitiveGroup.NumVertices != NumWeights)
            yield return new InvalidP3DException(this, $"Num Weights value {NumWeights} does not match parent Num Vertices value {oldPrimitiveGroup.NumVertices}.");

        for (var i = 0; i < Weights.Count; i++)
        {
            var weight = Weights[i];
            var sum = weight.X + weight.Y + weight.Z;

            if (MathUtil.IsZero(sum))
                yield return new InvalidP3DException(this, $"Weight {i} is all zeros.");
            //else if (!MathUtil.IsOne(sum))
            //    yield return new InvalidP3DException(this, $"Weight {i} does not have a sum of 1 (sum={sum}).");
        }
    }

    protected override void WriteData(EndianAwareBinaryWriter bw)
    {
        bw.Write(NumWeights);
        foreach (var weight in Weights)
            bw.Write(weight);
    }

    protected override Chunk CloneSelf() => new WeightListChunk(Weights);
}
