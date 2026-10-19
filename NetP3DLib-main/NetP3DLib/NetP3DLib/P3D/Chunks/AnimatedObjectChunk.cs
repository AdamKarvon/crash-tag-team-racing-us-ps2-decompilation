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
public class AnimatedObjectChunk : NamedChunk
{
    public const ChunkIdentifier ChunkID = ChunkIdentifier.Animated_Object;

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
    
    private readonly P3DString _factoryName;
    public string FactoryName
    {
        get => _factoryName?.Value ?? string.Empty;
        set => _factoryName.Value = value;
    }
    
    private uint _startingAnimation;
    public uint StartingAnimation
    {
        get => _startingAnimation;
        set
        {
            if (_startingAnimation == value)
                return;
    
            _startingAnimation = value;
            OnPropertyChanged(nameof(StartingAnimation));
        }
    }
    

    public override byte[] DataBytes
    {
        get
        {
            var data = new List<byte>((int)DataLength);

            data.AddRange(BitConverter.GetBytes(Version));
            data.AddRange(BinaryExtensions.GetP3DStringBytes(Name));
            data.AddRange(BinaryExtensions.GetP3DStringBytes(FactoryName));
            data.AddRange(BitConverter.GetBytes(StartingAnimation));

            return [.. data];
        }
    }
    public override uint DataLength => sizeof(uint) + BinaryExtensions.GetP3DStringLength(Name) + BinaryExtensions.GetP3DStringLength(FactoryName) + sizeof(uint);

    public AnimatedObjectChunk(EndianAwareBinaryReader br) : this(br.ReadUInt32(), br.ReadP3DString(), br.ReadP3DString(), br.ReadUInt32())
    {
    }

    public AnimatedObjectChunk(uint version, string name, string factoryName, uint startingAnimation) : base(ChunkID, name)
    {
        _version = version;
        _factoryName = new(this, factoryName, nameof(FactoryName));
        _startingAnimation = startingAnimation;
    }

    public override IEnumerable<InvalidP3DException> ValidateChunk()
    {
        foreach (var error in base.ValidateChunk())
            yield return error;

        if (!FactoryName.IsValidP3DString())
            yield return new InvalidP3DStringException(this, nameof(FactoryName), FactoryName);
    }

    protected override void WriteData(EndianAwareBinaryWriter bw)
    {
        bw.Write(Version);
        bw.WriteP3DString(Name);
        bw.WriteP3DString(FactoryName);
        bw.Write(StartingAnimation);
    }

    protected override Chunk CloneSelf() => new AnimatedObjectChunk(Version, Name, FactoryName, StartingAnimation);
}
