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
public class AnimationChannelCountChunk : Chunk
{
    public const ChunkIdentifier ChunkID = ChunkIdentifier.Animation_Channel_Count;

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
    
    private uint _channelChunkID;
    public uint ChannelChunkID
    {
        get => _channelChunkID;
        set
        {
            if (_channelChunkID == value)
                return;
    
            _channelChunkID = value;
            OnPropertyChanged(nameof(ChannelChunkID));
        }
    }
    
    public uint NumNumKeys
    {
        get => (uint)(NumKeys?.Count ?? 0);
        set
        {
            if (value == NumNumKeys)
                return;

            if (value < NumNumKeys)
            {
                NumKeys.RemoveRange((int)value, (int)(NumNumKeys - value));
            }
            else
            {
                int count = (int)(value - NumNumKeys);
                var newKeys = new ushort[count];

                for (var i = 0; i < count; i++)
                    newKeys[i] = default;

                NumKeys.AddRange(newKeys);
            }
        }
    }
    public SizeAwareList<ushort> NumKeys { get; }

    public override byte[] DataBytes
    {
        get
        {
            var data = new List<byte>((int)DataLength);

            data.AddRange(BitConverter.GetBytes(Version));
            data.AddRange(BitConverter.GetBytes(ChannelChunkID));
            data.AddRange(BitConverter.GetBytes(NumNumKeys));
            foreach (var key in NumKeys)
                data.AddRange(BitConverter.GetBytes(key));

            return [.. data];
        }
    }
    public override uint DataLength => sizeof(uint) + sizeof(uint) + sizeof(uint) + sizeof(ushort) * NumNumKeys;

    public AnimationChannelCountChunk(EndianAwareBinaryReader br) : this(br.ReadUInt32(), br.ReadUInt32(), br.ReadUInt16Array(out _))
    {
    }

    public AnimationChannelCountChunk(uint version, uint channelChunkID, IList<ushort> numKeys) : base(ChunkID)
    {
        _version = version;
        _channelChunkID = channelChunkID;
        NumKeys = CreateSizeAwareList(numKeys, NumKeys_CollectionChanged);
    }
    
    private void NumKeys_CollectionChanged(object sender, System.Collections.Specialized.NotifyCollectionChangedEventArgs e) => OnPropertyChanged(nameof(NumKeys));

    protected override void WriteData(EndianAwareBinaryWriter bw)
    {
        bw.Write(Version);
        bw.Write(ChannelChunkID);
        bw.Write(NumNumKeys);
        foreach (var key in NumKeys)
            bw.Write(key);
    }

    protected override Chunk CloneSelf() => new AnimationChannelCountChunk(Version, ChannelChunkID, NumKeys);
}
