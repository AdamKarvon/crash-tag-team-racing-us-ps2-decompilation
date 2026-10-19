using NetP3DLib.IO;
using NetP3DLib.P3D.Attributes;
using NetP3DLib.P3D.Enums;
using System;
using System.Collections.Generic;

namespace NetP3DLib.P3D.Chunks;

[ChunkAttributes(ChunkID)]
public class SortOrderChunk : Chunk
{
    public const ChunkIdentifier ChunkID = ChunkIdentifier.Sort_Order;

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
    
    private float _sortOrder;
    public float SortOrder
    {
        get => _sortOrder;
        set
        {
            if (_sortOrder == value)
                return;
    
            _sortOrder = value;
            OnPropertyChanged(nameof(SortOrder));
        }
    }
    

    public override byte[] DataBytes
    {
        get
        {
            var data = new List<byte>((int)DataLength);

            data.AddRange(BitConverter.GetBytes(Version));
            data.AddRange(BitConverter.GetBytes(SortOrder));

            return [.. data];
        }
    }
    public override uint DataLength => sizeof(uint) + sizeof(uint);

    public SortOrderChunk(EndianAwareBinaryReader br) : this(br.ReadUInt32(), br.ReadSingle())
    {
    }

    public SortOrderChunk(uint version, float sortOrder) : base(ChunkID)
    {
        _version = version;
        _sortOrder = sortOrder;
    }

    protected override void WriteData(EndianAwareBinaryWriter bw)
    {
        bw.Write(Version);
        bw.Write(SortOrder);
    }

    protected override Chunk CloneSelf() => new SortOrderChunk(Version, SortOrder);
}
