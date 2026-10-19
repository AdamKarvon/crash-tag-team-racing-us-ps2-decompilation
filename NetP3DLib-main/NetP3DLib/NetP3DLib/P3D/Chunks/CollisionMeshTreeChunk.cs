using NetP3DLib.IO;
using NetP3DLib.P3D.Attributes;
using NetP3DLib.P3D.Enums;
using NetP3DLib.P3D.Extensions;
using System;
using System.Collections.Generic;
using System.Numerics;

namespace NetP3DLib.P3D.Chunks;

[ChunkAttributes(ChunkID)]
public class CollisionMeshTreeChunk : Chunk
{
    public const ChunkIdentifier ChunkID = ChunkIdentifier.Collision_Mesh_Tree;

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
    
    private uint _firstNode;
    public uint FirstNode
    {
        get => _firstNode;
        set
        {
            if (_firstNode == value)
                return;
    
            _firstNode = value;
            OnPropertyChanged(nameof(FirstNode));
        }
    }
    
    private uint _hierarchyIndex;
    public uint HierarchyIndex
    {
        get => _hierarchyIndex;
        set
        {
            if (_hierarchyIndex == value)
                return;
    
            _hierarchyIndex = value;
            OnPropertyChanged(nameof(HierarchyIndex));
        }
    }
    
    private Vector3 _low;
    public Vector3 Low
    {
        get => _low;
        set
        {
            if (_low == value)
                return;
    
            _low = value;
            OnPropertyChanged(nameof(Low));
        }
    }
    
    private Vector3 _high;
    public Vector3 High
    {
        get => _high;
        set
        {
            if (_high == value)
                return;
    
            _high = value;
            OnPropertyChanged(nameof(High));
        }
    }
    

    public override byte[] DataBytes
    {
        get
        {
            var data = new List<byte>((int)DataLength);

            data.AddRange(BitConverter.GetBytes(Version));
            data.AddRange(BitConverter.GetBytes(FirstNode));
            data.AddRange(BitConverter.GetBytes(HierarchyIndex));
            data.AddRange(BinaryExtensions.GetBytes(Low));
            data.AddRange(BinaryExtensions.GetBytes(High));

            return [.. data];
        }
    }
    public override uint DataLength => sizeof(uint) + sizeof(uint) + sizeof(uint) + sizeof(float) * 3 + sizeof(float) * 3;

    public CollisionMeshTreeChunk(EndianAwareBinaryReader br) : this(br.ReadUInt32(), br.ReadUInt32(), br.ReadUInt32(), br.ReadVector3(), br.ReadVector3())
    {
    }

    public CollisionMeshTreeChunk(uint version, uint firstNode, uint hierarchyIndex, Vector3 low, Vector3 high) : base(ChunkID)
    {
        _version = version;
        _firstNode = firstNode;
        _hierarchyIndex = hierarchyIndex;
        _low = low;
        _high = high;
    }

    protected override void WriteData(EndianAwareBinaryWriter bw)
    {
        bw.Write(Version);
        bw.Write(FirstNode);
        bw.Write(HierarchyIndex);
        bw.Write(Low);
        bw.Write(High);
    }

    protected override Chunk CloneSelf() => new CollisionMeshTreeChunk(Version, FirstNode, HierarchyIndex, Low, High);
}
