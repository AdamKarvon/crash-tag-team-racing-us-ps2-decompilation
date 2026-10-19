using NetP3DLib.IO;
using NetP3DLib.P3D.Attributes;
using NetP3DLib.P3D.Collections;
using NetP3DLib.P3D.Enums;
using NetP3DLib.P3D.Extensions;
using System;
using System.Collections.Generic;
using System.ComponentModel;

namespace NetP3DLib.P3D.Chunks;

[ChunkAttributes(ChunkID)]
public class BooleanChannelChunk : ParamChunk
{
    public const ChunkIdentifier ChunkID = ChunkIdentifier.Boolean_Channel;

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
    
    private ushort _startState;
    public bool StartState
    {
        get => _startState == 1;
        set
        {
            if (StartState == value)
                return;

            _startState = (ushort)(value ? 1u : 0u);
            OnPropertyChanged(nameof(StartState));
        }
    }
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
                var newValues = new ushort[count];

                for (var i = 0; i < count; i++)
                    newValues[i] = default;

                Values.AddRange(newValues);
            }
        }
    }
    public SizeAwareList<ushort> Values { get; }

    public override byte[] DataBytes
    {
        get
        {
            var data = new List<byte>((int)DataLength);

            data.AddRange(BitConverter.GetBytes(Version));
            data.AddRange(BinaryExtensions.GetFourCCBytes(Param));
            data.AddRange(BitConverter.GetBytes((ushort)(StartState ? 1 : 0)));
            data.AddRange(BitConverter.GetBytes(NumValues));
            foreach (var value in Values)
                data.AddRange(BitConverter.GetBytes(value));

            return [.. data];
        }
    }
    public override uint DataLength => sizeof(uint) + 4 + sizeof(ushort) + sizeof(uint) + sizeof(ushort) * NumValues;

    public BooleanChannelChunk(EndianAwareBinaryReader br) : this(br.ReadUInt32(), br.ReadFourCC(), br.ReadUInt16(), br.ReadUInt16Array(out _))
    {
    }

    public BooleanChannelChunk(uint version, string param, bool startState, IList<ushort> values) : this(version, param, (ushort)(startState ? 1u : 0u), values)
    {
    }

    public BooleanChannelChunk(uint version, string param, ushort startState, IList<ushort> values) : base(ChunkID, param)
    {
        _version = version;
        _startState = startState;
        Values = CreateSizeAwareList(values, Values_CollectionChanged);
    }
    
    private void Values_CollectionChanged(object sender, System.Collections.Specialized.NotifyCollectionChangedEventArgs e) => OnPropertyChanged(nameof(Values));

    protected override void WriteData(EndianAwareBinaryWriter bw)
    {
        bw.Write(Version);
        bw.WriteFourCC(Param);
        bw.Write((ushort)(StartState ? 1 : 0));
        bw.Write(NumValues);
        foreach (var value in Values)
            bw.Write(value);
    }

    protected override Chunk CloneSelf() => new BooleanChannelChunk(Version, Param, StartState, Values);

    internal uint CalculateMemorySize(AnimationChunk.Platform platform, uint size)
    {
        size = ((size + 3u) & ~3u) + (platform == AnimationChunk.Platform.PS2 ? 36u : 32u);
        size = ((size + 3u) & ~3u) + NumValues * 2;

        return size;
    }
}
