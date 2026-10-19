using NetP3DLib.IO;
using NetP3DLib.P3D.Attributes;
using NetP3DLib.P3D.Enums;
using NetP3DLib.P3D.Extensions;
using System;
using System.Collections.Generic;
using System.ComponentModel;

namespace NetP3DLib.P3D.Chunks;

[ChunkAttributes(ChunkID)]
public class WorldSphereChunk : NamedChunk
{
    public const ChunkIdentifier ChunkID = ChunkIdentifier.World_Sphere;

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
    
    public uint NumMeshes => GetChildCount(ChunkIdentifier.Mesh);
    public uint NumOldBillboardQuadGroups => GetChildCount(ChunkIdentifier.Old_Billboard_Quad_Group);

    public override byte[] DataBytes
    {
        get
        {
            var data = new List<byte>((int)DataLength);

            data.AddRange(BinaryExtensions.GetP3DStringBytes(Name));
            data.AddRange(BitConverter.GetBytes(Version));
            data.AddRange(BitConverter.GetBytes(NumMeshes));
            data.AddRange(BitConverter.GetBytes(NumOldBillboardQuadGroups));

            return [.. data];
        }
    }
    public override uint DataLength => BinaryExtensions.GetP3DStringLength(Name) + sizeof(uint) + sizeof(uint) + sizeof(uint);

    [System.Diagnostics.CodeAnalysis.SuppressMessage("Style", "IDE0059:Unnecessary assignment of a value", Justification = "We want to read the value to progress the BinaryReader, but not set the value anywhere because it's calculated dynamically.")]
    public WorldSphereChunk(EndianAwareBinaryReader br) : this(br.ReadP3DString(), br.ReadUInt32())
    {
        var numMeshes = br.ReadUInt32();
        var numOldBillboardQuadGroups = br.ReadUInt32();
    }

    public WorldSphereChunk(string name, uint version) : base(ChunkID, name)
    {
        _version = version;
    }

    protected override void WriteData(EndianAwareBinaryWriter bw)
    {
        bw.WriteP3DString(Name);
        bw.Write(Version);
        bw.Write(NumMeshes);
        bw.Write(NumOldBillboardQuadGroups);
    }

    protected override Chunk CloneSelf() => new WorldSphereChunk(Name, Version);
}
