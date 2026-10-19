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
public class CollisionMetaDataVectorChannelChunk : NamedChunk
{
    public const ChunkIdentifier ChunkID = ChunkIdentifier.Collision_Meta_Data_Vector_Channel;

    private uint _version;
    public uint Version
    {
        get => _version;
        set
        {
            if (_version == value)
                return;
    
            _version = value;
            OnPropertyChanged(nameof(Version));
        }
    }

    public uint NumIndices
    {
        get => (uint)(Indices?.Count ?? 0);
        set
        {
            if (value == NumIndices)
                return;

            if (value < NumIndices)
            {
                Indices.RemoveRange((int)value, (int)(NumIndices - value));
            }
            else
            {
                int count = (int)(value - NumIndices);
                var newIndices = new ushort[count];

                for (var i = 0; i < count; i++)
                    newIndices[i] = default;

                Indices.AddRange(newIndices);
            }
            NumValues = value;
        }
    }
    public SizeAwareList<ushort> Indices { get; }
    public uint NumValues
    {
        get => (uint)(Values?.Count ?? 0);
        set
        {
            if (value == NumValues)
                return;

            if (value < NumValues)
            {
                Values.RemoveRange((int)value, (int)(NumValues - value));
            }
            else
            {
                int count = (int)(value - NumValues);
                var newValues = new Vector3[count];

                for (var i = 0; i < count; i++)
                    newValues[i] = default;

                Values.AddRange(newValues);
            }
            NumIndices = value;
        }
    }
    public SizeAwareList<Vector3> Values { get; }

    public override byte[] DataBytes
    {
        get
        {
            var data = new List<byte>((int)DataLength);

            data.AddRange(BitConverter.GetBytes(Version));
            data.AddRange(BinaryExtensions.GetP3DStringBytes(Name));
            data.AddRange(BitConverter.GetBytes(NumIndices));
            foreach (var frame in Indices)
                data.AddRange(BitConverter.GetBytes(frame));
            foreach (var value in Values)
                data.AddRange(BinaryExtensions.GetBytes(value));

            return [.. data];
        }
    }
    public override uint DataLength => sizeof(uint) + BinaryExtensions.GetP3DStringLength(Name) + sizeof(uint) + sizeof(ushort) * NumIndices + sizeof(float) * 3 * NumValues;

    public CollisionMetaDataVectorChannelChunk(EndianAwareBinaryReader br) : this(br.ReadUInt32(), br.ReadP3DString(), br.ReadUInt16Array(out var numIndices), br.ReadVector3Array(numIndices))
    {
    }

    public CollisionMetaDataVectorChannelChunk(uint version, string name, IList<ushort> indices, IList<Vector3> values) : base(ChunkID, name)
    {
        _version = version;
        Indices = CreateSizeAwareList(indices, Indices_CollectionChanged);
        Values = CreateSizeAwareList(values, Values_CollectionChanged);
    }
    
    private void Indices_CollectionChanged(object sender, System.Collections.Specialized.NotifyCollectionChangedEventArgs e) => OnPropertyChanged(nameof(Indices));
    
    private void Values_CollectionChanged(object sender, System.Collections.Specialized.NotifyCollectionChangedEventArgs e) => OnPropertyChanged(nameof(Values));

    public override IEnumerable<InvalidP3DException> ValidateChunk()
    {
        foreach (var error in base.ValidateChunk())
            yield return error;

        if (Indices.Count != Values.Count)
            yield return new InvalidP3DException(this, $"{nameof(Indices)} and {nameof(Values)} must have equal counts.");
    }

    protected override void WriteData(EndianAwareBinaryWriter bw)
    {
        bw.Write(Version);
        bw.WriteP3DString(Name);
        bw.Write(NumIndices);
        foreach (var index in Indices)
            bw.Write(index);
        foreach (var value in Values)
            bw.Write(value);
    }

    protected override Chunk CloneSelf() => new CollisionMetaDataVectorChannelChunk(Version, Name, Indices, Values);
}
