using NetP3DLib.IO;
using NetP3DLib.P3D.Attributes;
using NetP3DLib.P3D.Enums;
using NetP3DLib.P3D.Extensions;
using System;
using System.Collections.Generic;
using System.ComponentModel;

namespace NetP3DLib.P3D.Chunks;

[ChunkAttributes(ChunkID)]
public class DynaPhysChunk : NamedChunk
{
    public const ChunkIdentifier ChunkID = ChunkIdentifier.Dyna_Phys;

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
    
    private uint _hasAlpha;
    public bool HasAlpha
    {
        get => _hasAlpha != 0;
        set
        {
            if (HasAlpha == value)
                return;

            _hasAlpha = value ? 1u : 0u;
            OnPropertyChanged(nameof(HasAlpha));
        }
    }

    public override byte[] DataBytes
    {
        get
        {
            var data = new List<byte>((int)DataLength);

            data.AddRange(BinaryExtensions.GetP3DStringBytes(Name));
            data.AddRange(BitConverter.GetBytes(Version));
            data.AddRange(BitConverter.GetBytes(_hasAlpha));

            return [.. data];
        }
    }
    public override uint DataLength => BinaryExtensions.GetP3DStringLength(Name) + sizeof(uint) + sizeof(uint);

    public DynaPhysChunk(EndianAwareBinaryReader br) : this(br.ReadP3DString(), br.ReadUInt32(), br.ReadUInt32())
    {
    }

    public DynaPhysChunk(string name, uint version, bool hasAlpha) : this(name, version, hasAlpha ? 1u : 0u)
    {
    }

    public DynaPhysChunk(string name, uint version, uint hasAlpha) : base(ChunkID, name)
    {
        _version = version;
        _hasAlpha = hasAlpha;
    }

    protected override void WriteData(EndianAwareBinaryWriter bw)
    {
        bw.WriteP3DString(Name);
        bw.Write(Version);
        bw.Write(_hasAlpha);
    }

    protected override Chunk CloneSelf() => new DynaPhysChunk(Name, Version, HasAlpha);
}
