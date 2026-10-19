using NetP3DLib.IO;
using NetP3DLib.P3D.Attributes;
using NetP3DLib.P3D.Enums;
using NetP3DLib.P3D.Extensions;
using System;
using System.Collections.Generic;
using System.Numerics;

namespace NetP3DLib.P3D.Chunks;

[ChunkAttributes(ChunkID)]
public class ParticleSystem2Chunk : NamedChunk
{
    public const ChunkIdentifier ChunkID = ChunkIdentifier.Particle_System_2;

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

    private float _frameRate;
    public float FrameRate
    {
        get => _frameRate;
        set
        {
            if (_frameRate == value)
                return;

            _frameRate = value;
            OnPropertyChanged(nameof(FrameRate));
        }
    }

    private uint _numFrames;
    public uint NumFrames
    {
        get => _numFrames;
        set
        {
            if (_numFrames == value)
                return;

            _numFrames = value;
            OnPropertyChanged(nameof(NumFrames));
        }
    }

    private uint _isCyclic;
    public uint IsCyclic
    {
        get => _isCyclic;
        set
        {
            if (_isCyclic == value)
                return;

            _isCyclic = value;
            OnPropertyChanged(nameof(IsCyclic));
        }
    }

    private Quaternion _rotation;
    public Quaternion Rotation
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

    private Vector3 _translation;
    public Vector3 Translation
    {
        get => _translation;
        set
        {
            if (_translation == value)
                return;

            _translation = value;
            OnPropertyChanged(nameof(Translation));
        }
    }

    public uint NumEmitters => GetChildCount(ChunkIdentifier.Sprite_Particle_Emitter);

    public override byte[] DataBytes
    {
        get
        {
            var data = new List<byte>((int)DataLength);

            data.AddRange(BitConverter.GetBytes(Version));
            data.AddRange(BinaryExtensions.GetP3DStringBytes(Name));
            data.AddRange(BitConverter.GetBytes(FrameRate));
            data.AddRange(BitConverter.GetBytes(NumFrames));
            data.AddRange(BitConverter.GetBytes(IsCyclic));
            data.AddRange(BinaryExtensions.GetBytes(Rotation));
            data.AddRange(BinaryExtensions.GetBytes(Translation));
            data.AddRange(BitConverter.GetBytes(NumEmitters));

            return [.. data];
        }
    }
    public override uint DataLength => sizeof(uint) + BinaryExtensions.GetP3DStringLength(Name) + sizeof(float) + sizeof(uint) + sizeof(uint) + sizeof(float) * 4 + sizeof(float) * 3 + sizeof(uint);

    [System.Diagnostics.CodeAnalysis.SuppressMessage("Style", "IDE0059:Unnecessary assignment of a value", Justification = "We want to read the value to progress the BinaryReader, but not set the value anywhere because it's calculated dynamically.")]
    public ParticleSystem2Chunk(EndianAwareBinaryReader br) : this(br.ReadUInt32(), br.ReadP3DString(), br.ReadSingle(), br.ReadUInt32(), br.ReadUInt32(), br.ReadQuaternion(), br.ReadVector3())
    {
        var numEmitters = br.ReadUInt32();
    }

    public ParticleSystem2Chunk(uint version, string name, float frameRate, uint numFrames, uint isCyclic, Quaternion rotation, Vector3 translation) : base(ChunkID, name)
    {
        _version = version;
        _frameRate = frameRate;
        _numFrames = numFrames;
        _isCyclic = isCyclic;
        _rotation = rotation;
        _translation = translation;
    }

    protected override void WriteData(EndianAwareBinaryWriter bw)
    {
        bw.Write(Version);
        bw.WriteP3DString(Name);
        bw.Write(FrameRate);
        bw.Write(NumFrames);
        bw.Write(IsCyclic);
        bw.Write(Rotation);
        bw.Write(Translation);
        bw.Write(NumEmitters);
    }

    protected override Chunk CloneSelf() => new ParticleSystem2Chunk(Version, Name, FrameRate, NumFrames, IsCyclic, Rotation, Translation);
}
