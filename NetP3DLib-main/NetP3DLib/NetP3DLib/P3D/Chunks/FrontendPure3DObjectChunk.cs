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
public class FrontendPure3DObjectChunk : NamedChunk
{
    public const ChunkIdentifier ChunkID = ChunkIdentifier.Frontend_Pure3D_Object;

    public enum Justifications : uint
    {
        Left,
        Right,
        Top,
        Bottom,
        Centre
    }

    private uint _version;
    [DefaultValue(1)]
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
    
    private readonly P3DString _pure3DFilename;
    public string Pure3DFilename
    {
        get => _pure3DFilename?.Value ?? string.Empty;
        set => _pure3DFilename.Value = value;
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
            data.AddRange(BinaryExtensions.GetP3DStringBytes(Pure3DFilename));

            return [.. data];
        }
    }
    public override uint DataLength => BinaryExtensions.GetP3DStringLength(Name) + sizeof(uint) + sizeof(int) + sizeof(int) + sizeof(uint) + sizeof(uint) + sizeof(uint) + sizeof(uint) + sizeof(uint) + sizeof(uint) + sizeof(float) + BinaryExtensions.GetP3DStringLength(Pure3DFilename);

    public FrontendPure3DObjectChunk(EndianAwareBinaryReader br) : this(br.ReadP3DString(), br.ReadUInt32(), br.ReadInt32(), br.ReadInt32(), br.ReadUInt32(), br.ReadUInt32(), (Justifications)br.ReadUInt32(), (Justifications)br.ReadUInt32(), br.ReadColor(), br.ReadUInt32(), br.ReadSingle(), br.ReadP3DString())
    {
    }

    public FrontendPure3DObjectChunk(string name, uint version, int positionX, int positionY, uint dimensionX, uint dimensionY, Justifications justificationX, Justifications justificationY, Color colour, uint translucency, float rotationValue, string pure3DFilename) : base(ChunkID, name)
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
        _pure3DFilename = new(this, pure3DFilename, nameof(Pure3DFilename));
    }

    public override IEnumerable<InvalidP3DException> ValidateChunk()
    {
        foreach (var error in base.ValidateChunk())
            yield return error;

        if (!Pure3DFilename.IsValidP3DString())
            yield return new InvalidP3DStringException(this, nameof(Pure3DFilename), Pure3DFilename);
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
        bw.WriteP3DString(Pure3DFilename);
    }

    protected override Chunk CloneSelf() => new FrontendPure3DObjectChunk(Name, Version, PositionX, PositionY, DimensionX, DimensionY, JustificationX, JustificationY, Colour, Translucency, RotationValue, Pure3DFilename);
}
