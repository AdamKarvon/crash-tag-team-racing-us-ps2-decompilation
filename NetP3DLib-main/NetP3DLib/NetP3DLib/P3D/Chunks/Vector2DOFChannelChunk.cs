using NetP3DLib.IO;
using NetP3DLib.P3D.Attributes;
using NetP3DLib.P3D.Collections;
using NetP3DLib.P3D.Enums;
using NetP3DLib.P3D.Exceptions;
using NetP3DLib.P3D.Extensions;
using System;
using System.Collections.Generic;
using System.ComponentModel;
using System.IO;
using System.Numerics;

namespace NetP3DLib.P3D.Chunks;

[ChunkAttributes(ChunkID)]
public class Vector2DOFChannelChunk : ParamChunk
{
    public const ChunkIdentifier ChunkID = ChunkIdentifier.Vector_2D_OF_Channel;

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
    
    private Coordinate _staticIndex;
    public Coordinate StaticIndex
    {
        get => _staticIndex;
        set
        {
            if (_staticIndex == value)
                return;
    
            _staticIndex = value;
            OnPropertyChanged(nameof(StaticIndex));
        }
    }
    
    private Vector3 _constants;
    public Vector3 Constants
    {
        get => _constants;
        set
        {
            if (_constants == value)
                return;
    
            _constants = value;
            OnPropertyChanged(nameof(Constants));
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
                var newValues = new Vector2[count];

                for (var i = 0; i < count; i++)
                    newValues[i] = default;

                Values.AddRange(newValues);
            }
            NumFrames = value;
        }
    }
    public SizeAwareList<Vector2> Values { get; }

    public override byte[] DataBytes
    {
        get
        {
            var data = new List<byte>((int)DataLength);

            data.AddRange(BitConverter.GetBytes(Version));
            data.AddRange(BinaryExtensions.GetFourCCBytes(Param));
            data.AddRange(BitConverter.GetBytes((ushort)StaticIndex));
            data.AddRange(BinaryExtensions.GetBytes(Constants));
            data.AddRange(BitConverter.GetBytes(NumFrames));
            foreach (var frame in Frames)
                data.AddRange(BitConverter.GetBytes(frame));
            foreach (var value in Values)
                data.AddRange(BinaryExtensions.GetBytes(value));

            return [.. data];
        }
    }
    public override uint DataLength => sizeof(uint) + 4 + sizeof(ushort) + sizeof(float) * 3 + sizeof(uint) + sizeof(ushort) * NumFrames + sizeof(float) * 2 * NumValues;

    public Vector2DOFChannelChunk(EndianAwareBinaryReader br) : this(br.ReadUInt32(), br.ReadFourCC(), (Coordinate)br.ReadInt16(), br.ReadVector3(), br.ReadUInt16Array(out var numFrames), br.ReadVector2Array(numFrames))
    {
    }

    public Vector2DOFChannelChunk(uint version, string param, Coordinate staticIndex, Vector3 constants, IList<ushort> frames, IList<Vector2> values) : base(ChunkID, param)
    {
        _version = version;
        _staticIndex = staticIndex;
        _constants = constants;
        Frames = CreateSizeAwareList(frames, Frames_CollectionChanged);
        Values = CreateSizeAwareList(values, Values_CollectionChanged);
    }
    
    private void Frames_CollectionChanged(object sender, System.Collections.Specialized.NotifyCollectionChangedEventArgs e) => OnPropertyChanged(nameof(Frames));
    
    private void Values_CollectionChanged(object sender, System.Collections.Specialized.NotifyCollectionChangedEventArgs e) => OnPropertyChanged(nameof(Values));

    public Vector3[] GetValues()
    {
        var values = new Vector3[Values.Count];

        Func<Vector2, Vector3> map = StaticIndex switch
        {
            Coordinate.X => v => new Vector3(Constants.X, v.X, v.Y),
            Coordinate.Y => v => new Vector3(v.X, Constants.Y, v.Y),
            Coordinate.Z => v => new Vector3(v.X, v.Y, Constants.Z),
            _ => throw new InvalidDataException($"Invalid {nameof(StaticIndex)} value: {StaticIndex}"),
        };

        for (var i = 0; i < Values.Count; i++)
            values[i] = map(Values[i]);

        return values;
    }

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
        bw.Write((ushort)StaticIndex);
        bw.Write(Constants);
        bw.Write(NumFrames);
        foreach (var frame in Frames)
            bw.Write(frame);
        foreach (var value in Values)
            bw.Write(value);
    }

    protected override Chunk CloneSelf() => new Vector2DOFChannelChunk(Version, Param, StaticIndex, Constants, Frames, Values);

    internal uint CalculateMemorySize(AnimationChunk.Platform platform, uint size)
    {
        size = ((size + 3u) & ~3u) + (platform == AnimationChunk.Platform.PS2 ? 52u : 48u);
        size = ((size + 3u) & ~3u) + NumFrames * 2;
        size = ((size + 3u) & ~3u) + NumFrames * 2 * 4;

        return size;
    }
}
