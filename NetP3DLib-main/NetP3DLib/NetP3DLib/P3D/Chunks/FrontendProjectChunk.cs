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
public class FrontendProjectChunk : NamedChunk
{
    public const ChunkIdentifier ChunkID = ChunkIdentifier.Frontend_Project;

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
    
    private uint _resolutionX;
    public uint ResolutionX
    {
        get => _resolutionX;
        set
        {
            if (_resolutionX == value)
                return;
    
            _resolutionX = value;
            OnPropertyChanged(nameof(ResolutionX));
        }
    }
    
    private uint _resolutionY;
    public uint ResolutionY
    {
        get => _resolutionY;
        set
        {
            if (_resolutionY == value)
                return;
    
            _resolutionY = value;
            OnPropertyChanged(nameof(ResolutionY));
        }
    }
    
    private readonly P3DString _platform;
    public string Platform
    {
        get => _platform?.Value ?? string.Empty;
        set => _platform.Value = value;
    }
    private readonly P3DString _pagePath;
    public string PagePath
    {
        get => _pagePath?.Value ?? string.Empty;
        set => _pagePath.Value = value;
    }
    private readonly P3DString _resourcePath;
    public string ResourcePath
    {
        get => _resourcePath?.Value ?? string.Empty;
        set => _resourcePath.Value = value;
    }
    private readonly P3DString _screenPath;
    public string ScreenPath
    {
        get => _screenPath?.Value ?? string.Empty;
        set => _screenPath.Value = value;
    }

    public override byte[] DataBytes
    {
        get
        {
            var data = new List<byte>((int)DataLength);

            data.AddRange(BinaryExtensions.GetP3DStringBytes(Name));
            data.AddRange(BitConverter.GetBytes(Version));
            data.AddRange(BitConverter.GetBytes(ResolutionX));
            data.AddRange(BitConverter.GetBytes(ResolutionY));
            data.AddRange(BinaryExtensions.GetP3DStringBytes(Platform));
            data.AddRange(BinaryExtensions.GetP3DStringBytes(PagePath));
            data.AddRange(BinaryExtensions.GetP3DStringBytes(ResourcePath));
            data.AddRange(BinaryExtensions.GetP3DStringBytes(ScreenPath));

            return [.. data];
        }
    }
    public override uint DataLength => BinaryExtensions.GetP3DStringLength(Name) + sizeof(uint) + sizeof(uint) + sizeof(uint) + BinaryExtensions.GetP3DStringLength(Platform) + BinaryExtensions.GetP3DStringLength(PagePath) + BinaryExtensions.GetP3DStringLength(ResourcePath) + BinaryExtensions.GetP3DStringLength(ScreenPath);

    public FrontendProjectChunk(EndianAwareBinaryReader br) : this(br.ReadP3DString(), br.ReadUInt32(), br.ReadUInt32(), br.ReadUInt32(), br.ReadP3DString(), br.ReadP3DString(), br.ReadP3DString(), br.ReadP3DString())
    {
    }

    public FrontendProjectChunk(string name, uint version, uint resolutionX, uint resolutionY, string platform, string pagePath, string resourcePath, string screenPath) : base(ChunkID, name)
    {
        _version = version;
        _resolutionX = resolutionX;
        _resolutionY = resolutionY;
        _platform = new(this, platform, nameof(Platform));
        _pagePath = new(this, pagePath, nameof(PagePath));
        _resourcePath = new(this, resourcePath, nameof(ResourcePath));
        _screenPath = new(this, screenPath, nameof(ScreenPath));
    }

    public override IEnumerable<InvalidP3DException> ValidateChunk()
    {
        foreach (var error in base.ValidateChunk())
            yield return error;

        if (!Platform.IsValidP3DString())
            yield return new InvalidP3DStringException(this, nameof(Platform), Platform);

        if (!PagePath.IsValidP3DString())
            yield return new InvalidP3DStringException(this, nameof(PagePath), PagePath);

        if (!ResourcePath.IsValidP3DString())
            yield return new InvalidP3DStringException(this, nameof(ResourcePath), ResourcePath);

        if (!ScreenPath.IsValidP3DString())
            yield return new InvalidP3DStringException(this, nameof(ScreenPath), ScreenPath);
    }

    protected override void WriteData(EndianAwareBinaryWriter bw)
    {
        bw.WriteP3DString(Name);
        bw.Write(Version);
        bw.Write(ResolutionX);
        bw.Write(ResolutionY);
        bw.WriteP3DString(Platform);
        bw.WriteP3DString(PagePath);
        bw.WriteP3DString(ResourcePath);
        bw.WriteP3DString(ScreenPath);
    }

    protected override Chunk CloneSelf() => new FrontendProjectChunk(Name, Version, ResolutionX, ResolutionY, Platform, PagePath, ResourcePath, ScreenPath);
}
