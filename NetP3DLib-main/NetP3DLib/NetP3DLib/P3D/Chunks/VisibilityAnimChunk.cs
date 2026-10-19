using NetP3DLib.IO;
using NetP3DLib.P3D.Attributes;
using NetP3DLib.P3D.Enums;
using NetP3DLib.P3D.Exceptions;
using NetP3DLib.P3D.Extensions;
using NetP3DLib.P3D.Types;
using System;
using System.Collections.Generic;

namespace NetP3DLib.P3D.Chunks;

[ChunkAttributes(ChunkID)]
public class VisibilityAnimChunk : NamedChunk
{
    public const ChunkIdentifier ChunkID = ChunkIdentifier.Visibility_Anim;

    private readonly P3DString _sceneName;
    public string SceneName
    {
        get => _sceneName?.Value ?? string.Empty;
        set => _sceneName.Value = value;
    }
    
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
    
    public uint NumChannels => GetChildCount(ChunkIdentifier.Visibility_Anim_Channel);

    public override byte[] DataBytes
    {
        get
        {
            var data = new List<byte>((int)DataLength);

            data.AddRange(BinaryExtensions.GetP3DStringBytes(Name));
            data.AddRange(BinaryExtensions.GetP3DStringBytes(SceneName));
            data.AddRange(BitConverter.GetBytes(Version));
            data.AddRange(BitConverter.GetBytes(NumFrames));
            data.AddRange(BitConverter.GetBytes(FrameRate));
            data.AddRange(BitConverter.GetBytes(NumChannels));

            return [.. data];
        }
    }
    public override uint DataLength => BinaryExtensions.GetP3DStringLength(Name) + BinaryExtensions.GetP3DStringLength(SceneName) + sizeof(uint) + sizeof(uint) + sizeof(float) + sizeof(uint);

    [System.Diagnostics.CodeAnalysis.SuppressMessage("Style", "IDE0059:Unnecessary assignment of a value", Justification = "We want to read the value to progress the BinaryReader, but not set the value anywhere because it's calculated dynamically.")]
    public VisibilityAnimChunk(EndianAwareBinaryReader br) : this(br.ReadP3DString(), br.ReadP3DString(), br.ReadUInt32(), br.ReadUInt32(), br.ReadSingle())
    {
        var numChannels = br.ReadUInt32();
    }

    public VisibilityAnimChunk(string name, string sceneName, uint version, uint numFrames, float frameRate) : base(ChunkID, name)
    {
        _sceneName = new(this, sceneName, nameof(SceneName));
        _version = version;
        _numFrames = numFrames;
        _frameRate = frameRate;
    }

    public override IEnumerable<InvalidP3DException> ValidateChunk()
    {
        foreach (var error in base.ValidateChunk())
            yield return error;

        if (!SceneName.IsValidP3DString())
            yield return new InvalidP3DStringException(this, nameof(SceneName), SceneName);
    }

    protected override void WriteData(EndianAwareBinaryWriter bw)
    {
        bw.WriteP3DString(Name);
        bw.WriteP3DString(SceneName);
        bw.Write(Version);
        bw.Write(NumFrames);
        bw.Write(FrameRate);
        bw.Write(NumChannels);
    }

    protected override Chunk CloneSelf() => new VisibilityAnimChunk(Name, SceneName, Version, NumFrames, FrameRate);
}
