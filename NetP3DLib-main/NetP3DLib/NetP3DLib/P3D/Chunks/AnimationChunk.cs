using NetP3DLib.IO;
using NetP3DLib.P3D.Attributes;
using NetP3DLib.P3D.Enums;
using NetP3DLib.P3D.Exceptions;
using NetP3DLib.P3D.Extensions;
using System;
using System.Collections.Generic;
using System.ComponentModel;

namespace NetP3DLib.P3D.Chunks;

[ChunkAttributes(ChunkID)]
public class AnimationChunk : NamedChunk
{
    public const ChunkIdentifier ChunkID = ChunkIdentifier.Animation;
    public enum Platform
    {
        PC,
        PS2,
        XBOX,
        GC,
    }

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
    
    private AnimationType _animationType;
    public AnimationType AnimationType
    {
        get => _animationType;
        set
        {
            if (_animationType == value)
                return;
    
            _animationType = value;
            OnPropertyChanged(nameof(AnimationType));
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
    
    private uint _cyclic;
    public bool Cyclic
    {
        get => _cyclic == 1;
        set
        {
            if (Cyclic == value)
                return;

            _cyclic = value ? 1u : 0u;
            OnPropertyChanged(nameof(Cyclic));
        }
    }

    public override byte[] DataBytes
    {
        get
        {
            var data = new List<byte>((int)DataLength);

            data.AddRange(BitConverter.GetBytes(Version));
            data.AddRange(BinaryExtensions.GetP3DStringBytes(Name));
            data.AddRange(BitConverter.GetBytes((uint)AnimationType));
            data.AddRange(BitConverter.GetBytes(NumFrames));
            data.AddRange(BitConverter.GetBytes(FrameRate));
            data.AddRange(BitConverter.GetBytes(_cyclic));

            return [.. data];
        }
    }
    public override uint DataLength => sizeof(uint) + BinaryExtensions.GetP3DStringLength(Name) + 4 + sizeof(float) + sizeof(float) + sizeof(uint);

    public AnimationChunk(EndianAwareBinaryReader br) : this(br.ReadUInt32(), br.ReadP3DString(), (AnimationType)br.ReadUInt32(), br.ReadSingle(), br.ReadSingle(), br.ReadUInt32())
    {
    }

    public AnimationChunk(uint version, string name, AnimationType animationType, float numFrames, float frameRate, bool cyclic) : this(version, name, animationType, numFrames, frameRate, cyclic ? 1u : 0u)
    {
    }

    public AnimationChunk(uint version, string name, AnimationType animationType, float numFrames, float frameRate, uint cyclic) : base(ChunkID, name)
    {
        _version = version;
        _animationType = animationType;
        _numFrames = numFrames;
        _frameRate = frameRate;
        _cyclic = cyclic;
    }

    public override IEnumerable<InvalidP3DException> ValidateChunk()
    {
        foreach (var error in base.ValidateChunk())
            yield return error;

        var animationSizeCount = GetChildCount(ChunkIdentifier.Animation_Size);
        if (animationSizeCount > 1)
            yield return new InvalidP3DException(this, $"There can only be one {nameof(AnimationSizeChunk)} per {nameof(AnimationChunk)}.");
        var animationSize = animationSizeCount == 1 ? GetFirstChunkOfType<AnimationSizeChunk>() : null;

        var animationGroupListCount = GetChildCount(ChunkIdentifier.Animation_Group_List);
        if (animationGroupListCount > 1)
            yield return new InvalidP3DException(this, $"There can only be one {nameof(AnimationGroupListChunk)} per {nameof(AnimationChunk)}.");
        var animationGroupList = animationGroupListCount == 1 ? GetFirstChunkOfType<AnimationGroupListChunk>() : null;

        if (animationSize != null && animationGroupList != null && animationGroupList.IndexInParent < animationSize.IndexInParent)
            yield return new InvalidP3DException(this, $"The {nameof(AnimationSizeChunk)} must be before the {nameof(AnimationGroupListChunk)}.");
    }

    protected override void WriteData(EndianAwareBinaryWriter bw)
    {
        bw.Write(Version);
        bw.WriteP3DString(Name);
        bw.Write((uint)AnimationType);
        bw.Write(NumFrames);
        bw.Write(FrameRate);
        bw.Write(_cyclic);
    }

    protected override Chunk CloneSelf() => new AnimationChunk(Version, Name, AnimationType, NumFrames, FrameRate, Cyclic);

    // TODO: Caching
    public uint CalculateMemorySize(Platform platform)
    {
        var animationGroupList = GetLastChunkOfType<AnimationGroupListChunk>();
        if (animationGroupList == null)
            return 0u;

        var total = 0u;
        foreach (var animationGroup in animationGroupList.GetChunksOfType<AnimationGroupChunk>())
            total = animationGroup.CalculateMemorySize(platform, total);

        return total;
    }
}
