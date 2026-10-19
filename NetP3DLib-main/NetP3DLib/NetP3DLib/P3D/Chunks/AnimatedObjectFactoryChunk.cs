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
public class AnimatedObjectFactoryChunk : NamedChunk
{
    public const ChunkIdentifier ChunkID = ChunkIdentifier.Animated_Object_Factory;

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
    
    private readonly P3DString _baseAnimation;
    public string BaseAnimation
    {
        get => _baseAnimation?.Value ?? string.Empty;
        set => _baseAnimation.Value = value;
    }
    
    private uint _numAnimations;
    public uint NumAnimations
    {
        get => _numAnimations;
        set
        {
            if (_numAnimations == value)
                return;
    
            _numAnimations = value;
            OnPropertyChanged(nameof(NumAnimations));
        }
    }
    

    public override byte[] DataBytes
    {
        get
        {
            var data = new List<byte>((int)DataLength);

            data.AddRange(BitConverter.GetBytes(Version));
            data.AddRange(BinaryExtensions.GetP3DStringBytes(Name));
            data.AddRange(BinaryExtensions.GetP3DStringBytes(BaseAnimation));
            data.AddRange(BitConverter.GetBytes(NumAnimations));

            return [.. data];
        }
    }
    public override uint DataLength => sizeof(uint) + BinaryExtensions.GetP3DStringLength(Name) + BinaryExtensions.GetP3DStringLength(BaseAnimation) + sizeof(uint);

    public AnimatedObjectFactoryChunk(EndianAwareBinaryReader br) : this(br.ReadUInt32(), br.ReadP3DString(), br.ReadP3DString(), br.ReadUInt32())
    {
    }

    public AnimatedObjectFactoryChunk(uint version, string name, string baseAnimation, uint numAnimations) : base(ChunkID, name)
    {
        _version = version;
        _baseAnimation = new(this, baseAnimation, nameof(BaseAnimation));
        _numAnimations = numAnimations;
    }

    public override IEnumerable<InvalidP3DException> ValidateChunk()
    {
        foreach (var error in base.ValidateChunk())
            yield return error;

        if (!BaseAnimation.IsValidP3DString())
            yield return new InvalidP3DStringException(this, nameof(BaseAnimation), BaseAnimation);
    }

    protected override void WriteData(EndianAwareBinaryWriter bw)
    {
        bw.Write(Version);
        bw.WriteP3DString(Name);
        bw.WriteP3DString(BaseAnimation);
        bw.Write(NumAnimations);
    }

    protected override Chunk CloneSelf() => new AnimatedObjectFactoryChunk(Version, Name, BaseAnimation, NumAnimations);
}
