using NetP3DLib.IO;
using NetP3DLib.P3D.Attributes;
using NetP3DLib.P3D.Enums;
using NetP3DLib.P3D.Extensions;
using System;
using System.Collections.Generic;
using System.Numerics;

namespace NetP3DLib.P3D.Chunks;

[ChunkAttributes(ChunkID)]
public class FollowCameraDataChunk : Chunk
{
    public const ChunkIdentifier ChunkID = ChunkIdentifier.Follow_Camera_Data;

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
    
    private float _rotation;
    public float Rotation
    {
        get => _rotation;
        set
        {
            if (_rotation == value)
                return;
    
            _rotation = value;
            OnPropertyChanged(nameof(Rotation));
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
    
    private float _magnitude;
    public float Magnitude
    {
        get => _magnitude;
        set
        {
            if (_magnitude == value)
                return;
    
            _magnitude = value;
            OnPropertyChanged(nameof(Magnitude));
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
            data.AddRange(BitConverter.GetBytes(Rotation));
            data.AddRange(BitConverter.GetBytes(Elevation));
            data.AddRange(BitConverter.GetBytes(Magnitude));
            data.AddRange(BinaryExtensions.GetBytes(TargetOffset));

            return [.. data];
        }
    }
    public override uint DataLength => sizeof(uint) + sizeof(float) + sizeof(float) + sizeof(float) + sizeof(float) * 3;

    public FollowCameraDataChunk(EndianAwareBinaryReader br) : this(br.ReadUInt32(), br.ReadSingle(), br.ReadSingle(), br.ReadSingle(), br.ReadVector3())
    {
    }

    public FollowCameraDataChunk(uint index, float rotation, float elevation, float magnitude, Vector3 targetOffset) : base(ChunkID)
    {
        _index = index;
        _rotation = rotation;
        _elevation = elevation;
        _magnitude = magnitude;
        _targetOffset = targetOffset;
    }

    protected override void WriteData(EndianAwareBinaryWriter bw)
    {
        bw.Write(Index);
        bw.Write(Rotation);
        bw.Write(Elevation);
        bw.Write(Magnitude);
        bw.Write(TargetOffset);
    }

    protected override Chunk CloneSelf() => new FollowCameraDataChunk(Index, Rotation, Elevation, Magnitude, TargetOffset);
}
