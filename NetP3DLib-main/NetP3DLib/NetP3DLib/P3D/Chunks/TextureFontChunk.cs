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
public class TextureFontChunk : NamedChunk
{
    public const ChunkIdentifier ChunkID = ChunkIdentifier.Texture_Font;

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
    
    private readonly P3DString _shader;
    public string Shader
    {
        get => _shader?.Value ?? string.Empty;
        set => _shader.Value = value;
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
    
    public uint NumTextures => GetChildCount(ChunkIdentifier.Texture);

    public override byte[] DataBytes
    {
        get
        {
            var data = new List<byte>((int)DataLength);

            data.AddRange(BitConverter.GetBytes(Version));
            data.AddRange(BinaryExtensions.GetP3DStringBytes(Name));
            data.AddRange(BinaryExtensions.GetP3DStringBytes(Shader));
            data.AddRange(BitConverter.GetBytes(FontSize));
            data.AddRange(BitConverter.GetBytes(FontWidth));
            data.AddRange(BitConverter.GetBytes(FontHeight));
            data.AddRange(BitConverter.GetBytes(FontBaseLine));
            data.AddRange(BitConverter.GetBytes(NumTextures));

            return [.. data];
        }
    }
    public override uint DataLength => sizeof(uint) + BinaryExtensions.GetP3DStringLength(Name) + BinaryExtensions.GetP3DStringLength(Shader) + sizeof(float) + sizeof(float) + sizeof(float) + sizeof(float) + sizeof(uint);

    [System.Diagnostics.CodeAnalysis.SuppressMessage("Style", "IDE0059:Unnecessary assignment of a value", Justification = "We want to read the value to progress the BinaryReader, but not set the value anywhere because it's calculated dynamically.")]
    public TextureFontChunk(EndianAwareBinaryReader br) : this(br.ReadUInt32(), br.ReadP3DString(), br.ReadP3DString(), br.ReadSingle(), br.ReadSingle(), br.ReadSingle(), br.ReadSingle())
    {
        var numTexture = br.ReadUInt32();
    }

    public TextureFontChunk(uint version, string name, string shader, float fontSize, float fontWidth, float fontHeight, float fontBaseLine) : base(ChunkID, name)
    {
        _version = version;
        _shader = new(this, shader, nameof(Shader));
        _fontSize = fontSize;
        _fontWidth = fontWidth;
        _fontHeight = fontHeight;
        _fontBaseLine = fontBaseLine;
    }

    public override IEnumerable<InvalidP3DException> ValidateChunk()
    {
        foreach (var error in base.ValidateChunk())
            yield return error;

        if (!Shader.IsValidP3DString())
            yield return new InvalidP3DStringException(this, nameof(Shader), Shader);
    }

    protected override void WriteData(EndianAwareBinaryWriter bw)
    {
        bw.Write(Version);
        bw.WriteP3DString(Name);
        bw.WriteP3DString(Shader);
        bw.Write(FontSize);
        bw.Write(FontWidth);
        bw.Write(FontHeight);
        bw.Write(FontBaseLine);
        bw.Write(NumTextures);
    }

    protected override Chunk CloneSelf() => new TextureFontChunk(Version, Name, Shader, FontSize, FontWidth, FontHeight, FontBaseLine);
}
