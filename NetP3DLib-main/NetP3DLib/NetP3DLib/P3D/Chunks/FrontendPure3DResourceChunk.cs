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
public class FrontendPure3DResourceChunk : NamedChunk
{
    public const ChunkIdentifier ChunkID = ChunkIdentifier.Frontend_Pure3D_Resource;

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
    
    private readonly P3DString _filename;
    public string Filename
    {
        get => _filename?.Value ?? string.Empty;
        set => _filename.Value = value;
    }
    private readonly P3DString _inventoryName;
    public string InventoryName
    {
        get => _inventoryName?.Value ?? string.Empty;
        set => _inventoryName.Value = value;
    }
    private readonly P3DString _cameraName;
    public string CameraName
    {
        get => _cameraName?.Value ?? string.Empty;
        set => _cameraName.Value = value;
    }
    private readonly P3DString _animationName;
    public string AnimationName
    {
        get => _animationName?.Value ?? string.Empty;
        set => _animationName.Value = value;
    }

    public override byte[] DataBytes
    {
        get
        {
            var data = new List<byte>((int)DataLength);

            data.AddRange(BinaryExtensions.GetP3DStringBytes(Name));
            data.AddRange(BitConverter.GetBytes(Version));
            data.AddRange(BinaryExtensions.GetP3DStringBytes(Filename));
            data.AddRange(BinaryExtensions.GetP3DStringBytes(InventoryName));
            data.AddRange(BinaryExtensions.GetP3DStringBytes(CameraName));
            data.AddRange(BinaryExtensions.GetP3DStringBytes(AnimationName));

            return [.. data];
        }
    }
    public override uint DataLength => BinaryExtensions.GetP3DStringLength(Name) + sizeof(uint) + BinaryExtensions.GetP3DStringLength(Filename) + BinaryExtensions.GetP3DStringLength(InventoryName) + BinaryExtensions.GetP3DStringLength(CameraName) + BinaryExtensions.GetP3DStringLength(AnimationName);

    public FrontendPure3DResourceChunk(EndianAwareBinaryReader br) : this(br.ReadP3DString(), br.ReadUInt32(), br.ReadP3DString(), br.ReadP3DString(), br.ReadP3DString(), br.ReadP3DString())
    {
    }

    public FrontendPure3DResourceChunk(string name, uint version, string filename, string inventoryName, string cameraName, string animationName) : base(ChunkID, name)
    {
        _version = version;
        _filename = new(this, filename, nameof(Filename));
        _inventoryName = new(this, inventoryName, nameof(InventoryName));
        _cameraName = new(this, cameraName, nameof(CameraName));
        _animationName = new(this, animationName, nameof(AnimationName));
    }

    public override IEnumerable<InvalidP3DException> ValidateChunk()
    {
        foreach (var error in base.ValidateChunk())
            yield return error;

        if (!Filename.IsValidP3DString())
            yield return new InvalidP3DStringException(this, nameof(Filename), Filename);

        if (!InventoryName.IsValidP3DString())
            yield return new InvalidP3DStringException(this, nameof(InventoryName), InventoryName);

        if (!CameraName.IsValidP3DString())
            yield return new InvalidP3DStringException(this, nameof(CameraName), CameraName);

        if (!AnimationName.IsValidP3DString())
            yield return new InvalidP3DStringException(this, nameof(AnimationName), AnimationName);
    }

    protected override void WriteData(EndianAwareBinaryWriter bw)
    {
        bw.WriteP3DString(Name);
        bw.Write(Version);
        bw.WriteP3DString(Filename);
        bw.WriteP3DString(InventoryName);
        bw.WriteP3DString(CameraName);
        bw.WriteP3DString(AnimationName);
    }

    protected override Chunk CloneSelf() => new FrontendPure3DResourceChunk(Name, Version, Filename, InventoryName, CameraName, AnimationName);
}
