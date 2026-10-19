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
public class TextureChunk : NamedChunk
{
    public const ChunkIdentifier ChunkID = ChunkIdentifier.Texture;

    public enum TextureTypes : uint
    {
        RGB,
        Palettized,
        Luminance,
        BumpMap,
        DXT1,
        DXT2,
        DXT3,
        DXT4,
        DXT5,
        IPU,
        Z,
        Linear,
        RenderTarget,
        PS2_4Bit,
        PS2_8Bit,
        PS2_16Bit,
        PS2_32Bit,
        GC_4Bit,
        GC_8Bit,
        GC_16Bit,
        GC_32Bit,
        GC_DXT1,
    }

    public enum UsageHints : uint
    {
        Static,
        Dynamic,
        NoCache,
    }

    private uint _version;
    [DefaultValue(14000)]
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
    
    private uint _width;
    public uint Width
    {
        get => _width;
        set
        {
            if (_width == value)
                return;
    
            _width = value;
            OnPropertyChanged(nameof(Width));
        }
    }
    
    private uint _height;
    public uint Height
    {
        get => _height;
        set
        {
            if (_height == value)
                return;
    
            _height = value;
            OnPropertyChanged(nameof(Height));
        }
    }
    
    private uint _bpp;
    public uint Bpp
    {
        get => _bpp;
        set
        {
            if (_bpp == value)
                return;
    
            _bpp = value;
            OnPropertyChanged(nameof(Bpp));
        }
    }
    
    private uint _alphaDepth;
    public uint AlphaDepth
    {
        get => _alphaDepth;
        set
        {
            if (_alphaDepth == value)
                return;
    
            _alphaDepth = value;
            OnPropertyChanged(nameof(AlphaDepth));
        }
    }
    
    private uint _numMipMaps;
    public uint NumMipMaps
    {
        get => _numMipMaps;
        set
        {
            if (_numMipMaps == value)
                return;
    
            _numMipMaps = value;
            OnPropertyChanged(nameof(NumMipMaps));
        }
    }
    
    private TextureTypes _textureType;
    public TextureTypes TextureType
    {
        get => _textureType;
        set
        {
            if (_textureType == value)
                return;
    
            _textureType = value;
            OnPropertyChanged(nameof(TextureType));
        }
    }
    
    private UsageHints _usageHint;
    public UsageHints UsageHint
    {
        get => _usageHint;
        set
        {
            if (_usageHint == value)
                return;
    
            _usageHint = value;
            OnPropertyChanged(nameof(UsageHint));
        }
    }
    
    private uint _priority;
    public uint Priority
    {
        get => _priority;
        set
        {
            if (_priority == value)
                return;
    
            _priority = value;
            OnPropertyChanged(nameof(Priority));
        }
    }
    

    public override byte[] DataBytes
    {
        get
        {
            var data = new List<byte>((int)DataLength);

            data.AddRange(BinaryExtensions.GetP3DStringBytes(Name));
            data.AddRange(BitConverter.GetBytes(Version));
            data.AddRange(BitConverter.GetBytes(Width));
            data.AddRange(BitConverter.GetBytes(Height));
            data.AddRange(BitConverter.GetBytes(Bpp));
            data.AddRange(BitConverter.GetBytes(AlphaDepth));
            data.AddRange(BitConverter.GetBytes(NumMipMaps));
            data.AddRange(BitConverter.GetBytes((uint)TextureType));
            data.AddRange(BitConverter.GetBytes((uint)UsageHint));
            data.AddRange(BitConverter.GetBytes(Priority));

            return [.. data];
        }
    }
    public override uint DataLength => BinaryExtensions.GetP3DStringLength(Name) + sizeof(uint) + sizeof(uint) + sizeof(uint) + sizeof(uint) + sizeof(uint) + sizeof(uint) + sizeof(uint) + sizeof(uint) + sizeof(uint);

    public TextureChunk(EndianAwareBinaryReader br) : this(br.ReadP3DString(), br.ReadUInt32(), br.ReadUInt32(), br.ReadUInt32(), br.ReadUInt32(), br.ReadUInt32(), br.ReadUInt32(), (TextureTypes)br.ReadUInt32(), (UsageHints)br.ReadUInt32(), br.ReadUInt32())
    {
    }
    public TextureChunk(string name, uint version, uint width, uint height, uint bpp, uint alphaDepth, uint numMipMaps, TextureTypes textureType, UsageHints usageHint, uint priority) : base(ChunkID, name)
    {
        _version = version;
        _width = width;
        _height = height;
        _bpp = bpp;
        _alphaDepth = alphaDepth;
        _numMipMaps = numMipMaps;
        _textureType = textureType;
        _usageHint = usageHint;
        _priority = priority;
    }

    public override IEnumerable<InvalidP3DException> ValidateChunk()
    {
        foreach (var error in base.ValidateChunk())
            yield return error;

        if (!Width.IsPowerOfTwo())
            yield return new InvalidP3DException(this, $"{nameof(Width)} must be a power of 2.");

        if (!Height.IsPowerOfTwo())
            yield return new InvalidP3DException(this, $"{nameof(Height)} must be a power of 2.");
    }

    protected override void WriteData(EndianAwareBinaryWriter bw)
    {
        bw.WriteP3DString(Name);
        bw.Write(Version);
        bw.Write(Width);
        bw.Write(Height);
        bw.Write(Bpp);
        bw.Write(AlphaDepth);
        bw.Write(NumMipMaps);
        bw.Write((uint)TextureType);
        bw.Write((uint)UsageHint);
        bw.Write(Priority);
    }

    protected override Chunk CloneSelf() => new TextureChunk(Name, Version, Width, Height, Bpp, AlphaDepth, NumMipMaps, TextureType, UsageHint, Priority);
}
