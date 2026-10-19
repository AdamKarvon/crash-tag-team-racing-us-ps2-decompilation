using NetP3DLib.IO;
using NetP3DLib.P3D.Attributes;
using NetP3DLib.P3D.Enums;
using System;
using System.Collections.Generic;

namespace NetP3DLib.P3D.Chunks;

[ChunkAttributes(ChunkID)]
public class SpatialNodeChunk : Chunk
{
    public const ChunkIdentifier ChunkID = ChunkIdentifier.Spatial_Node;

    public enum Axis : sbyte
    {
        None = -1,
        X = 0,
        Y = 1,
        Z = 2,
    }

    private Axis _splitAxis;
    public Axis SplitAxis
    {
        get => _splitAxis;
        set
        {
            if (_splitAxis == value)
                return;
    
            _splitAxis = value;
            OnPropertyChanged(nameof(SplitAxis));
        }
    }
    
    private float _splitPosition;
    public float SplitPosition
    {
        get => _splitPosition;
        set
        {
            if (_splitPosition == value)
                return;
    
            _splitPosition = value;
            OnPropertyChanged(nameof(SplitPosition));
        }
    }
    
    private uint _staticEntityLimit;
    public uint StaticEntityLimit
    {
        get => _staticEntityLimit;
        set
        {
            if (_staticEntityLimit == value)
                return;
    
            _staticEntityLimit = value;
            OnPropertyChanged(nameof(StaticEntityLimit));
        }
    }
    
    private uint _staticPhysEntityLimit;
    public uint StaticPhysEntityLimit
    {
        get => _staticPhysEntityLimit;
        set
        {
            if (_staticPhysEntityLimit == value)
                return;
    
            _staticPhysEntityLimit = value;
            OnPropertyChanged(nameof(StaticPhysEntityLimit));
        }
    }
    
    private uint _intersectEntityLimit;
    public uint IntersectEntityLimit
    {
        get => _intersectEntityLimit;
        set
        {
            if (_intersectEntityLimit == value)
                return;
    
            _intersectEntityLimit = value;
            OnPropertyChanged(nameof(IntersectEntityLimit));
        }
    }
    
    private uint _dynaPhysEntityLimit;
    public uint DynaPhysEntityLimit
    {
        get => _dynaPhysEntityLimit;
        set
        {
            if (_dynaPhysEntityLimit == value)
                return;
    
            _dynaPhysEntityLimit = value;
            OnPropertyChanged(nameof(DynaPhysEntityLimit));
        }
    }
    
    private uint _fenceEntityLimit;
    public uint FenceEntityLimit
    {
        get => _fenceEntityLimit;
        set
        {
            if (_fenceEntityLimit == value)
                return;
    
            _fenceEntityLimit = value;
            OnPropertyChanged(nameof(FenceEntityLimit));
        }
    }
    
    private uint _roadSegmentEntityLimit;
    public uint RoadSegmentEntityLimit
    {
        get => _roadSegmentEntityLimit;
        set
        {
            if (_roadSegmentEntityLimit == value)
                return;
    
            _roadSegmentEntityLimit = value;
            OnPropertyChanged(nameof(RoadSegmentEntityLimit));
        }
    }
    
    private uint _pathSegmentEntityLimit;
    public uint PathSegmentEntityLimit
    {
        get => _pathSegmentEntityLimit;
        set
        {
            if (_pathSegmentEntityLimit == value)
                return;
    
            _pathSegmentEntityLimit = value;
            OnPropertyChanged(nameof(PathSegmentEntityLimit));
        }
    }
    
    private uint _animEntityLimit;
    public uint AnimEntityLimit
    {
        get => _animEntityLimit;
        set
        {
            if (_animEntityLimit == value)
                return;
    
            _animEntityLimit = value;
            OnPropertyChanged(nameof(AnimEntityLimit));
        }
    }
    

    public override byte[] DataBytes
    {
        get
        {
            var data = new List<byte>((int)DataLength);

            data.Add((byte)SplitAxis);
            data.AddRange(BitConverter.GetBytes(SplitPosition));
            data.AddRange(BitConverter.GetBytes(StaticEntityLimit));
            data.AddRange(BitConverter.GetBytes(StaticPhysEntityLimit));
            data.AddRange(BitConverter.GetBytes(IntersectEntityLimit));
            data.AddRange(BitConverter.GetBytes(DynaPhysEntityLimit));
            data.AddRange(BitConverter.GetBytes(FenceEntityLimit));
            data.AddRange(BitConverter.GetBytes(RoadSegmentEntityLimit));
            data.AddRange(BitConverter.GetBytes(PathSegmentEntityLimit));
            data.AddRange(BitConverter.GetBytes(AnimEntityLimit));

            return [.. data];
        }
    }
    public override uint DataLength => sizeof(sbyte) + sizeof(float) + sizeof(uint) + sizeof(uint) + sizeof(uint) + sizeof(uint) + sizeof(uint) + sizeof(uint) + sizeof(uint) + sizeof(uint);

    public SpatialNodeChunk(EndianAwareBinaryReader br) : this((Axis)br.ReadSByte(), br.ReadSingle(), br.ReadUInt32(), br.ReadUInt32(), br.ReadUInt32(), br.ReadUInt32(), br.ReadUInt32(), br.ReadUInt32(), br.ReadUInt32(), br.ReadUInt32())
    {
    }

    public SpatialNodeChunk(Axis splitAxis, float splitPosition, uint staticEntityLimit, uint staticPhysEntityLimit, uint intersectEntityLimit, uint dynaPhysEntityLimit, uint fencyEntityLimit, uint roadSegmentEntityLimit, uint pathSegmentEntityLimit, uint animEntityLimit) : base(ChunkID)
    {
        _splitAxis = splitAxis;
        _splitPosition = splitPosition;
        _staticEntityLimit = staticEntityLimit;
        _staticPhysEntityLimit = staticPhysEntityLimit;
        _intersectEntityLimit = intersectEntityLimit;
        _dynaPhysEntityLimit = dynaPhysEntityLimit;
        _fenceEntityLimit = fencyEntityLimit;
        _roadSegmentEntityLimit = roadSegmentEntityLimit;
        _pathSegmentEntityLimit = pathSegmentEntityLimit;
        _animEntityLimit = animEntityLimit;
    }

    protected override void WriteData(EndianAwareBinaryWriter bw)
    {
        bw.Write((sbyte)SplitAxis);
        bw.Write(SplitPosition);
        bw.Write(StaticEntityLimit);
        bw.Write(StaticPhysEntityLimit);
        bw.Write(IntersectEntityLimit);
        bw.Write(DynaPhysEntityLimit);
        bw.Write(FenceEntityLimit);
        bw.Write(RoadSegmentEntityLimit);
        bw.Write(PathSegmentEntityLimit);
        bw.Write(AnimEntityLimit);
    }

    protected override Chunk CloneSelf() => new SpatialNodeChunk(SplitAxis, SplitPosition, StaticEntityLimit, StaticPhysEntityLimit, IntersectEntityLimit, DynaPhysEntityLimit, FenceEntityLimit, RoadSegmentEntityLimit, PathSegmentEntityLimit, AnimEntityLimit);
}
