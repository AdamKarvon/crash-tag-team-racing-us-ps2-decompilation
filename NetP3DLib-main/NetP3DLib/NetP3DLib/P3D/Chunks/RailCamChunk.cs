using NetP3DLib.IO;
using NetP3DLib.P3D.Attributes;
using NetP3DLib.P3D.Enums;
using NetP3DLib.P3D.Extensions;
using System;
using System.Collections.Generic;
using System.Numerics;

namespace NetP3DLib.P3D.Chunks;

[ChunkAttributes(ChunkID)]
public class RailCamChunk : NamedChunk
{
    public const ChunkIdentifier ChunkID = ChunkIdentifier.Rail_Cam;

    public enum Behaviours : uint
    {
        Distance = 1,
        Projection,
    }

    private Behaviours _behaviour;
    public Behaviours Behaviour
    {
        get => _behaviour;
        set
        {
            if (_behaviour == value)
                return;
    
            _behaviour = value;
            OnPropertyChanged(nameof(Behaviour));
        }
    }
    
    private float _minRadius;
    public float MinRadius
    {
        get => _minRadius;
        set
        {
            if (_minRadius == value)
                return;
    
            _minRadius = value;
            OnPropertyChanged(nameof(MinRadius));
        }
    }
    
    private float _maxRadius;
    public float MaxRadius
    {
        get => _maxRadius;
        set
        {
            if (_maxRadius == value)
                return;
    
            _maxRadius = value;
            OnPropertyChanged(nameof(MaxRadius));
        }
    }
    
    private uint _trackRail;
    public bool TrackRail
    {
        get => _trackRail == 1;
        set
        {
            if (TrackRail == value)
                return;

            _trackRail = value ? 1u : 0u;
            OnPropertyChanged(nameof(TrackRail));
        }
    }
    
    private float _trackDist;
    public float TrackDist
    {
        get => _trackDist;
        set
        {
            if (_trackDist == value)
                return;
    
            _trackDist = value;
            OnPropertyChanged(nameof(TrackDist));
        }
    }
    
    private uint _reverseSense;
    public bool ReverseSense
    {
        get => _reverseSense == 1;
        set
        {
            if (ReverseSense == value)
                return;

            _reverseSense = value ? 1u : 0u;
            OnPropertyChanged(nameof(ReverseSense));
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
    
    private Vector3 _axisPlay;
    public Vector3 AxisPlay
    {
        get => _axisPlay;
        set
        {
            if (_axisPlay == value)
                return;
    
            _axisPlay = value;
            OnPropertyChanged(nameof(AxisPlay));
        }
    }
    
    private float _positionLag;
    public float PositionLag
    {
        get => _positionLag;
        set
        {
            if (_positionLag == value)
                return;
    
            _positionLag = value;
            OnPropertyChanged(nameof(PositionLag));
        }
    }
    
    private float _targetLag;
    public float TargetLag
    {
        get => _targetLag;
        set
        {
            if (_targetLag == value)
                return;
    
            _targetLag = value;
            OnPropertyChanged(nameof(TargetLag));
        }
    }
    

    public override byte[] DataBytes
    {
        get
        {
            var data = new List<byte>((int)DataLength);

            data.AddRange(BinaryExtensions.GetP3DStringBytes(Name));
            data.AddRange(BitConverter.GetBytes((uint)Behaviour));
            data.AddRange(BitConverter.GetBytes(MinRadius));
            data.AddRange(BitConverter.GetBytes(MaxRadius));
            data.AddRange(BitConverter.GetBytes(_trackRail));
            data.AddRange(BitConverter.GetBytes(TrackDist));
            data.AddRange(BitConverter.GetBytes(_reverseSense));
            data.AddRange(BitConverter.GetBytes(FOV));
            data.AddRange(BinaryExtensions.GetBytes(TargetOffset));
            data.AddRange(BinaryExtensions.GetBytes(AxisPlay));
            data.AddRange(BitConverter.GetBytes(PositionLag));
            data.AddRange(BitConverter.GetBytes(TargetLag));

            return [.. data];
        }
    }
    public override uint DataLength => BinaryExtensions.GetP3DStringLength(Name) + sizeof(uint) + sizeof(float) + sizeof(float) + sizeof(uint) + sizeof(float) + sizeof(uint) + sizeof(float) + sizeof(float) * 3 + sizeof(float) * 3 + sizeof(float) + sizeof(float);

    public RailCamChunk(EndianAwareBinaryReader br) : this(br.ReadP3DString(), (Behaviours)br.ReadUInt32(), br.ReadSingle(), br.ReadSingle(), br.ReadUInt32(), br.ReadSingle(), br.ReadUInt32(), br.ReadSingle(), br.ReadVector3(), br.ReadVector3(), br.ReadSingle(), br.ReadSingle())
    {
    }

    public RailCamChunk(string name, Behaviours behaviour, float minRadius, float maxRadius, bool trackRail, float trackDist, bool reverseSense, float fov, Vector3 targetOffset, Vector3 axisPlay, float positionLag, float targetLag) : this(name, behaviour, minRadius, maxRadius, trackRail ? 1u : 0u, trackDist, reverseSense ? 1u : 0u, fov, targetOffset, axisPlay, positionLag, targetLag)
    {
    }

    public RailCamChunk(string name, Behaviours behaviour, float minRadius, float maxRadius, uint trackRail, float trackDist, uint reverseSense, float fov, Vector3 targetOffset, Vector3 axisPlay, float positionLag, float targetLag) : base(ChunkID, name)
    {
        _behaviour = behaviour;
        _minRadius = minRadius;
        _maxRadius = maxRadius;
        _trackRail = trackRail;
        _trackDist = trackDist;
        _reverseSense = reverseSense;
        _fOV = fov;
        _targetOffset = targetOffset;
        _axisPlay = axisPlay;
        _positionLag = positionLag;
        _targetLag = targetLag;
    }

    protected override void WriteData(EndianAwareBinaryWriter bw)
    {
        bw.WriteP3DString(Name);
        bw.Write((uint)Behaviour);
        bw.Write(MinRadius);
        bw.Write(MaxRadius);
        bw.Write(_trackRail);
        bw.Write(TrackDist);
        bw.Write(_reverseSense);
        bw.Write(FOV);
        bw.Write(TargetOffset);
        bw.Write(AxisPlay);
        bw.Write(PositionLag);
        bw.Write(TargetLag);
    }

    protected override Chunk CloneSelf() => new RailCamChunk(Name, Behaviour, MinRadius, MaxRadius, TrackRail, TrackDist, ReverseSense, FOV, TargetOffset, AxisPlay, PositionLag, TargetLag);
}
