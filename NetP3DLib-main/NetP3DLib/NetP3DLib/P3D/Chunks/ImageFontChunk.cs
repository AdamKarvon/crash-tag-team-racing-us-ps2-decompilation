using NetP3DLib.IO;
using NetP3DLib.P3D.Attributes;
using NetP3DLib.P3D.Enums;
using NetP3DLib.P3D.Extensions;
using System;
using System.Collections.Generic;

namespace NetP3DLib.P3D.Chunks;

[ChunkAttributes(ChunkID)]
public class ImageFontChunk : NamedChunk
{
    public const ChunkIdentifier ChunkID = ChunkIdentifier.Image_Font;

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
    
    private float _fontSize;
    public float FontSize
    {
        get => _fontSize;
        set
        {
            if (_fontSize == value)
                return;
    
            _fontSize = value;
            OnPropertyChanged(nameof(FontSize));
        }
    }
    
    private float _fontWidth;
    public float FontWidth
    {
        get => _fontWidth;
        set
        {
            if (_fontWidth == value)
                return;
    
            _fontWidth = value;
            OnPropertyChanged(nameof(FontWidth));
        }
    }
    
    private float _fontHeight;
    public float FontHeight
    {
        get => _fontHeight;
        set
        {
            if (_fontHeight == value)
                return;
    
            _fontHeight = value;
            OnPropertyChanged(nameof(FontHeight));
        }
    }
    
    private float _fontBaseLine;
    public float FontBaseLine
    {
        get => _fontBaseLine;
        set
        {
            if (_fontBaseLine == value)
                return;
    
            _fontBaseLine = value;
            OnPropertyChanged(nameof(FontBaseLine));
        }
    }
    

    public override byte[] DataBytes
    {
        get
        {
            var data = new List<byte>((int)DataLength);

            data.AddRange(BitConverter.GetBytes(Version));
            data.AddRange(BinaryExtensions.GetP3DStringBytes(Name));
            data.AddRange(BitConverter.GetBytes(FontSize));
            data.AddRange(BitConverter.GetBytes(FontWidth));
            data.AddRange(BitConverter.GetBytes(FontHeight));
            data.AddRange(BitConverter.GetBytes(FontBaseLine));

            return [.. data];
        }
    }
    public override uint DataLength => sizeof(uint) + BinaryExtensions.GetP3DStringLength(Name) + sizeof(float) + sizeof(float) + sizeof(float) + sizeof(float);

    public ImageFontChunk(EndianAwareBinaryReader br) : this(br.ReadUInt32(), br.ReadP3DString(), br.ReadSingle(), br.ReadSingle(), br.ReadSingle(), br.ReadSingle())
    {
    }

    public ImageFontChunk(uint version, string name, float fontSize, float fontWidth, float fontHeight, float fontBaseLine) : base(ChunkID, name)
    {
        _version = version;
        _fontSize = fontSize;
        _fontWidth = fontWidth;
        _fontHeight = fontHeight;
        _fontBaseLine = fontBaseLine;
    }

    protected override void WriteData(EndianAwareBinaryWriter bw)
    {
        bw.Write(Version);
        bw.WriteP3DString(Name);
        bw.Write(FontSize);
        bw.Write(FontWidth);
        bw.Write(FontHeight);
        bw.Write(FontBaseLine);
    }

    protected override Chunk CloneSelf() => new ImageFontChunk(Version, Name, FontSize, FontWidth, FontHeight, FontBaseLine);
}
