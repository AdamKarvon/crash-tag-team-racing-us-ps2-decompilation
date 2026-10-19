using NetP3DLib.IO;
using NetP3DLib.P3D.Attributes;
using NetP3DLib.P3D.Enums;
using NetP3DLib.P3D.Extensions;
using System;
using System.Collections.Generic;

namespace NetP3DLib.P3D.Chunks;

[ChunkAttributes(ChunkID)]
public class ScenegraphBranchChunk : NamedChunk
{
    public const ChunkIdentifier ChunkID = ChunkIdentifier.Scenegraph_Branch;

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
    
    public uint NumChildren => (uint)Children.Count;

    public override byte[] DataBytes
    {
        get
        {
            var data = new List<byte>((int)DataLength);

            data.AddRange(BitConverter.GetBytes(Version));
            data.AddRange(BinaryExtensions.GetP3DStringBytes(Name));
            data.AddRange(BitConverter.GetBytes(NumChildren));

            return [.. data];
        }
    }
    public override uint DataLength => sizeof(uint) + BinaryExtensions.GetP3DStringLength(Name) + sizeof(uint);

    [System.Diagnostics.CodeAnalysis.SuppressMessage("Style", "IDE0059:Unnecessary assignment of a value", Justification = "We want to read the value to progress the BinaryReader, but not set the value anywhere because it's calculated dynamically.")]
    public ScenegraphBranchChunk(EndianAwareBinaryReader br) : this(br.ReadUInt32(), br.ReadP3DString())
    {
        var numChildren = br.ReadUInt32();
    }

    public ScenegraphBranchChunk(uint version, string name) : base(ChunkID, name)
    {
        _version = version;
    }

    protected override void WriteData(EndianAwareBinaryWriter bw)
    {
        bw.Write(Version);
        bw.WriteP3DString(Name);
        bw.Write(NumChildren);
    }

    protected override Chunk CloneSelf() => new ScenegraphBranchChunk(Version, Name);
}
