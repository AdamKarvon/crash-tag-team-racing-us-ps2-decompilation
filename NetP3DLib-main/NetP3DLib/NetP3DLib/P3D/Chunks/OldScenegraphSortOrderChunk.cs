using NetP3DLib.IO;
using NetP3DLib.P3D.Attributes;
using NetP3DLib.P3D.Enums;
using System;
using System.Collections.Generic;

namespace NetP3DLib.P3D.Chunks;

[ChunkAttributes(ChunkID)]
public class OldScenegraphSortOrderChunk : Chunk
{
    public const ChunkIdentifier ChunkID = ChunkIdentifier.Old_Scenegraph_Sort_Order;

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

            data.AddRange(BitConverter.GetBytes(SortOrder));

            return [.. data];
        }
    }
    public override uint DataLength => sizeof(float);

    public OldScenegraphSortOrderChunk(EndianAwareBinaryReader br) : this(br.ReadSingle())
    {
    }

    public OldScenegraphSortOrderChunk(float sortOrder) : base(ChunkID)
    {
        _sortOrder = sortOrder;
    }

    protected override void WriteData(EndianAwareBinaryWriter bw)
    {
        bw.Write(SortOrder);
    }

    protected override Chunk CloneSelf() => new OldScenegraphSortOrderChunk(SortOrder);
}
