using NetP3DLib.IO;
using NetP3DLib.P3D.Attributes;
using NetP3DLib.P3D.Enums;
using NetP3DLib.P3D.Extensions;
using System;
using System.Collections.Generic;
using System.Numerics;

namespace NetP3DLib.P3D.Chunks;

[ChunkAttributes(ChunkID)]
public class TreeChunk : Chunk
{
    public const ChunkIdentifier ChunkID = ChunkIdentifier.Tree;

    public uint NumNodes => GetChildCount(ChunkIdentifier.Tree_Node);
    private Vector3 _boundsMin;
    public Vector3 BoundsMin
    {
        get => _boundsMin;
        set
        {
            if (_boundsMin == value)
                return;
    
            _boundsMin = value;
            OnPropertyChanged(nameof(BoundsMin));
        }
    }
    
    private Vector3 _boundsMax;
    public Vector3 BoundsMax
    {
        get => _boundsMax;
        set
        {
            if (_boundsMax == value)
                return;
    
            _boundsMax = value;
            OnPropertyChanged(nameof(BoundsMax));
        }
    }
    

    public override byte[] DataBytes
    {
        get
        {
            var data = new List<byte>((int)DataLength);

            data.AddRange(BitConverter.GetBytes(NumNodes));
            data.AddRange(BinaryExtensions.GetBytes(BoundsMin));
            data.AddRange(BinaryExtensions.GetBytes(BoundsMax));

            return [.. data];
        }
    }
    public override uint DataLength => sizeof(uint) + sizeof(float) * 3 + sizeof(float) * 3;

    public TreeChunk(EndianAwareBinaryReader br) : this(br.SkipAndRead(sizeof(uint), br.ReadVector3), br.ReadVector3())
    {
    }

    public TreeChunk(Vector3 minimum, Vector3 maximum) : base(ChunkID)
    {
        _boundsMin = minimum;
        _boundsMax = maximum;

        ChildAdded += Chunk_ChildAdded;
        ChildRemoved += Chunk_ChildRemoved;
        ChildrenAdded += Chunk_ChildrenAdded;
        ChildrenRemoved += Chunk_ChildrenRemoved;
        ChildrenCleared += Chunk_ChildrenCleared;
    }

    protected override void WriteData(EndianAwareBinaryWriter bw)
    {
        bw.Write(NumNodes);
        bw.Write(BoundsMin);
        bw.Write(BoundsMax);
    }

    protected override Chunk CloneSelf() => new TreeChunk(BoundsMin, BoundsMax);

    private bool _needsNewNodeList = true;
    private bool _needsRecalculate = true;
    private IReadOnlyList<TreeNodeChunk>? _cachedNodeList = null;

    internal void MarkDirty() => _needsRecalculate = true;

    internal void MarkTopologyDirty()
    {
        _needsNewNodeList = true;
        _needsRecalculate = true;
    }

    private void HandleChildAdded(Chunk child)
    {
        if (child is not TreeNodeChunk)
            return;

        MarkTopologyDirty();
        child.PropertyChanged += TreeNode_PropertyChanged;
    }

    private void HandleChildRemoved(Chunk child)
    {
        if (child is not TreeNodeChunk)
            return;

        MarkTopologyDirty();
        child.PropertyChanged -= TreeNode_PropertyChanged;
    }

    private void Chunk_ChildAdded(Chunk child) => HandleChildAdded(child);

    private void Chunk_ChildRemoved(Chunk child, int oldIndex) => HandleChildRemoved(child);

    private void Chunk_ChildrenAdded(IReadOnlyList<Chunk> children)
    {
        foreach (var child in children)
            HandleChildAdded(child);
    }

    private void Chunk_ChildrenRemoved(IReadOnlyList<(Chunk child, int oldIndex)> children)
    {
        foreach (var (child, _) in children)
            HandleChildRemoved(child);
    }

    private void TreeNode_PropertyChanged(string propertyName) => MarkDirty();

    private void Chunk_ChildrenCleared(IReadOnlyList<(Chunk chunk, int oldIndex)> children)
    {
        foreach (var (child, _) in children)
            HandleChildAdded(child);
    }

    internal void RecalculateSubTreeSizeIfNeeded()
    {
        if (_needsNewNodeList || _cachedNodeList == null)
        {
            _cachedNodeList = GetChunksOfType<TreeNodeChunk>();
            _needsNewNodeList = false;
        }

        if (_needsRecalculate)
        {
            ComputeSubTreeSizeForAllNodes();
            _needsRecalculate = false;
        }
    }

    private void ComputeSubTreeSizeForAllNodes()
    {
        var nodes = _cachedNodeList;
        if (nodes == null)
            return;

        for (int i = 0; i < nodes.Count; i++)
            nodes[i]._cachedSubTreeSize = 0;

        for (int i = nodes.Count - 1; i >= 0; i--)
        {
            var node = nodes[i];
            if (node.ParentOffset == 0)
                continue;

            int parentIndex = i + node.ParentOffset;
            if (parentIndex >= 0 && parentIndex < nodes.Count)
                nodes[parentIndex]._cachedSubTreeSize += 1 + node._cachedSubTreeSize;
        }
    }
}
