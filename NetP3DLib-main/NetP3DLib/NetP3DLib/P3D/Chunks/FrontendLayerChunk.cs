using NetP3DLib.IO;
using NetP3DLib.P3D.Attributes;
using NetP3DLib.P3D.Enums;
using NetP3DLib.P3D.Exceptions;
using NetP3DLib.P3D.Extensions;
using System;
using System.Collections.Generic;
using System.ComponentModel;

namespace NetP3DLib.P3D.Chunks;

[ChunkAttributes(ChunkID)]
public class FrontendLayerChunk : NamedChunk
{
    public const ChunkIdentifier ChunkID = ChunkIdentifier.Frontend_Layer;

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
    
    private uint _visible;
    public bool Visible
    {
        get => _visible != 0;
        set
        {
            if (Visible == value)
                return;

            _visible = value ? 1u : 0u;
            OnPropertyChanged(nameof(Visible));
        }
    }
    private uint _editable;
    public bool Editable
    {
        get => _editable != 0;
        set
        {
            if (Editable == value)
                return;

            _editable = value ? 1u : 0u;
            OnPropertyChanged(nameof(Editable));
        }
    }
    
    private uint _alpha;
    public uint Alpha
    {
        get => _alpha;
        set
        {
            if (_alpha == value)
                return;
    
            _alpha = value;
            OnPropertyChanged(nameof(Alpha));
        }
    }
    

    public override byte[] DataBytes
    {
        get
        {
            var data = new List<byte>((int)DataLength);

            data.AddRange(BinaryExtensions.GetP3DStringBytes(Name));
            data.AddRange(BitConverter.GetBytes(Version));
            data.AddRange(BitConverter.GetBytes(_visible));
            data.AddRange(BitConverter.GetBytes(_editable));
            data.AddRange(BitConverter.GetBytes(Alpha));

            return [.. data];
        }
    }
    public override uint DataLength => BinaryExtensions.GetP3DStringLength(Name) + sizeof(uint) + sizeof(uint) + sizeof(uint) + sizeof(uint);

    public FrontendLayerChunk(EndianAwareBinaryReader br) : this(br.ReadP3DString(), br.ReadUInt32(), br.ReadUInt32(), br.ReadUInt32(), br.ReadUInt32())
    {
    }

    public FrontendLayerChunk(string name, uint version, bool visible, bool editable, uint alpha) : this(name, version, visible ? 1u : 0u, editable ? 1u : 0u, alpha)
    {
    }

    public FrontendLayerChunk(string name, uint version, uint visible, uint editable, uint alpha) : base(ChunkID, name)
    {
        _version = version;
        _visible = visible;
        _editable = editable;
        _alpha = alpha;
    }

    public override IEnumerable<InvalidP3DException> ValidateChunk()
    {
        foreach (var error in base.ValidateChunk())
            yield return error;

        if (Alpha > 256)
            yield return new InvalidP3DException(this, $"{nameof(Alpha)} must be between 0 and 256.");
    }

    protected override void WriteData(EndianAwareBinaryWriter bw)
    {
        bw.WriteP3DString(Name);
        bw.Write(Version);
        bw.Write(_visible);
        bw.Write(_editable);
        bw.Write(Alpha);
    }

    protected override Chunk CloneSelf() => new FrontendLayerChunk(Name, Version, Visible, Editable, Alpha);
}
