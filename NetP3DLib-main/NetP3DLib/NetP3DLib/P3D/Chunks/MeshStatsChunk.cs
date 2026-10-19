using NetP3DLib.IO;
using NetP3DLib.P3D.Attributes;
using NetP3DLib.P3D.Enums;
using NetP3DLib.P3D.Extensions;
using System;
using System.Collections.Generic;

namespace NetP3DLib.P3D.Chunks;

[ChunkAttributes(ChunkID)]
public class MeshStatsChunk : NamedChunk
{
    public const ChunkIdentifier ChunkID = ChunkIdentifier.Mesh_Stats;

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
    
    private uint _isRendered;
    public uint IsRendered
    {
        get => _isRendered;
        set
        {
            if (_isRendered == value)
                return;
    
            _isRendered = value;
            OnPropertyChanged(nameof(IsRendered));
        }
    }
    
    private uint _isCollision;
    public uint IsCollision
    {
        get => _isCollision;
        set
        {
            if (_isCollision == value)
                return;
    
            _isCollision = value;
            OnPropertyChanged(nameof(IsCollision));
        }
    }
    

    public override byte[] DataBytes
    {
        get
        {
            var data = new List<byte>((int)DataLength);

            data.AddRange(BinaryExtensions.GetP3DStringBytes(Name));
            data.AddRange(BitConverter.GetBytes(Version));
            data.AddRange(BitConverter.GetBytes(IsRendered));

            return [.. data];
        }
    }
    public override uint DataLength => sizeof(uint) + BinaryExtensions.GetP3DStringLength(Name) + sizeof(uint) + sizeof(uint);

    public MeshStatsChunk(EndianAwareBinaryReader br) : this(br.ReadUInt32(), br.ReadP3DString(), br.ReadUInt32(), br.ReadUInt32())
    {
    }

    public MeshStatsChunk(uint version, string name, uint isRendered, uint isCollision) : base(ChunkID, name)
    {
        _version = version;
        _isRendered = isRendered;
        _isCollision = isCollision;
    }

    protected override void WriteData(EndianAwareBinaryWriter bw)
    {
        bw.Write(Version);
        bw.WriteP3DString(Name);
        bw.Write(IsRendered);
        bw.Write(IsCollision);
    }

    protected override Chunk CloneSelf() => new MeshStatsChunk(Version, Name, IsRendered, IsCollision);
}
