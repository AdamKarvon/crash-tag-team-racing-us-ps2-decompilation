using NetP3DLib.IO;
using NetP3DLib.P3D.Attributes;
using NetP3DLib.P3D.Enums;
using NetP3DLib.P3D.Exceptions;
using NetP3DLib.P3D.Extensions;
using NetP3DLib.P3D.Types;
using System;
using System.Collections.Generic;
using System.ComponentModel;
using System.Drawing;

namespace NetP3DLib.P3D.Chunks;

[ChunkAttributes(ChunkID)]
public class FrontendMultiTextChunk : NamedChunk
{
    public const ChunkIdentifier ChunkID = ChunkIdentifier.Frontend_Multi_Text;

    public enum Justifications : uint
    {
        Left,
        Right,
        Top,
        Bottom,
        Centre
    }

    private uint _version;
    [DefaultValue(17)]
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
    
    private int _positionX;
    public int PositionX
    {
        get => _positionX;
        set
        {
            if (_positionX == value)
                return;
    
            _positionX = value;
            OnPropertyChanged(nameof(PositionX));
        }
    }
    
    private int _positionY;
    public int PositionY
    {
        get => _positionY;
        set
        {
            if (_positionY == value)
                return;
    
            _positionY = value;
            OnPropertyChanged(nameof(PositionY));
        }
    }
    
    private uint _dimensionX;
    public uint DimensionX
    {
        get => _dimensionX;
        set
        {
            if (_dimensionX == value)
                return;
    
            _dimensionX = value;
            OnPropertyChanged(nameof(DimensionX));
        }
    }
    
    private uint _dimensionY;
    public uint DimensionY
    {
        get => _dimensionY;
        set
        {
            if (_dimensionY == value)
                return;
    
            _dimensionY = value;
            OnPropertyChanged(nameof(DimensionY));
        }
    }
    
    private Justifications _justificationX;
    public Justifications JustificationX
    {
        get => _justificationX;
        set
        {
            if (_justificationX == value)
                return;
    
            _justificationX = value;
            OnPropertyChanged(nameof(JustificationX));
        }
    }
    
    private Justifications _justificationY;
    public Justifications JustificationY
    {
        get => _justificationY;
        set
        {
            if (_justificationY == value)
                return;
    
            _justificationY = value;
            OnPropertyChanged(nameof(JustificationY));
        }
    }
    
    private Color _colour;
    public Color Colour
    {
        get => _colour;
        set
        {
            if (_colour == value)
                return;
    
            _colour = value;
            OnPropertyChanged(nameof(Colour));
        }
    }
    
    private uint _translucency;
    public uint Translucency
    {
        get => _translucency;
        set
        {
            if (_translucency == value)
                return;
    
            _translucency = value;
            OnPropertyChanged(nameof(Translucency));
        }
    }
    
    private float _rotationValue;
    public float RotationValue
    {
        get => _rotationValue;
        set
        {
            if (_rotationValue == value)
                return;
    
            _rotationValue = value;
            OnPropertyChanged(nameof(RotationValue));
        }
    }
    
    private readonly P3DString _textStyleName;
    public string TextStyleName
    {
        get => _textStyleName?.Value ?? string.Empty;
        set => _textStyleName.Value = value;
    }
    private byte _shadowEnabled;
    public bool ShadowEnabled
    {
        get => _shadowEnabled != 0;
        set
        {
            if (ShadowEnabled == value)
                return;

            _shadowEnabled = (byte)(value ? 1 : 0);
            OnPropertyChanged(nameof(ShadowEnabled));
        }
    }
    
    private Color _shadowColour;
    public Color ShadowColour
    {
        get => _shadowColour;
        set
        {
            if (_shadowColour == value)
                return;
    
            _shadowColour = value;
            OnPropertyChanged(nameof(ShadowColour));
        }
    }
    
    private int _shadowOffsetX;
    public int ShadowOffsetX
    {
        get => _shadowOffsetX;
        set
        {
            if (_shadowOffsetX == value)
                return;
    
            _shadowOffsetX = value;
            OnPropertyChanged(nameof(ShadowOffsetX));
        }
    }
    
    private int _shadowOffsetY;
    public int ShadowOffsetY
    {
        get => _shadowOffsetY;
        set
        {
            if (_shadowOffsetY == value)
                return;
    
            _shadowOffsetY = value;
            OnPropertyChanged(nameof(ShadowOffsetY));
        }
    }
    
    private uint _currentText;
    public uint CurrentText
    {
        get => _currentText;
        set
        {
            if (_currentText == value)
                return;
    
            _currentText = value;
            OnPropertyChanged(nameof(CurrentText));
        }
    }
    

    public override byte[] DataBytes
    {
        get
        {
            var data = new List<byte>((int)DataLength);

            data.AddRange(BinaryExtensions.GetP3DStringBytes(Name));
            data.AddRange(BitConverter.GetBytes(Version));
            data.AddRange(BitConverter.GetBytes(PositionX));
            data.AddRange(BitConverter.GetBytes(PositionY));
            data.AddRange(BitConverter.GetBytes(DimensionX));
            data.AddRange(BitConverter.GetBytes(DimensionY));
            data.AddRange(BitConverter.GetBytes((uint)JustificationX));
            data.AddRange(BitConverter.GetBytes((uint)JustificationY));
            data.AddRange(BinaryExtensions.GetBytes(Colour));
            data.AddRange(BitConverter.GetBytes(Translucency));
            data.AddRange(BitConverter.GetBytes(RotationValue));
            data.AddRange(BinaryExtensions.GetP3DStringBytes(TextStyleName));
            data.Add(_shadowEnabled);
            data.AddRange(BinaryExtensions.GetBytes(ShadowColour));
            data.AddRange(BitConverter.GetBytes(ShadowOffsetX));
            data.AddRange(BitConverter.GetBytes(ShadowOffsetY));
            data.AddRange(BitConverter.GetBytes(CurrentText));

            return [.. data];
        }
    }
    public override uint DataLength => BinaryExtensions.GetP3DStringLength(Name) + sizeof(uint) + sizeof(int) + sizeof(int) + sizeof(uint) + sizeof(uint) + sizeof(uint) + sizeof(uint) + sizeof(uint) + sizeof(uint) + sizeof(float) + BinaryExtensions.GetP3DStringLength(TextStyleName) + sizeof(byte) + sizeof(uint) + sizeof(int) + sizeof(int) + sizeof(uint);

    public FrontendMultiTextChunk(EndianAwareBinaryReader br) : this(br.ReadP3DString(), br.ReadUInt32(), br.ReadInt32(), br.ReadInt32(), br.ReadUInt32(), br.ReadUInt32(), (Justifications)br.ReadUInt32(), (Justifications)br.ReadUInt32(), br.ReadColor(), br.ReadUInt32(), br.ReadSingle(), br.ReadP3DString(), br.ReadByte(), br.ReadColor(), br.ReadInt32(), br.ReadInt32(), br.ReadUInt32())
    {
    }

    public FrontendMultiTextChunk(string name, uint version, int positionX, int positionY, uint dimensionX, uint dimensionY, Justifications justificationX, Justifications justificationY, Color colour, uint translucency, float rotationValue, string textStyleName, bool shadowEnabled, Color shadowColour, int shadowOffsetX, int shadowOffsetY, uint currentText) : this(name, version, positionX, positionY, dimensionX, dimensionY, justificationX, justificationY, colour, translucency, rotationValue, textStyleName, (byte)(shadowEnabled ? 1 : 0), shadowColour, shadowOffsetX, shadowOffsetY, currentText)
    {
    }

    public FrontendMultiTextChunk(string name, uint version, int positionX, int positionY, uint dimensionX, uint dimensionY, Justifications justificationX, Justifications justificationY, Color colour, uint translucency, float rotationValue, string textStyleName, byte shadowEnabled, Color shadowColour, int shadowOffsetX, int shadowOffsetY, uint currentText) : base(ChunkID, name)
    {
        _version = version;
        _positionX = positionX;
        _positionY = positionY;
        _dimensionX = dimensionX;
        _dimensionY = dimensionY;
        _justificationX = justificationX;
        _justificationY = justificationY;
        _colour = colour;
        _translucency = translucency;
        _rotationValue = rotationValue;
        _textStyleName = new(this, textStyleName, nameof(TextStyleName));
        _shadowEnabled = shadowEnabled;
        ShadowColour = shadowColour;
        _shadowOffsetX = shadowOffsetX;
        _shadowOffsetY = shadowOffsetY;
        _currentText = currentText;
    }

    public override IEnumerable<InvalidP3DException> ValidateChunk()
    {
        foreach (var error in base.ValidateChunk())
            yield return error;

        if (!TextStyleName.IsValidP3DString())
            yield return new InvalidP3DStringException(this, nameof(TextStyleName), TextStyleName);
    }

    protected override void WriteData(EndianAwareBinaryWriter bw)
    {
        bw.WriteP3DString(Name);
        bw.Write(Version);
        bw.Write(PositionX);
        bw.Write(PositionY);
        bw.Write(DimensionX);
        bw.Write(DimensionY);
        bw.Write((uint)JustificationX);
        bw.Write((uint)JustificationY);
        bw.Write(Colour);
        bw.Write(Translucency);
        bw.Write(RotationValue);
        bw.WriteP3DString(TextStyleName);
        bw.Write(_shadowEnabled);
        bw.Write(ShadowColour);
        bw.Write(ShadowOffsetX);
        bw.Write(ShadowOffsetY);
        bw.Write(CurrentText);
    }

    protected override Chunk CloneSelf() => new FrontendMultiTextChunk(Name, Version, PositionX, PositionY, DimensionX, DimensionY, JustificationX, JustificationY, Colour, Translucency, RotationValue, TextStyleName, ShadowEnabled, ShadowColour, ShadowOffsetX, ShadowOffsetY, CurrentText);
}
