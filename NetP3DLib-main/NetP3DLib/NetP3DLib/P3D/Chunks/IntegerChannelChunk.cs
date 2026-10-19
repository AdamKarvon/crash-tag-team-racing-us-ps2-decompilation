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
public class IntegerChannelChunk : ParamChunk
{
    public const ChunkIdentifier ChunkID = ChunkIdentifier.Integer_Channel;

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

    public uint NumFrames
    {
        get => (uint)(Frames?.Count ?? 0);
        set
        {
            if (value == NumFrames)
                return;

            if (value < NumFrames)
            {
                Frames.RemoveRange((int)value, (int)(NumFrames - value));
            }
            else
            {
                int count = (int)(value - NumFrames);
                var newFrames = new ushort[count];

                for (var i = 0; i < count; i++)
                    newFrames[i] = default;

                Frames.AddRange(newFrames);
            }
            NumValues = value;
        }
    }
    public SizeAwareList<ushort> Frames { get; }
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
                var newValues = new int[count];

                for (var i = 0; i < count; i++)
                    newValues[i] = default;

                Values.AddRange(newValues);
            }
            NumFrames = value;
        }
    }
    public SizeAwareList<int> Values { get; }

    public override byte[] DataBytes
    {
        get
        {
            var data = new List<byte>((int)DataLength);

            data.AddRange(BitConverter.GetBytes(Version));
            data.AddRange(BinaryExtensions.GetFourCCBytes(Param));
            data.AddRange(BitConverter.GetBytes(NumFrames));
            foreach (var frame in Frames)
                data.AddRange(BitConverter.GetBytes(frame));
            foreach (var value in Values)
                data.AddRange(BitConverter.GetBytes(value));

            return [.. data];
        }
    }
    public override uint DataLength => sizeof(uint) + 4 + sizeof(uint) + sizeof(ushort) * NumFrames + sizeof(int) * NumValues;

    public IntegerChannelChunk(EndianAwareBinaryReader br) : this(br.ReadUInt32(), br.ReadFourCC(), br.ReadUInt16Array(out var numFrames), br.ReadInt32Array(numFrames))
    {
    }

    public IntegerChannelChunk(uint version, string param, IList<ushort> frames, IList<int> values) : base(ChunkID, param)
    {
        _version = version;
        Frames = CreateSizeAwareList(frames, Frames_CollectionChanged);
        Values = CreateSizeAwareList(values, Values_CollectionChanged);
    }
    
    private void Frames_CollectionChanged(object sender, System.Collections.Specialized.NotifyCollectionChangedEventArgs e) => OnPropertyChanged(nameof(Frames));
    
    private void Values_CollectionChanged(object sender, System.Collections.Specialized.NotifyCollectionChangedEventArgs e) => OnPropertyChanged(nameof(Values));

    public override IEnumerable<InvalidP3DException> ValidateChunk()
    {
        foreach (var error in base.ValidateChunk())
            yield return error;

        if (Frames.Count != Values.Count)
            yield return new InvalidP3DException(this, $"{nameof(Frames)} and {nameof(Values)} must have equal counts.");
    }

    protected override void WriteData(EndianAwareBinaryWriter bw)
    {
        bw.Write(Version);
        bw.WriteFourCC(Param);
        bw.Write(NumFrames);
        foreach (var frame in Frames)
            bw.Write(frame);
        foreach (var value in Values)
            bw.Write(value);
    }

    protected override Chunk CloneSelf() => new IntegerChannelChunk(Version, Param, Frames, Values);

    internal uint CalculateMemorySize(AnimationChunk.Platform platform, uint size)
    {
        size = ((size + 3u) & ~3u) + (platform == AnimationChunk.Platform.PS2 ? 36u : 32u);
        size = ((size + 3u) & ~3u) + NumFrames * 2;
        size = ((size + 3u) & ~3u) + NumFrames * 4;

        return size;
    }
}
