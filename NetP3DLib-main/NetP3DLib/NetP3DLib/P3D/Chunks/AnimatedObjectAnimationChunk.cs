using NetP3DLib.IO;
using NetP3DLib.P3D.Attributes;
using NetP3DLib.P3D.Enums;
using NetP3DLib.P3D.Extensions;
using System;
using System.Collections.Generic;
using System.ComponentModel;

namespace NetP3DLib.P3D.Chunks;

[ChunkAttributes(ChunkID)]
public class AnimatedObjectAnimationChunk : NamedChunk
{
    public const ChunkIdentifier ChunkID = ChunkIdentifier.Animated_Object_Animation;

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

    public uint NumOldFrameControllers => GetChildCount(ChunkIdentifier.Old_Frame_Controller);

    public override byte[] DataBytes
    {
        get
        {
            var data = new List<byte>((int)DataLength);

            data.AddRange(BitConverter.GetBytes(Version));
            data.AddRange(BinaryExtensions.GetP3DStringBytes(Name));
            data.AddRange(BitConverter.GetBytes(FrameRate));
            data.AddRange(BitConverter.GetBytes(NumOldFrameControllers));

            return [.. data];
        }
    }
    public override uint DataLength => sizeof(uint) + BinaryExtensions.GetP3DStringLength(Name) + sizeof(float) + sizeof(uint);

    [System.Diagnostics.CodeAnalysis.SuppressMessage("Style", "IDE0059:Unnecessary assignment of a value", Justification = "We want to read the value to progress the BinaryReader, but not set the value anywhere because it's calculated dynamically.")]
    public AnimatedObjectAnimationChunk(EndianAwareBinaryReader br) : this(br.ReadUInt32(), br.ReadP3DString(), br.ReadSingle())
    {
        var numOldFrameControllers = br.ReadUInt32();
    }

    public AnimatedObjectAnimationChunk(uint version, string name, float frameRate) : base(ChunkID, name)
    {
        _version = version;
        _frameRate = frameRate;
    }

    protected override void WriteData(EndianAwareBinaryWriter bw)
    {
        bw.Write(Version);
        bw.WriteP3DString(Name);
        bw.Write(FrameRate);
        bw.Write(NumOldFrameControllers);
    }

    protected override Chunk CloneSelf() => new AnimatedObjectAnimationChunk(Version, Name, FrameRate);
}
