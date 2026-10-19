using NetP3DLib.IO;
using NetP3DLib.P3D.Attributes;
using NetP3DLib.P3D.Enums;
using System;
using System.Collections.Generic;

namespace NetP3DLib.P3D.Chunks;

[ChunkAttributes(ChunkID)]
public class PhysicsJointChunk : Chunk
{
    public const ChunkIdentifier ChunkID = ChunkIdentifier.Physics_Joint;

    public enum DegreeOfFreedom
    {
        Fixed_0D = 0,
        Hinge_1D = 1,
        BallAndSocket_3D = 3
    }

    private int _index;
    public int Index
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
    
    private float _volume;
    public float Volume
    {
        get => _volume;
        set
        {
            if (_volume == value)
                return;
    
            _volume = value;
            OnPropertyChanged(nameof(Volume));
        }
    }
    
    private float _stiffness;
    public float Stiffness
    {
        get => _stiffness;
        set
        {
            if (_stiffness == value)
                return;
    
            _stiffness = value;
            OnPropertyChanged(nameof(Stiffness));
        }
    }
    
    private float _maxAngle;
    public float MaxAngle
    {
        get => _maxAngle;
        set
        {
            if (_maxAngle == value)
                return;
    
            _maxAngle = value;
            OnPropertyChanged(nameof(MaxAngle));
        }
    }
    
    private float _minAngle;
    public float MinAngle
    {
        get => _minAngle;
        set
        {
            if (_minAngle == value)
                return;
    
            _minAngle = value;
            OnPropertyChanged(nameof(MinAngle));
        }
    }
    
    private DegreeOfFreedom _degreesOfFreedom;
    public DegreeOfFreedom DegreesOfFreedom
    {
        get => _degreesOfFreedom;
        set
        {
            if (_degreesOfFreedom == value)
                return;
    
            _degreesOfFreedom = value;
            OnPropertyChanged(nameof(DegreesOfFreedom));
        }
    }
    

    public override byte[] DataBytes
    {
        get
        {
            var data = new List<byte>((int)DataLength);

            data.AddRange(BitConverter.GetBytes(Index));
            data.AddRange(BitConverter.GetBytes(Volume));
            data.AddRange(BitConverter.GetBytes(Stiffness));
            data.AddRange(BitConverter.GetBytes(MaxAngle));
            data.AddRange(BitConverter.GetBytes(MinAngle));
            data.AddRange(BitConverter.GetBytes((int)DegreesOfFreedom));

            return [.. data];
        }
    }
    public override uint DataLength => sizeof(int) + sizeof(float) + sizeof(float) + sizeof(float) + sizeof(float) + sizeof(int);

    public PhysicsJointChunk(EndianAwareBinaryReader br) : this(br.ReadInt32(), br.ReadSingle(), br.ReadSingle(), br.ReadSingle(), br.ReadSingle(), (DegreeOfFreedom)br.ReadInt32())
    {
    }

    public PhysicsJointChunk(int index, float volume, float stiffness, float maxAngle, float minAngle, DegreeOfFreedom degreesOfFreedom) : base(ChunkID)
    {
        _index = index;
        _volume = volume;
        _stiffness = stiffness;
        _maxAngle = maxAngle;
        _minAngle = minAngle;
        _degreesOfFreedom = degreesOfFreedom;
    }

    protected override void WriteData(EndianAwareBinaryWriter bw)
    {
        bw.Write(Index);
        bw.Write(Volume);
        bw.Write(Stiffness);
        bw.Write(MaxAngle);
        bw.Write(MinAngle);
        bw.Write((int)DegreesOfFreedom);
    }

    protected override Chunk CloneSelf() => new PhysicsJointChunk(Index, Volume, Stiffness, MaxAngle, MinAngle, DegreesOfFreedom);
}
