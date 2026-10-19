using NetP3DLib.IO;
using NetP3DLib.P3D.Attributes;
using NetP3DLib.P3D.Enums;
using System;
using System.Collections.Generic;

namespace NetP3DLib.P3D.Chunks;

[ChunkAttributes(ChunkID)]
public class VertexAnimKeyFrameChunk : Chunk
{
    public const ChunkIdentifier ChunkID = ChunkIdentifier.Vertex_Anim_Key_Frame;

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
    
    private uint _keyFrameId;
    public uint KeyFrameId
    {
        get => _keyFrameId;
        set
        {
            if (_keyFrameId == value)
                return;
    
            _keyFrameId = value;
            OnPropertyChanged(nameof(KeyFrameId));
        }
    }
    
    private uint _primGroupIndex;
    public uint PrimGroupIndex
    {
        get => _primGroupIndex;
        set
        {
            if (_primGroupIndex == value)
                return;
    
            _primGroupIndex = value;
            OnPropertyChanged(nameof(PrimGroupIndex));
        }
    }
    

    public override byte[] DataBytes
    {
        get
        {
            var data = new List<byte>((int)DataLength);

            data.AddRange(BitConverter.GetBytes(Version));
            data.AddRange(BitConverter.GetBytes(KeyFrameId));
            data.AddRange(BitConverter.GetBytes(PrimGroupIndex));

            return [.. data];
        }
    }
    public override uint DataLength => sizeof(uint) + sizeof(uint) + sizeof(uint);

    public VertexAnimKeyFrameChunk(EndianAwareBinaryReader br) : this(br.ReadUInt32(), br.ReadUInt32(), br.ReadUInt32())
    {
    }

    public VertexAnimKeyFrameChunk(uint version, uint keyFrameId, uint primGroupIndex) : base(ChunkID)
    {
        _version = version;
        _keyFrameId = keyFrameId;
        _primGroupIndex = primGroupIndex;
    }

    protected override void WriteData(EndianAwareBinaryWriter bw)
    {
        bw.Write(Version);
        bw.Write(KeyFrameId);
        bw.Write(PrimGroupIndex);
    }

    protected override Chunk CloneSelf() => new VertexAnimKeyFrameChunk(Version, KeyFrameId, PrimGroupIndex);
}
