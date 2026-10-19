using NetP3DLib.IO;
using NetP3DLib.P3D.Attributes;
using NetP3DLib.P3D.Enums;
using NetP3DLib.P3D.Extensions;
using System;
using System.Collections.Generic;
using System.ComponentModel;

namespace NetP3DLib.P3D.Chunks;

[ChunkAttributes(ChunkID)]
public class ImageChunk : NamedChunk
{
    public const ChunkIdentifier ChunkID = ChunkIdentifier.Image;

    public enum Formats
    {
        Raw = 0,
        PNG = 1,
        TGA = 2,
        BMP = 3,
        IPU = 4,
        DXT = 5,
        DXT1 = 6,
        DXT2 = 7,
        DXT3 = 8,
        DXT4 = 9,
        DXT5 = 10,
        PS24Bit = 11,
        PS28Bit = 12,
        PS216Bit = 13,
        PS232Bit = 14,
        GC4Bit = 15,
        GC8Bit = 16,
        GC16Bit = 17,
        GC32Bit = 18,
        GCDXT1 = 19,
        Other = 20,
        Invalid = 21,
        PSP4Bit = 25,
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
    
    private uint _palettized;
    public bool Palettized
    {
        get => _palettized == 1;
        set
        {
            if (Palettized == value)
                return;

            _palettized = value ? 1u : 0u;
            OnPropertyChanged(nameof(Palettized));
        }
    }
    private uint _hasAlpha;
    public bool HasAlpha
    {
        get => _hasAlpha == 1;
        set
        {
            if (HasAlpha == value)
                return;

            _hasAlpha = value ? 1u : 0u;
            OnPropertyChanged(nameof(HasAlpha));
        }
    }
    
    private Formats _format;
    public Formats Format
    {
        get => _format;
        set
        {
            if (_format == value)
                return;
    
            _format = value;
            OnPropertyChanged(nameof(Format));
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
            data.AddRange(BitConverter.GetBytes(_palettized));
            data.AddRange(BitConverter.GetBytes(_hasAlpha));
            data.AddRange(BitConverter.GetBytes((uint)Format));

            return [.. data];
        }
    }
    public override uint DataLength => BinaryExtensions.GetP3DStringLength(Name) + sizeof(uint) + sizeof(uint) + sizeof(uint) + sizeof(uint) + sizeof(uint) + sizeof(uint) + sizeof(uint);

    public ImageChunk(EndianAwareBinaryReader br) : this(br.ReadP3DString(), br.ReadUInt32(), br.ReadUInt32(), br.ReadUInt32(), br.ReadUInt32(), br.ReadUInt32(), br.ReadUInt32(), (Formats)br.ReadUInt32())
    {
    }

    public ImageChunk(string name, uint version, uint width, uint height, uint bpp, bool palettized, bool hasAlpha, Formats format) : this(name, version, width, height, bpp, palettized ? 1u : 0u, hasAlpha ? 1u : 0u, format)
    {
    }

    public ImageChunk(string name, uint version, uint width, uint height, uint bpp, uint palettized, uint hasAlpha, Formats format) : base(ChunkID, name)
    {
        _version = version;
        _width = width;
        _height = height;
        _bpp = bpp;
        _palettized = palettized;
        _hasAlpha = hasAlpha;
        _format = format;
    }

    private static readonly HashSet<Formats> FlippedFormats = [
        Formats.PS216Bit,
        Formats.PS232Bit,
        Formats.GC16Bit,
        Formats.GC32Bit,
        Formats.Raw,
        Formats.DXT1,
        Formats.GCDXT1,
        Formats.DXT3,
        Formats.DXT5,
    ];
    protected override void WriteData(EndianAwareBinaryWriter bw)
    {
        if (bw.Endianness != BinaryExtensions.DefaultEndian && FlippedFormats.Contains(Format))
            throw new NotImplementedException($"Writing {Format} images with swapped endian is not supported at this time.");

        bw.WriteP3DString(Name);
        bw.Write(Version);
        bw.Write(Width);
        bw.Write(Height);
        bw.Write(Bpp);
        bw.Write(_palettized);
        bw.Write(_hasAlpha);
        bw.Write((uint)Format);
    }

    protected override Chunk CloneSelf() => new ImageChunk(Name, Version, Width, Height, Bpp, Palettized, HasAlpha, Format);
}
