using NetP3DLib.IO;
using NetP3DLib.P3D.Attributes;
using NetP3DLib.P3D.Enums;
using System;
using System.Collections.Generic;

namespace NetP3DLib.P3D.Chunks;

[ChunkAttributes(ChunkID)]
public class CollisionMetaDataChunk : Chunk
{
    public const ChunkIdentifier ChunkID = ChunkIdentifier.Collision_Meta_Data;

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
    
    public uint NumChannels => (uint)Children.Count; // TODO: Potentially calculate from channel chunks specifically

    public override byte[] DataBytes
    {
        get
        {
            var data = new List<byte>((int)DataLength);

            data.AddRange(BitConverter.GetBytes(Version));
            data.AddRange(BitConverter.GetBytes(NumChannels));

            return [.. data];
        }
    }
    public override uint DataLength => sizeof(uint) + sizeof(uint);

    [System.Diagnostics.CodeAnalysis.SuppressMessage("Style", "IDE0059:Unnecessary assignment of a value", Justification = "We want to read the value to progress the BinaryReader, but not set the value anywhere because it's calculated dynamically.")]
    public CollisionMetaDataChunk(EndianAwareBinaryReader br) : this(br.ReadUInt32())
    {
        var numChannels = br.ReadUInt32();
    }

    public CollisionMetaDataChunk(uint version) : base(ChunkID)
    {
        _version = version;
    }

    protected override void WriteData(EndianAwareBinaryWriter bw)
    {
        bw.Write(Version);
        bw.Write(NumChannels);
    }

    protected override Chunk CloneSelf() => new CollisionMetaDataChunk(Version);
}
