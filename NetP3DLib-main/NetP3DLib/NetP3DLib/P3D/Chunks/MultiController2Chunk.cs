using NetP3DLib.IO;
using NetP3DLib.P3D.Attributes;
using NetP3DLib.P3D.Enums;
using NetP3DLib.P3D.Exceptions;
using NetP3DLib.P3D.Extensions;
using NetP3DLib.P3D.Types;
using System;
using System.Collections.Generic;
using System.ComponentModel;

namespace NetP3DLib.P3D.Chunks;

[ChunkAttributes(ChunkID)]
public class MultiController2Chunk : NamedChunk
{
    public const ChunkIdentifier ChunkID = ChunkIdentifier.Multi_Controller_2;

    private uint _version;
    [DefaultValue(1)]
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
    
    private readonly FourCC _cycleMode;
    [MaxLength(4)]
    public string CycleMode
    {
        get => _cycleMode?.Value ?? string.Empty;
        set => _cycleMode.Value = value;
    }
    
    private uint _numCycles;
    public uint NumCycles
    {
        get => _numCycles;
        set
        {
            if (_numCycles == value)
                return;
    
            _numCycles = value;
            OnPropertyChanged(nameof(NumCycles));
        }
    }
    
    private uint _infiniteCycle;
    public uint InfiniteCycle
    {
        get => _infiniteCycle;
        set
        {
            if (_infiniteCycle == value)
                return;
    
            _infiniteCycle = value;
            OnPropertyChanged(nameof(InfiniteCycle));
        }
    }
    
    private float _numFrames;
    public float NumFrames
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
    
    public uint NumTracks => GetChildCount(ChunkIdentifier.Multi_Controller_Track);

    public override byte[] DataBytes
    {
        get
        {
            var data = new List<byte>((int)DataLength);

            data.AddRange(BitConverter.GetBytes(Version));
            data.AddRange(BinaryExtensions.GetP3DStringBytes(Name));
            data.AddRange(BinaryExtensions.GetFourCCBytes(CycleMode));
            data.AddRange(BitConverter.GetBytes(NumCycles));
            data.AddRange(BitConverter.GetBytes(InfiniteCycle));
            data.AddRange(BitConverter.GetBytes(NumFrames));
            data.AddRange(BitConverter.GetBytes(FrameRate));
            data.AddRange(BitConverter.GetBytes(NumTracks));

            return [.. data];
        }
    }
    public override uint DataLength => sizeof(uint) + BinaryExtensions.GetP3DStringLength(Name) + 4 + sizeof(uint) + sizeof(uint) + sizeof(float) + sizeof(float) + sizeof(uint);

    [System.Diagnostics.CodeAnalysis.SuppressMessage("Style", "IDE0059:Unnecessary assignment of a value", Justification = "We want to read the value to progress the BinaryReader, but not set the value anywhere because it's calculated dynamically.")]
    public MultiController2Chunk(EndianAwareBinaryReader br) : this(br.ReadUInt32(), br.ReadP3DString(), br.ReadFourCC(), br.ReadUInt32(), br.ReadUInt32(), br.ReadSingle(), br.ReadSingle())
    {
        var numTracks = br.ReadUInt32();
    }

    public MultiController2Chunk(uint version, string name, string cycleMode, uint numCycles, uint infiniteCycle, float numFrames, float frameRate) : base(ChunkID, name)
    {
        _version = version;
        _cycleMode = new(this, cycleMode, nameof(CycleMode));
        _numCycles = numCycles;
        _infiniteCycle = infiniteCycle;
        _numFrames = numFrames;
        _frameRate = frameRate;
    }

    public override IEnumerable<InvalidP3DException> ValidateChunk()
    {
        foreach (var error in base.ValidateChunk())
            yield return error;

        if (!CycleMode.IsValidFourCC())
            yield return new InvalidP3DFourCCException(this, nameof(CycleMode), CycleMode);
    }

    protected override void WriteData(EndianAwareBinaryWriter bw)
    {
        bw.Write(Version);
        bw.WriteP3DString(Name);
        bw.WriteFourCC(CycleMode);
        bw.Write(NumCycles);
        bw.Write(InfiniteCycle);
        bw.Write(NumFrames);
        bw.Write(FrameRate);
        bw.Write(NumTracks);
    }

    protected override Chunk CloneSelf() => new MultiController2Chunk(Version, Name, CycleMode, NumCycles, InfiniteCycle, NumFrames, FrameRate);
}
