using NetP3DLib.IO;
using NetP3DLib.P3D.Attributes;
using NetP3DLib.P3D.Enums;
using NetP3DLib.P3D.Exceptions;
using NetP3DLib.P3D.Extensions;
using NetP3DLib.P3D.Types;
using System;
using System.Collections.Generic;
using System.Drawing;

namespace NetP3DLib.P3D.Chunks;

[ChunkAttributes(ChunkID)]
public class BillboardQuadChunk : NamedChunk
{
    public const ChunkIdentifier ChunkID = ChunkIdentifier.Billboard_Quad;

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
    
    private uint _cutOffEnabled;
    public uint CutOffEnabled
    {
        get => _cutOffEnabled;
        set
        {
            if (_cutOffEnabled == value)
                return;
    
            _cutOffEnabled = value;
            OnPropertyChanged(nameof(CutOffEnabled));
        }
    }
    
    private uint _perspective;
    public uint Perspective
    {
        get => _perspective;
        set
        {
            if (_perspective == value)
                return;
    
            _perspective = value;
            OnPropertyChanged(nameof(Perspective));
        }
    }
    
    private readonly FourCC _axisMode;
    [MaxLength(4)]
    public string AxisMode
    {
        get => _axisMode?.Value ?? string.Empty;
        set => _axisMode.Value = value;
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
    
    private float _width;
    public float Width
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
    
    private float _height;
    public float Height
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
    
    private float _distance;
    public float Distance
    {
        get => _distance;
        set
        {
            if (_distance == value)
                return;
    
            _distance = value;
            OnPropertyChanged(nameof(Distance));
        }
    }
    

    public override byte[] DataBytes
    {
        get
        {
            var data = new List<byte>((int)DataLength);

            data.AddRange(BitConverter.GetBytes(Version));
            data.AddRange(BinaryExtensions.GetP3DStringBytes(Name));
            data.AddRange(BitConverter.GetBytes(CutOffEnabled));
            data.AddRange(BitConverter.GetBytes(Perspective));
            data.AddRange(BinaryExtensions.GetFourCCBytes(AxisMode));
            data.AddRange(BinaryExtensions.GetBytes(Colour));
            data.AddRange(BitConverter.GetBytes(Width));
            data.AddRange(BitConverter.GetBytes(Height));
            data.AddRange(BitConverter.GetBytes(Distance));

            return [.. data];
        }
    }
    public override uint DataLength => sizeof(uint) + BinaryExtensions.GetP3DStringLength(Name) + sizeof(uint) + sizeof(uint) + 4 + sizeof(uint) + sizeof(float) + sizeof(float) + sizeof(float);

    public BillboardQuadChunk(EndianAwareBinaryReader br) : this(br.ReadUInt32(), br.ReadP3DString(), br.ReadUInt32(),br.ReadUInt32(), br.ReadFourCC(), br.ReadColor(), br.ReadSingle(), br.ReadSingle(), br.ReadSingle())
    {
    }

    public BillboardQuadChunk(uint version, string name, uint cutOffEnabled, uint perspective, string axisMode, Color colour, float width, float height, float distance) : base(ChunkID, name)
    {
        _version = version;
        _cutOffEnabled = cutOffEnabled;
        _perspective = perspective;
        _axisMode = new(this, axisMode, nameof(AxisMode));
        _colour = colour;
        _width = width;
        _height = height;
        _distance = distance;
    }

    public override IEnumerable<InvalidP3DException> ValidateChunk()
    {
        foreach (var error in base.ValidateChunk())
            yield return error;

        if (!AxisMode.IsValidFourCC())
            yield return new InvalidP3DFourCCException(this, nameof(AxisMode), AxisMode);
    }

    protected override void WriteData(EndianAwareBinaryWriter bw)
    {
        bw.Write(Version);
        bw.WriteP3DString(Name);
        bw.Write(CutOffEnabled);
        bw.Write(Perspective);
        bw.WriteFourCC(AxisMode);
        bw.Write(Colour);
        bw.Write(Width);
        bw.Write(Height);
        bw.Write(Distance);
    }

    protected override Chunk CloneSelf() => new BillboardQuadChunk(Version, Name, CutOffEnabled, Perspective, AxisMode, Colour, Width, Height, Distance);
}
