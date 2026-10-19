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
public class FrameControllerChunk : NamedChunk
{
    public const ChunkIdentifier ChunkID = ChunkIdentifier.Frame_Controller;

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
    
    private readonly FourCC _type;
    [MaxLength(4)]
    public string Type
    {
        get => _type?.Value ?? string.Empty;
        set => _type.Value = value;
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
    
    private readonly P3DString _hierarchyName;
    public string HierarchyName
    {
        get => _hierarchyName?.Value ?? string.Empty;
        set => _hierarchyName.Value = value;
    }
    private readonly P3DString _animationName;
    public string AnimationName
    {
        get => _animationName?.Value ?? string.Empty;
        set => _animationName.Value = value;
    }

    public override byte[] DataBytes
    {
        get
        {
            var data = new List<byte>((int)DataLength);

            data.AddRange(BitConverter.GetBytes(Version));
            data.AddRange(BinaryExtensions.GetP3DStringBytes(Name));
            data.AddRange(BinaryExtensions.GetFourCCBytes(Type));
            data.AddRange(BinaryExtensions.GetFourCCBytes(CycleMode));
            data.AddRange(BitConverter.GetBytes(NumCycles));
            data.AddRange(BitConverter.GetBytes(InfiniteCycle));
            data.AddRange(BinaryExtensions.GetP3DStringBytes(HierarchyName));
            data.AddRange(BinaryExtensions.GetP3DStringBytes(AnimationName));

            return [.. data];
        }
    }
    public override uint DataLength => sizeof(uint) + BinaryExtensions.GetP3DStringLength(Name) + 4 + 4 + sizeof(uint) + sizeof(uint) + BinaryExtensions.GetP3DStringLength(HierarchyName) + BinaryExtensions.GetP3DStringLength(AnimationName);

    public FrameControllerChunk(EndianAwareBinaryReader br) : this(br.ReadUInt32(), br.ReadP3DString(), br.ReadFourCC(), br.ReadFourCC(), br.ReadUInt32(), br.ReadUInt32(), br.ReadP3DString(), br.ReadP3DString())
    {
    }

    public FrameControllerChunk(uint version, string name, string type, string cycleMode, uint numCycles, uint infiniteCycle, string hierarchyName, string animationName) : base(ChunkID, name)
    {
        _version = version;
        _type = new(this, type, nameof(Type));
        _cycleMode = new(this, cycleMode, nameof(CycleMode));
        _numCycles = numCycles;
        _infiniteCycle = infiniteCycle;
        _hierarchyName = new(this, hierarchyName, nameof(HierarchyName));
        _animationName = new(this, animationName, nameof(AnimationName));
    }

    public override IEnumerable<InvalidP3DException> ValidateChunk()
    {
        foreach (var error in base.ValidateChunk())
            yield return error;

        if (!Type.IsValidFourCC())
            yield return new InvalidP3DFourCCException(this, nameof(Type), Type);

        if (!CycleMode.IsValidFourCC())
            yield return new InvalidP3DFourCCException(this, nameof(CycleMode), CycleMode);

        if (!HierarchyName.IsValidP3DString())
            yield return new InvalidP3DStringException(this, nameof(HierarchyName), HierarchyName);

        if (!AnimationName.IsValidP3DString())
            yield return new InvalidP3DStringException(this, nameof(AnimationName), AnimationName);
    }

    protected override void WriteData(EndianAwareBinaryWriter bw)
    {
        bw.Write(Version);
        bw.WriteP3DString(Name);
        bw.WriteFourCC(Type);
        bw.WriteFourCC(CycleMode);
        bw.Write(NumCycles);
        bw.Write(InfiniteCycle);
        bw.WriteP3DString(HierarchyName);
        bw.WriteP3DString(AnimationName);
    }

    protected override Chunk CloneSelf() => new FrameControllerChunk(Version, Name, Type, CycleMode, NumCycles, InfiniteCycle, HierarchyName, AnimationName);
}
