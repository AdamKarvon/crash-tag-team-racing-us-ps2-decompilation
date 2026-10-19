using NetP3DLib.IO;
using NetP3DLib.P3D.Attributes;
using NetP3DLib.P3D.Collections;
using NetP3DLib.P3D.Enums;
using NetP3DLib.P3D.Exceptions;
using NetP3DLib.P3D.Extensions;
using System;
using System.Collections.Generic;
using System.ComponentModel;

namespace NetP3DLib.P3D.Chunks;

[ChunkAttributes(ChunkID)]
public class ExpressionChunk : NamedChunk
{
    public const ChunkIdentifier ChunkID = ChunkIdentifier.Expression;

    private uint _version;
    [DefaultValue(0)]
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

    public uint NumKeys
    {
        get => (uint)(Keys?.Count ?? 0);
        set
        {
            if (value == NumKeys)
                return;

            if (value < NumKeys)
            {
                Keys.RemoveRange((int)value, (int)(NumKeys - value));
            }
            else
            {
                int count = (int)(value - NumKeys);
                var newKeys = new float[count];

                for (var i = 0; i < count; i++)
                    newKeys[i] = default;

                Keys.AddRange(newKeys);
            }
            NumIndices = value;
        }
    }
    public SizeAwareList<float> Keys { get; }
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
                var newIndices = new uint[count];

                for (var i = 0; i < count; i++)
                    newIndices[i] = default;

                Indices.AddRange(newIndices);
            }
            NumKeys = value;
        }
    }
    public SizeAwareList<uint> Indices { get; }

    public override byte[] DataBytes
    {
        get
        {
            var data = new List<byte>((int)DataLength);

            data.AddRange(BitConverter.GetBytes(Version));
            data.AddRange(BinaryExtensions.GetP3DStringBytes(Name));
            data.AddRange(BitConverter.GetBytes(NumKeys));
            foreach (var key in Keys)
                data.AddRange(BitConverter.GetBytes(key));
            foreach (var index in Indices)
                data.AddRange(BitConverter.GetBytes(index));

            return [.. data];
        }
    }
    public override uint DataLength => sizeof(uint) + BinaryExtensions.GetP3DStringLength(Name) + sizeof(uint) + sizeof(float) * NumKeys + sizeof(uint) * NumIndices;

    public ExpressionChunk(EndianAwareBinaryReader br) : this(br.ReadUInt32(), br.ReadP3DString(), br.ReadSingleArray(out var numKeys), br.ReadUInt32Array(numKeys))
    {
    }

    public ExpressionChunk(uint version, string name, IList<float> keys, IList<uint> indices) : base(ChunkID, name)
    {
        _version = version;
        Keys = CreateSizeAwareList(keys, Keys_CollectionChanged);
        Indices = CreateSizeAwareList(indices, Indices_CollectionChanged);
    }
    
    private void Keys_CollectionChanged(object sender, System.Collections.Specialized.NotifyCollectionChangedEventArgs e) => OnPropertyChanged(nameof(Keys));
    
    private void Indices_CollectionChanged(object sender, System.Collections.Specialized.NotifyCollectionChangedEventArgs e) => OnPropertyChanged(nameof(Indices));

    public override IEnumerable<InvalidP3DException> ValidateChunk()
    {
        foreach (var error in base.ValidateChunk())
            yield return error;

        if (Keys.Count != Indices.Count)
            yield return new InvalidP3DException(this, $"{nameof(Keys)} and {nameof(Indices)} must have equal counts.");
    }

    protected override void WriteData(EndianAwareBinaryWriter bw)
    {
        bw.Write(Version);
        bw.WriteP3DString(Name);
        bw.Write(NumKeys);
        foreach (var key in Keys)
            bw.Write(key);
        foreach (var index in Indices)
            bw.Write(index);
    }

    protected override Chunk CloneSelf() => new ExpressionChunk(Version, Name, Keys, Indices);
}
