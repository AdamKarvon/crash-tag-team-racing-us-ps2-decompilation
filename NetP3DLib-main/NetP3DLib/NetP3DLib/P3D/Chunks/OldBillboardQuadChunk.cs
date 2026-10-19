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
using System.Numerics;

namespace NetP3DLib.P3D.Chunks;

[ChunkAttributes(ChunkID)]
public class OldBillboardQuadChunk : NamedChunk
{
    public const ChunkIdentifier ChunkID = ChunkIdentifier.Old_Billboard_Quad;

    private uint _version;
    [DefaultValue(2)]
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
    
    private readonly FourCC _billboardMode;
    [MaxLength(4)]
    public string BillboardMode
    {
        get => _billboardMode?.Value ?? string.Empty;
        set => _billboardMode.Value = value;
    }
    
    private Vector3 _translation;
    public Vector3 Translation
    {
        get => _translation;
        set
        {
            if (_translation == value)
                return;
    
            _translation = value;
            OnPropertyChanged(nameof(Translation));
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
    
    private Vector2 _uV0;
    public Vector2 UV0
    {
        get => _uV0;
        set
        {
            if (_uV0 == value)
                return;
    
            _uV0 = value;
            OnPropertyChanged(nameof(UV0));
        }
    }
    
    private Vector2 _uV1;
    public Vector2 UV1
    {
        get => _uV1;
        set
        {
            if (_uV1 == value)
                return;
    
            _uV1 = value;
            OnPropertyChanged(nameof(UV1));
        }
    }
    
    private Vector2 _uV2;
    public Vector2 UV2
    {
        get => _uV2;
        set
        {
            if (_uV2 == value)
                return;
    
            _uV2 = value;
            OnPropertyChanged(nameof(UV2));
        }
    }
    
    private Vector2 _uV3;
    public Vector2 UV3
    {
        get => _uV3;
        set
        {
            if (_uV3 == value)
                return;
    
            _uV3 = value;
            OnPropertyChanged(nameof(UV3));
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
    
    private Vector2 _uVOffset;
    public Vector2 UVOffset
    {
        get => _uVOffset;
        set
        {
            if (_uVOffset == value)
                return;
    
            _uVOffset = value;
            OnPropertyChanged(nameof(UVOffset));
        }
    }
    

    public override byte[] DataBytes
    {
        get
        {
            var data = new List<byte>((int)DataLength);

            data.AddRange(BitConverter.GetBytes(Version));
            data.AddRange(BinaryExtensions.GetP3DStringBytes(Name));
            data.AddRange(BinaryExtensions.GetFourCCBytes(BillboardMode));
            data.AddRange(BinaryExtensions.GetBytes(Translation));
            data.AddRange(BinaryExtensions.GetBytes(Colour));
            data.AddRange(BinaryExtensions.GetBytes(UV0));
            data.AddRange(BinaryExtensions.GetBytes(UV1));
            data.AddRange(BinaryExtensions.GetBytes(UV2));
            data.AddRange(BinaryExtensions.GetBytes(UV3));
            data.AddRange(BitConverter.GetBytes(Width));
            data.AddRange(BitConverter.GetBytes(Height));
            data.AddRange(BitConverter.GetBytes(Distance));
            data.AddRange(BinaryExtensions.GetBytes(UVOffset));

            return [.. data];
        }
    }
    public override uint DataLength => sizeof(uint) + BinaryExtensions.GetP3DStringLength(Name) + 4 + sizeof(float) * 3 + sizeof(uint) + sizeof(float) * 2 + sizeof(float) * 2 + sizeof(float) * 2 + sizeof(float) * 2 + sizeof(float) + sizeof(float) + sizeof(float) + sizeof(float) * 2;

    public OldBillboardQuadChunk(EndianAwareBinaryReader br) : this(br.ReadUInt32(), br.ReadP3DString(), br.ReadFourCC(), br.ReadVector3(), br.ReadColor(), br.ReadVector2(), br.ReadVector2(), br.ReadVector2(), br.ReadVector2(), br.ReadSingle(), br.ReadSingle(), br.ReadSingle(), br.ReadVector2())
    {
    }

    public OldBillboardQuadChunk(uint version, string name, string billboardMode, Vector3 translation, Color colour, Vector2 uv0, Vector2 uv1, Vector2 uv2, Vector2 uv3, float width, float height, float distance, Vector2 uvOffset) : base(ChunkID, name)
    {
        _version = version;
        _billboardMode = new(this, billboardMode, nameof(BillboardMode));
        _translation = translation;
        _colour = colour;
        _uV0 = uv0;
        _uV1 = uv1;
        _uV2 = uv2;
        _uV3 = uv3;
        _width = width;
        _height = height;
        _distance = distance;
        _uVOffset = uvOffset;
    }

    public override IEnumerable<InvalidP3DException> ValidateChunk()
    {
        foreach (var error in base.ValidateChunk())
            yield return error;

        if (!BillboardMode.IsValidFourCC())
            yield return new InvalidP3DFourCCException(this, nameof(BillboardMode), BillboardMode);
    }

    protected override void WriteData(EndianAwareBinaryWriter bw)
    {
        bw.Write(Version);
        bw.WriteP3DString(Name);
        bw.WriteFourCC(BillboardMode);
        bw.Write(Translation);
        bw.Write(Colour);
        bw.Write(UV0);
        bw.Write(UV1);
        bw.Write(UV2);
        bw.Write(UV3);
        bw.Write(Width);
        bw.Write(Height);
        bw.Write(Distance);
        bw.Write(UVOffset);
    }

    protected override Chunk CloneSelf() => new OldBillboardQuadChunk(Version, Name, BillboardMode, Translation, Colour, UV0, UV1, UV2, UV3, Width, Height, Distance, UVOffset);
}
