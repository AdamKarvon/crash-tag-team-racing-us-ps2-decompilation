using NetP3DLib.IO;
using NetP3DLib.P3D.Attributes;
using NetP3DLib.P3D.Enums;
using NetP3DLib.P3D.Exceptions;
using NetP3DLib.P3D.Extensions;
using System;
using System.Collections.Generic;

namespace NetP3DLib.P3D.Chunks;

[ChunkAttributes(ChunkID)]
public class TreeNodeChunk : Chunk
{
    public const ChunkIdentifier ChunkID = ChunkIdentifier.Tree_Node;

    internal uint _cachedSubTreeSize;
    public uint SubTreeSize
    {
        get
        {
            if (ParentChunk is not TreeChunk treeChunk)
                return 0;

            treeChunk.RecalculateSubTreeSizeIfNeeded();
            return _cachedSubTreeSize;
        }
    }
    private int _parentOffset;
    public int ParentOffset
    {
        get => _parentOffset;
        set
        {
            if (_parentOffset == value)
                return;

            _parentOffset = value;
            OnPropertyChanged(nameof(ParentOffset));
        }
    }

    public override byte[] DataBytes
    {
        get
        {
            var data = new List<byte>((int)DataLength);

            data.AddRange(BitConverter.GetBytes(SubTreeSize));
            data.AddRange(BitConverter.GetBytes(ParentOffset));

            return [.. data];
        }
    }
    public override uint DataLength => sizeof(uint) + sizeof(int);

    public TreeNodeChunk(EndianAwareBinaryReader br) : this(br.SkipAndRead(sizeof(uint), br.ReadInt32))
    {
    }

    public TreeNodeChunk(int parentOffset) : base(ChunkID)
    {
        ParentOffset = parentOffset;
    }

    public override IEnumerable<InvalidP3DException> ValidateChunk()
    {
        foreach (var error in base.ValidateChunk())
            yield return error;

        if (Children.Count == 0)
            yield return new InvalidP3DException(this, $"There must be at least one Spatial Node child chunk.");
        foreach (var child in Children)
            if (child.ID != (uint)ChunkIdentifier.Spatial_Node)
                yield return new InvalidP3DException(this, $"Child chunk {child} is invalid. Child chunks must be an instance of Spatial Node.");
    }

    protected override void WriteData(EndianAwareBinaryWriter bw)
    {
        bw.Write(SubTreeSize);
        bw.Write(ParentOffset);
    }

    protected override Chunk CloneSelf() => new TreeNodeChunk(ParentOffset);
}