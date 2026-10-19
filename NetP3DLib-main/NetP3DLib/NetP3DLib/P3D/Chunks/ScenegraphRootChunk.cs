using NetP3DLib.IO;
using NetP3DLib.P3D.Attributes;
using NetP3DLib.P3D.Enums;
using System;

namespace NetP3DLib.P3D.Chunks;

[ChunkAttributes(ChunkID)]
public class ScenegraphRootChunk : Chunk
{
    public const ChunkIdentifier ChunkID = ChunkIdentifier.Scenegraph_Root;

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
    

    public override byte[] DataBytes
    {
        get
        {
            return BitConverter.GetBytes(Version);
        }
    }
    public override uint DataLength => sizeof(uint);

    public ScenegraphRootChunk(EndianAwareBinaryReader br) : this(br.ReadUInt32())
    {
    }

    public ScenegraphRootChunk(uint version) : base(ChunkID)
    {
        _version = version;
    }

    protected override void WriteData(EndianAwareBinaryWriter bw)
    {
        bw.Write(Version);
    }

    protected override Chunk CloneSelf() => new OldScenegraphRootChunk();
}
