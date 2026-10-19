using NetP3DLib.IO;
using NetP3DLib.P3D.Attributes;
using NetP3DLib.P3D.Enums;
using NetP3DLib.P3D.Extensions;
using System;
using System.Collections.Generic;
using System.ComponentModel;
using System.Numerics;

namespace NetP3DLib.P3D.Chunks;

[ChunkAttributes(ChunkID)]
public class OldLocatorChunk : NamedChunk
{
    public const ChunkIdentifier ChunkID = ChunkIdentifier.Old_Locator;

    private uint _version;
    [DefaultValue(257)]
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
    
    private Vector3 _position;
    public Vector3 Position
    {
        get => _position;
        set
        {
            if (_position == value)
                return;
    
            _position = value;
            OnPropertyChanged(nameof(Position));
        }
    }
    

    public override byte[] DataBytes
    {
        get
        {
            var data = new List<byte>((int)DataLength);

            data.AddRange(BinaryExtensions.GetP3DStringBytes(Name));
            data.AddRange(BitConverter.GetBytes(Version));
            data.AddRange(BinaryExtensions.GetBytes(Position));

            return [.. data];
        }
    }
    public override uint DataLength => BinaryExtensions.GetP3DStringLength(Name) + sizeof(uint) + sizeof(float) * 3;

    public OldLocatorChunk(EndianAwareBinaryReader br) : this(br.ReadP3DString(), br.ReadUInt32(), br.ReadVector3())
    {
    }

    public OldLocatorChunk(string name, uint version, Vector3 position) : base(ChunkID, name)
    {
        _version = version;
        _position = position;
    }

    protected override void WriteData(EndianAwareBinaryWriter bw)
    {
        bw.WriteP3DString(Name);
        bw.Write(Version);
        bw.Write(Position);
    }

    protected override Chunk CloneSelf() => new OldLocatorChunk(Name, Version, Position);
}
