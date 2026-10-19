using NetP3DLib.IO;
using NetP3DLib.P3D.Attributes;
using NetP3DLib.P3D.Enums;
using NetP3DLib.P3D.Exceptions;
using NetP3DLib.P3D.Extensions;
using NetP3DLib.P3D.Types;
using System;
using System.Collections.Generic;
using System.ComponentModel;
using System.Numerics;

namespace NetP3DLib.P3D.Chunks;

[ChunkAttributes(ChunkID)]
public class OldBillboardDisplayInfoChunk : Chunk
{
    public const ChunkIdentifier ChunkID = ChunkIdentifier.Old_Billboard_Display_Info;

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
    
    private Quaternion _rotation;
    public Quaternion Rotation
    {
        get => _rotation;
        set
        {
            if (_rotation == value)
                return;
    
            _rotation = value;
            OnPropertyChanged(nameof(Rotation));
        }
    }
    
    private readonly FourCC _cutOffMode;
    [MaxLength(4)]
    public string CutOffMode
    {
        get => _cutOffMode?.Value ?? string.Empty;
        set => _cutOffMode.Value = value;
    }
    
    private Vector2 _uVOffsetRange;
    public Vector2 UVOffsetRange
    {
        get => _uVOffsetRange;
        set
        {
            if (_uVOffsetRange == value)
                return;
    
            _uVOffsetRange = value;
            OnPropertyChanged(nameof(UVOffsetRange));
        }
    }
    
    private float _sourceRange;
    public float SourceRange
    {
        get => _sourceRange;
        set
        {
            if (_sourceRange == value)
                return;
    
            _sourceRange = value;
            OnPropertyChanged(nameof(SourceRange));
        }
    }
    
    private float _edgeRange;
    public float EdgeRange
    {
        get => _edgeRange;
        set
        {
            if (_edgeRange == value)
                return;
    
            _edgeRange = value;
            OnPropertyChanged(nameof(EdgeRange));
        }
    }
    

    public override byte[] DataBytes
    {
        get
        {
            var data = new List<byte>((int)DataLength);

            data.AddRange(BitConverter.GetBytes(Version));
            data.AddRange(BinaryExtensions.GetBytes(Rotation));
            data.AddRange(BinaryExtensions.GetFourCCBytes(CutOffMode));
            data.AddRange(BinaryExtensions.GetBytes(UVOffsetRange));
            data.AddRange(BitConverter.GetBytes(SourceRange));
            data.AddRange(BitConverter.GetBytes(EdgeRange));

            return [.. data];
        }
    }
    public override uint DataLength => sizeof(uint) + sizeof(float) * 4 + 4 + sizeof(float) * 2 + sizeof(float) + sizeof(float);

    public OldBillboardDisplayInfoChunk(EndianAwareBinaryReader br) : this(br.ReadUInt32(), br.ReadQuaternion(), br.ReadFourCC(), br.ReadVector2(), br.ReadSingle(), br.ReadSingle())
    {
    }

    public OldBillboardDisplayInfoChunk(uint version, Quaternion rotation, string cutOffMode, Vector2 uvOffsetRange, float sourceRange, float edgeRange) : base(ChunkID)
    {
        _version = version;
        _rotation = rotation;
        _cutOffMode = new(this, cutOffMode, nameof(CutOffMode));
        _uVOffsetRange = uvOffsetRange;
        _sourceRange = sourceRange;
        _edgeRange = edgeRange;
    }

    public override IEnumerable<InvalidP3DException> ValidateChunk()
    {
        foreach (var error in base.ValidateChunk())
            yield return error;

        if (!CutOffMode.IsValidFourCC())
            yield return new InvalidP3DFourCCException(this, nameof(CutOffMode), CutOffMode);
    }

    protected override void WriteData(EndianAwareBinaryWriter bw)
    {
        bw.Write(Version);
        bw.Write(Rotation);
        bw.WriteFourCC(CutOffMode);
        bw.Write(UVOffsetRange);
        bw.Write(SourceRange);
        bw.Write(EdgeRange);
    }

    protected override Chunk CloneSelf() => new OldBillboardDisplayInfoChunk(Version, Rotation, CutOffMode, UVOffsetRange, SourceRange, EdgeRange);
}
