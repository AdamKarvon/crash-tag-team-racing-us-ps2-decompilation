using NetP3DLib.IO;
using NetP3DLib.P3D.Attributes;
using NetP3DLib.P3D.Enums;
using NetP3DLib.P3D.Extensions;
using System;
using System.Collections.Generic;
using System.Numerics;

namespace NetP3DLib.P3D.Chunks;

[ChunkAttributes(ChunkID)]
public class WalkerCameraDataChunk : Chunk
{
    public const ChunkIdentifier ChunkID = ChunkIdentifier.Walker_Camera_Data;

    private uint _index;
    public uint Index
    {
        get => _index;
        set
        {
            if (_index == value)
                return;
    
            _index = value;
            OnPropertyChanged(nameof(Index));
        }
    }
    
    private float _minMagnitude;
    public float MinMagnitude
    {
        get => _minMagnitude;
        set
        {
            if (_minMagnitude == value)
                return;
    
            _minMagnitude = value;
            OnPropertyChanged(nameof(MinMagnitude));
        }
    }
    
    private float _maxMagnitude;
    public float MaxMagnitude
    {
        get => _maxMagnitude;
        set
        {
            if (_maxMagnitude == value)
                return;
    
            _maxMagnitude = value;
            OnPropertyChanged(nameof(MaxMagnitude));
        }
    }
    
    private float _elevation;
    public float Elevation
    {
        get => _elevation;
        set
        {
            if (_elevation == value)
                return;
    
            _elevation = value;
            OnPropertyChanged(nameof(Elevation));
        }
    }
    
    private Vector3 _targetOffset;
    public Vector3 TargetOffset
    {
        get => _targetOffset;
        set
        {
            if (_targetOffset == value)
                return;
    
            _targetOffset = value;
            OnPropertyChanged(nameof(TargetOffset));
        }
    }
    

    public override byte[] DataBytes
    {
        get
        {
            var data = new List<byte>((int)DataLength);

            data.AddRange(BitConverter.GetBytes(Index));
            data.AddRange(BitConverter.GetBytes(MinMagnitude));
            data.AddRange(BitConverter.GetBytes(MaxMagnitude));
            data.AddRange(BitConverter.GetBytes(Elevation));
            data.AddRange(BinaryExtensions.GetBytes(TargetOffset));

            return [.. data];
        }
    }
    public override uint DataLength => sizeof(uint) + sizeof(float) + sizeof(float) + sizeof(float) + sizeof(float) * 3;

    public WalkerCameraDataChunk(EndianAwareBinaryReader br) : this(br.ReadUInt32(), br.ReadSingle(), br.ReadSingle(), br.ReadSingle(), br.ReadVector3())
    {
    }

    public WalkerCameraDataChunk(uint index, float minMagnitude, float maxMagnitude, float elevation, Vector3 targetOffset) : base(ChunkID)
    {
        _index = index;
        _minMagnitude = minMagnitude;
        _maxMagnitude = maxMagnitude;
        _elevation = elevation;
        _targetOffset = targetOffset;
    }

    protected override void WriteData(EndianAwareBinaryWriter bw)
    {
        bw.Write(Index);
        bw.Write(MinMagnitude);
        bw.Write(MaxMagnitude);
        bw.Write(Elevation);
        bw.Write(TargetOffset);
    }

    protected override Chunk CloneSelf() => new WalkerCameraDataChunk(Index, MinMagnitude, MaxMagnitude, Elevation, TargetOffset);
}
