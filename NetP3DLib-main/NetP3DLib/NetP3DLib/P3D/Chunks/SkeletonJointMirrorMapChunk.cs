using NetP3DLib.IO;
using NetP3DLib.P3D.Attributes;
using NetP3DLib.P3D.Enums;
using System;
using System.Collections.Generic;

namespace NetP3DLib.P3D.Chunks;

[ChunkAttributes(ChunkID)]
public class SkeletonJointMirrorMapChunk : Chunk
{
    public const ChunkIdentifier ChunkID = ChunkIdentifier.Skeleton_Joint_Mirror_Map;

    private uint _mappedJointIndex;
    public uint MappedJointIndex
    {
        get => _mappedJointIndex;
        set
        {
            if (_mappedJointIndex == value)
                return;
    
            _mappedJointIndex = value;
            OnPropertyChanged(nameof(MappedJointIndex));
        }
    }
    
    private float _xAxisMap;
    public float XAxisMap
    {
        get => _xAxisMap;
        set
        {
            if (_xAxisMap == value)
                return;
    
            _xAxisMap = value;
            OnPropertyChanged(nameof(XAxisMap));
        }
    }
    
    private float _yAxisMap;
    public float YAxisMap
    {
        get => _yAxisMap;
        set
        {
            if (_yAxisMap == value)
                return;
    
            _yAxisMap = value;
            OnPropertyChanged(nameof(YAxisMap));
        }
    }
    
    private float _zAxisMap;
    public float ZAxisMap
    {
        get => _zAxisMap;
        set
        {
            if (_zAxisMap == value)
                return;
    
            _zAxisMap = value;
            OnPropertyChanged(nameof(ZAxisMap));
        }
    }
    

    public override byte[] DataBytes
    {
        get
        {
            var data = new List<byte>((int)DataLength);

            data.AddRange(BitConverter.GetBytes(MappedJointIndex));
            data.AddRange(BitConverter.GetBytes(XAxisMap));
            data.AddRange(BitConverter.GetBytes(YAxisMap));
            data.AddRange(BitConverter.GetBytes(ZAxisMap));

            return [.. data];
        }
    }
    public override uint DataLength => sizeof(uint) + sizeof(float) + sizeof(float) + sizeof(float);

    public SkeletonJointMirrorMapChunk(EndianAwareBinaryReader br) : this(br.ReadUInt32(), br.ReadSingle(), br.ReadSingle(), br.ReadSingle())
    {
    }

    public SkeletonJointMirrorMapChunk(uint mappedJointIndex, float xAxisMap, float yAxisMap, float zAxisMap) : base(ChunkID)
    {
        _mappedJointIndex = mappedJointIndex;
        _xAxisMap = xAxisMap;
        _yAxisMap = yAxisMap;
        _zAxisMap = zAxisMap;
    }

    protected override void WriteData(EndianAwareBinaryWriter bw)
    {
        bw.Write(MappedJointIndex);
        bw.Write(XAxisMap);
        bw.Write(YAxisMap);
        bw.Write(ZAxisMap);
    }

    protected override Chunk CloneSelf() => new SkeletonJointMirrorMapChunk(MappedJointIndex, XAxisMap, YAxisMap, ZAxisMap);
}
