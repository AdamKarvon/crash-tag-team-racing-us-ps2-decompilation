using NetP3DLib.IO;
using NetP3DLib.P3D.Attributes;
using NetP3DLib.P3D.Enums;
using NetP3DLib.P3D.Extensions;
using System;
using System.Collections.Generic;
using System.ComponentModel;

namespace NetP3DLib.P3D.Chunks;

[ChunkAttributes(ChunkID)]
public class ParticleSystemFactoryChunk : NamedChunk
{
    public const ChunkIdentifier ChunkID = ChunkIdentifier.Particle_System_Factory;

    private uint _version;
    [DefaultValue(0)]
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
    
    private uint _numAnimFrames;
    public uint NumAnimFrames
    {
        get => _numAnimFrames;
        set
        {
            if (_numAnimFrames == value)
                return;
    
            _numAnimFrames = value;
            OnPropertyChanged(nameof(NumAnimFrames));
        }
    }
    
    private uint _numOLFrames;
    public uint NumOLFrames
    {
        get => _numOLFrames;
        set
        {
            if (_numOLFrames == value)
                return;
    
            _numOLFrames = value;
            OnPropertyChanged(nameof(NumOLFrames));
        }
    }
    
    private ushort _cycleAnim;
    public ushort CycleAnim
    {
        get => _cycleAnim;
        set
        {
            if (_cycleAnim == value)
                return;
    
            _cycleAnim = value;
            OnPropertyChanged(nameof(CycleAnim));
        }
    }
    
    private ushort _enableSorting;
    public ushort EnableSorting
    {
        get => _enableSorting;
        set
        {
            if (_enableSorting == value)
                return;
    
            _enableSorting = value;
            OnPropertyChanged(nameof(EnableSorting));
        }
    }
    
    public uint NumEmitters => GetChildCount(ChunkIdentifier.Old_Sprite_Emitter);

    public override byte[] DataBytes
    {
        get
        {
            var data = new List<byte>((int)DataLength);

            data.AddRange(BitConverter.GetBytes(Version));
            data.AddRange(BinaryExtensions.GetP3DStringBytes(Name));
            data.AddRange(BitConverter.GetBytes(FrameRate));
            data.AddRange(BitConverter.GetBytes(NumAnimFrames));
            data.AddRange(BitConverter.GetBytes(NumOLFrames));
            data.AddRange(BitConverter.GetBytes(CycleAnim));
            data.AddRange(BitConverter.GetBytes(EnableSorting));
            data.AddRange(BitConverter.GetBytes(NumEmitters));

            return [.. data];
        }
    }
    public override uint DataLength => sizeof(uint) + BinaryExtensions.GetP3DStringLength(Name) + sizeof(float) + sizeof(uint) + sizeof(uint) + sizeof(ushort) + sizeof(ushort) + sizeof(uint);

    [System.Diagnostics.CodeAnalysis.SuppressMessage("Style", "IDE0059:Unnecessary assignment of a value", Justification = "We want to read the value to progress the BinaryReader, but not set the value anywhere because it's calculated dynamically.")]
    public ParticleSystemFactoryChunk(EndianAwareBinaryReader br) : this(br.ReadUInt32(), br.ReadP3DString(), br.ReadSingle(), br.ReadUInt32(), br.ReadUInt32(), br.ReadUInt16(), br.ReadUInt16())
    {
        var numEmitters = br.ReadUInt32();
    }

    public ParticleSystemFactoryChunk(uint version, string name, float frameRate, uint numAnimFrames, uint numOLFrames, ushort cycleAnim, ushort enableSorting) : base(ChunkID, name)
    {
        _version = version;
        _frameRate = frameRate;
        _numAnimFrames = numAnimFrames;
        _numOLFrames = numOLFrames;
        _cycleAnim = cycleAnim;
        _enableSorting = enableSorting;
    }

    protected override void WriteData(EndianAwareBinaryWriter bw)
    {
        bw.Write(Version);
        bw.WriteP3DString(Name);
        bw.Write(FrameRate);
        bw.Write(NumAnimFrames);
        bw.Write(NumOLFrames);
        bw.Write(CycleAnim);
        bw.Write(EnableSorting);
        bw.Write(NumEmitters);
    }

    protected override Chunk CloneSelf() => new ParticleSystemFactoryChunk(Version, Name, FrameRate, NumAnimFrames, NumOLFrames, CycleAnim, EnableSorting);
}
