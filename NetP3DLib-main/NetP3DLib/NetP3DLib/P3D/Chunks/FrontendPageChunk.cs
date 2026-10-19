using NetP3DLib.IO;
using NetP3DLib.P3D.Attributes;
using NetP3DLib.P3D.Enums;
using NetP3DLib.P3D.Extensions;
using System;
using System.Collections.Generic;
using System.ComponentModel;

namespace NetP3DLib.P3D.Chunks;

[ChunkAttributes(ChunkID)]
public class FrontendPageChunk : NamedChunk
{
    public const ChunkIdentifier ChunkID = ChunkIdentifier.Frontend_Page;

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
    

    public override byte[] DataBytes
    {
        get
        {
            var data = new List<byte>((int)DataLength);

            data.AddRange(BinaryExtensions.GetP3DStringBytes(Name));
            data.AddRange(BitConverter.GetBytes(Version));
            data.AddRange(BitConverter.GetBytes(ResolutionX));
            data.AddRange(BitConverter.GetBytes(ResolutionY));

            return [.. data];
        }
    }
    public override uint DataLength => BinaryExtensions.GetP3DStringLength(Name) + sizeof(uint) + sizeof(uint) + sizeof(uint);

    public FrontendPageChunk(EndianAwareBinaryReader br) : this(br.ReadP3DString(), br.ReadUInt32(), br.ReadUInt32(), br.ReadUInt32())
    {
    }

    public FrontendPageChunk(string name, uint version, uint resolutionX, uint resolutionY) : base(ChunkID, name)
    {
        _version = version;
        _resolutionX = resolutionX;
        _resolutionY = resolutionY;
    }

    protected override void WriteData(EndianAwareBinaryWriter bw)
    {
        bw.WriteP3DString(Name);
        bw.Write(Version);
        bw.Write(ResolutionX);
        bw.Write(ResolutionY);
    }

    protected override Chunk CloneSelf() => new FrontendPageChunk(Name, Version, ResolutionX, ResolutionY);
}
