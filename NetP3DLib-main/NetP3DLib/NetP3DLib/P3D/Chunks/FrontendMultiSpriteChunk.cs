using NetP3DLib.IO;
using NetP3DLib.P3D.Attributes;
using NetP3DLib.P3D.Collections;
using NetP3DLib.P3D.Enums;
using NetP3DLib.P3D.Exceptions;
using NetP3DLib.P3D.Extensions;
using System;
using System.Collections.Generic;
using System.ComponentModel;
using System.Drawing;

namespace NetP3DLib.P3D.Chunks;

[ChunkAttributes(ChunkID)]
public class FrontendMultiSpriteChunk : NamedChunk
{
    public const ChunkIdentifier ChunkID = ChunkIdentifier.Frontend_Multi_Sprite;

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
    
    public uint NumImageNames
    {
        get => (uint)(ImageNames?.Count ?? 0);
        set
        {
            if (value == NumImageNames)
                return;

            if (value < NumImageNames)
            {
                ImageNames.RemoveRange((int)value, (int)(NumImageNames - value));
            }
            else
            {
                int count = (int)(value - NumImageNames);
                var newFrames = new string[count];

                for (var i = 0; i < count; i++)
                    newFrames[i] = string.Empty;

                ImageNames.AddRange(newFrames);
            }
        }
    }
    public SizeAwareList<string> ImageNames { get; }

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
            data.AddRange(BitConverter.GetBytes(NumImageNames));
            foreach (var imageName in ImageNames)
                data.AddRange(BinaryExtensions.GetP3DStringBytes(imageName));

            return [.. data];
        }
    }
    public override uint DataLength
    {
        get
        {
            uint size = BinaryExtensions.GetP3DStringLength(Name) + sizeof(uint) + sizeof(int) + sizeof(int) + sizeof(uint) + sizeof(uint) + sizeof(uint) + sizeof(uint) + sizeof(uint) + sizeof(uint) + sizeof(float) + sizeof(uint);

            if (ImageNames != null)
                foreach (var imageName in ImageNames)
                    size += BinaryExtensions.GetP3DStringLength(imageName);

            return size;
        }
    }

    public FrontendMultiSpriteChunk(EndianAwareBinaryReader br) : this(br.ReadP3DString(), br.ReadUInt32(), br.ReadInt32(), br.ReadInt32(), br.ReadUInt32(), br.ReadUInt32(), (Justifications)br.ReadUInt32(), (Justifications)br.ReadUInt32(), br.ReadColor(), br.ReadUInt32(), br.ReadSingle(), br.ReadP3DStringArray(out _))
    {
    }

    public FrontendMultiSpriteChunk(string name, uint version, int positionX, int positionY, uint dimensionX, uint dimensionY, Justifications justificationX, Justifications justificationY, Color colour, uint translucency, float rotationValue, IList<string> imageNames) : base(ChunkID, name)
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
        ImageNames = CreateSizeAwareList(imageNames, ImageNames_CollectionChanged);
    }
    
    private void ImageNames_CollectionChanged(object sender, System.Collections.Specialized.NotifyCollectionChangedEventArgs e) => OnPropertyChanged(nameof(ImageNames));

    public override IEnumerable<InvalidP3DException> ValidateChunk()
    {
        foreach (var error in base.ValidateChunk())
            yield return error;

        foreach (var imageName in ImageNames)
            if (!imageName.IsValidP3DString())
                yield return new InvalidP3DStringException(this, nameof(ImageNames), imageName);
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
        bw.Write(NumImageNames);
        foreach (var imageName in ImageNames)
            bw.WriteP3DString(imageName);
    }

    protected override Chunk CloneSelf() => new FrontendMultiSpriteChunk(Name, Version, PositionX, PositionY, DimensionX, DimensionY, JustificationX, JustificationY, Colour, Translucency, RotationValue, ImageNames);
}
