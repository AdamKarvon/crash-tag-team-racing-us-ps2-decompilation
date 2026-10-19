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
public class MultiControllerTrackChunk : NamedChunk
{
    public const ChunkIdentifier ChunkID = ChunkIdentifier.Multi_Controller_Track;

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
    
    private readonly FourCC _type;
    [MaxLength(4)]
    public string Type
    {
        get => _type?.Value ?? string.Empty;
        set => _type.Value = value;
    }

    public override byte[] DataBytes
    {
        get
        {
            var data = new List<byte>((int)DataLength);

            data.AddRange(BitConverter.GetBytes(Version));
            data.AddRange(BinaryExtensions.GetP3DStringBytes(Name));
            data.AddRange(BinaryExtensions.GetFourCCBytes(Type));

            return [.. data];
        }
    }
    public override uint DataLength => sizeof(uint) + BinaryExtensions.GetP3DStringLength(Name) + 4;

    public MultiControllerTrackChunk(EndianAwareBinaryReader br) : this(br.ReadUInt32(), br.ReadP3DString(), br.ReadFourCC())
    {
    }

    public MultiControllerTrackChunk(uint version, string name, string type) : base(ChunkID, name)
    {
        _version = version;
        _type = new(this, type, nameof(Type));
    }

    public override IEnumerable<InvalidP3DException> ValidateChunk()
    {
        foreach (var error in base.ValidateChunk())
            yield return error;

        if (!Type.IsValidFourCC())
            yield return new InvalidP3DFourCCException(this, nameof(Type), Type);
    }

    protected override void WriteData(EndianAwareBinaryWriter bw)
    {
        bw.Write(Version);
        bw.WriteP3DString(Name);
        bw.WriteFourCC(Type);
    }

    protected override Chunk CloneSelf() => new MultiControllerTrackChunk(Version, Name, Type);
}
