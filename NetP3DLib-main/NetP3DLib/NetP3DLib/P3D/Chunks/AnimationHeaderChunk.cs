using NetP3DLib.IO;
using NetP3DLib.P3D.Attributes;
using NetP3DLib.P3D.Enums;
using System;
using System.Collections.Generic;
using System.ComponentModel;

namespace NetP3DLib.P3D.Chunks;

[ChunkAttributes(ChunkID)]
public class AnimationHeaderChunk : Chunk
{
    public const ChunkIdentifier ChunkID = ChunkIdentifier.Animation_Header;

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
    
    private uint _numGroups;
    public uint NumGroups
    {
        get => _numGroups;
        set
        {
            if (_numGroups == value)
                return;
    
            _numGroups = value;
            OnPropertyChanged(nameof(NumGroups));
        }
    }
    

    public override byte[] DataBytes
    {
        get
        {
            var data = new List<byte>((int)DataLength);

            data.AddRange(BitConverter.GetBytes(Version));
            data.AddRange(BitConverter.GetBytes(NumGroups));

            return [.. data];
        }
    }
    public override uint DataLength => sizeof(uint) + sizeof(uint);

    public AnimationHeaderChunk(EndianAwareBinaryReader br) : this(br.ReadUInt32(), br.ReadUInt32())
    {
    }

    public AnimationHeaderChunk(uint version, uint numGroups) : base(ChunkID)
    {
        _version = version;
        _numGroups = numGroups;
    }

    protected override void WriteData(EndianAwareBinaryWriter bw)
    {
        bw.Write(Version);
        bw.Write(NumGroups);
    }

    protected override Chunk CloneSelf() => new AnimationHeaderChunk(Version, NumGroups);
}
