using NetP3DLib.IO;
using NetP3DLib.P3D.Attributes;
using NetP3DLib.P3D.Enums;
using System;
using System.Collections.Generic;

namespace NetP3DLib.P3D.Chunks;

[ChunkAttributes(ChunkID)]
public class AnimationSyncFrameChunk : Chunk
{
    public const ChunkIdentifier ChunkID = ChunkIdentifier.Animation_Sync_Frame;

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
    
    private float _syncFrame;
    public float SyncFrame
    {
        get => _syncFrame;
        set
        {
            if (_syncFrame == value)
                return;
    
            _syncFrame = value;
            OnPropertyChanged(nameof(SyncFrame));
        }
    }
    

    public override byte[] DataBytes
    {
        get
        {
            var data = new List<byte>((int)DataLength);

            data.AddRange(BitConverter.GetBytes(Version));
            data.AddRange(BitConverter.GetBytes(SyncFrame));

            return [.. data];
        }
    }
    public override uint DataLength => sizeof(uint) + sizeof(float);

    public AnimationSyncFrameChunk(EndianAwareBinaryReader br) : this(br.ReadUInt32(), br.ReadSingle())
    {
    }

    public AnimationSyncFrameChunk(uint version, float syncFrame) : base(ChunkID)
    {
        _version = version;
        _syncFrame = syncFrame;
    }

    protected override void WriteData(EndianAwareBinaryWriter bw)
    {
        bw.Write(Version);
        bw.Write(SyncFrame);
    }

    protected override Chunk CloneSelf() => new AnimationSyncFrameChunk(Version, SyncFrame);
}
