using NetP3DLib.IO;
using NetP3DLib.P3D.Attributes;
using NetP3DLib.P3D.Enums;
using System;
using System.Collections.Generic;
using System.ComponentModel;

namespace NetP3DLib.P3D.Chunks;

[ChunkAttributes(ChunkID)]
public class ChannelInterpolationModeChunk : Chunk
{
    public const ChunkIdentifier ChunkID = ChunkIdentifier.Channel_Interpolation_Mode;

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
    
    private uint _interpolate;
    public bool Interpolate
    {
        get => _interpolate != 0;
        set
        {
            if (Interpolate == value)
                return;

            _interpolate = value ? 1u : 0u;
            OnPropertyChanged(nameof(Interpolate));
        }
    }

    public override byte[] DataBytes
    {
        get
        {
            var data = new List<byte>((int)DataLength);

            data.AddRange(BitConverter.GetBytes(Version));
            data.AddRange(BitConverter.GetBytes(_interpolate));

            return [.. data];
        }
    }
    public override uint DataLength => sizeof(uint) + sizeof(uint);

    public ChannelInterpolationModeChunk(EndianAwareBinaryReader br) : this(br.ReadUInt32(), br.ReadUInt32())
    {
    }

    public ChannelInterpolationModeChunk(uint version, bool interpolate) : this(version, interpolate ? 1u : 0u)
    {
    }

    public ChannelInterpolationModeChunk(uint version, uint interpolate) : base(ChunkID)
    {
        _version = version;
        _interpolate = interpolate;
    }

    protected override void WriteData(EndianAwareBinaryWriter bw)
    {
        bw.Write(Version);
        bw.Write(_interpolate);
    }

    protected override Chunk CloneSelf() => new ChannelInterpolationModeChunk(Version, Interpolate);
}
