using NetP3DLib.IO;
using NetP3DLib.P3D.Attributes;
using NetP3DLib.P3D.Enums;
using NetP3DLib.P3D.Extensions;
using System;
using System.Collections.Generic;

namespace NetP3DLib.P3D.Chunks;

[ChunkAttributes(ChunkID)]
public class WorldCollisionObjectChunk : NamedChunk
{
    public const ChunkIdentifier ChunkID = ChunkIdentifier.World_Collision_Object;

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
    

    public override byte[] DataBytes
    {
        get
        {
            var data = new List<byte>((int)DataLength);

            data.AddRange(BinaryExtensions.GetP3DStringBytes(Name));
            data.AddRange(BitConverter.GetBytes(Version));

            return [.. data];
        }
    }
    public override uint DataLength => BinaryExtensions.GetP3DStringLength(Name) + sizeof(uint);

    public WorldCollisionObjectChunk(EndianAwareBinaryReader br) : this(br.ReadP3DString(), br.ReadUInt32())
    {
    }

    public WorldCollisionObjectChunk(string name, uint version) : base(ChunkID, name)
    {
        _version = version;
    }

    protected override void WriteData(EndianAwareBinaryWriter bw)
    {
        bw.WriteP3DString(Name);
        bw.Write(Version);
    }

    protected override Chunk CloneSelf() => new WorldCollisionObjectChunk(Name, Version);
}
