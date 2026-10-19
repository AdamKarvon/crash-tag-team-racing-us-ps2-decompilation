using NetP3DLib.IO;
using NetP3DLib.P3D.Attributes;
using NetP3DLib.P3D.Enums;
using NetP3DLib.P3D.Extensions;
using System;
using System.Collections.Generic;
using System.ComponentModel;
using System.Numerics;

namespace NetP3DLib.P3D.Chunks;

[ChunkAttributes(ChunkID)]
public class CameraChunk : NamedChunk
{
    public const ChunkIdentifier ChunkID = ChunkIdentifier.Camera;

    private uint _version;
    [DefaultValue(2)]
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
    
    private float _fOV;
    public float FOV
    {
        get => _fOV;
        set
        {
            if (_fOV == value)
                return;
    
            _fOV = value;
            OnPropertyChanged(nameof(FOV));
        }
    }
    
    private float _aspectRatio;
    public float AspectRatio
    {
        get => _aspectRatio;
        set
        {
            if (_aspectRatio == value)
                return;
    
            _aspectRatio = value;
            OnPropertyChanged(nameof(AspectRatio));
        }
    }
    
    private float _nearClip;
    public float NearClip
    {
        get => _nearClip;
        set
        {
            if (_nearClip == value)
                return;
    
            _nearClip = value;
            OnPropertyChanged(nameof(NearClip));
        }
    }
    
    private float _farClip;
    public float FarClip
    {
        get => _farClip;
        set
        {
            if (_farClip == value)
                return;
    
            _farClip = value;
            OnPropertyChanged(nameof(FarClip));
        }
    }
    
    private Vector3 _position;
    public Vector3 Position
    {
        get => _position;
        set
        {
            if (_position == value)
                return;
    
            _position = value;
            OnPropertyChanged(nameof(Position));
        }
    }
    
    private Vector3 _look;
    public Vector3 Look
    {
        get => _look;
        set
        {
            if (_look == value)
                return;
    
            _look = value;
            OnPropertyChanged(nameof(Look));
        }
    }
    
    private Vector3 _up;
    public Vector3 Up
    {
        get => _up;
        set
        {
            if (_up == value)
                return;
    
            _up = value;
            OnPropertyChanged(nameof(Up));
        }
    }
    

    public override byte[] DataBytes
    {
        get
        {
            var data = new List<byte>((int)DataLength);

            data.AddRange(BinaryExtensions.GetP3DStringBytes(Name));
            data.AddRange(BitConverter.GetBytes(Version));
            data.AddRange(BitConverter.GetBytes(FOV));
            data.AddRange(BitConverter.GetBytes(AspectRatio));
            data.AddRange(BitConverter.GetBytes(NearClip));
            data.AddRange(BitConverter.GetBytes(FarClip));
            data.AddRange(BinaryExtensions.GetBytes(Position));
            data.AddRange(BinaryExtensions.GetBytes(Look));
            data.AddRange(BinaryExtensions.GetBytes(Up));

            return [.. data];
        }
    }
    public override uint DataLength => BinaryExtensions.GetP3DStringLength(Name) + sizeof(uint) + sizeof(float) + sizeof(float) + sizeof(float) + sizeof(float) + sizeof(float) * 3 + sizeof(float) * 3 + sizeof(float) * 3;

    public CameraChunk(EndianAwareBinaryReader br) : this(br.ReadP3DString(), br.ReadUInt32(), br.ReadSingle(), br.ReadSingle(), br.ReadSingle(), br.ReadSingle(), br.ReadVector3(), br.ReadVector3(), br.ReadVector3())
    {
    }

    public CameraChunk(string name, uint version, float fov, float aspectRatio, float nearClip, float farClip, Vector3 position, Vector3 look, Vector3 up) : base(ChunkID, name)
    {
        _version = version;
        _fOV = fov;
        _aspectRatio = aspectRatio;
        _nearClip = nearClip;
        _farClip = farClip;
        _position = position;
        _look = look;
        _up = up;
    }

    protected override void WriteData(EndianAwareBinaryWriter bw)
    {
        bw.WriteP3DString(Name);
        bw.Write(Version);
        bw.Write(FOV);
        bw.Write(AspectRatio);
        bw.Write(NearClip);
        bw.Write(FarClip);
        bw.Write(Position);
        bw.Write(Look);
        bw.Write(Up);
    }

    protected override Chunk CloneSelf() => new CameraChunk(Name, Version, FOV, AspectRatio, NearClip, FarClip, Position, Look, Up);
}
