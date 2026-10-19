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
public class ParticleSystemChunk : NamedChunk
{
    public const ChunkIdentifier ChunkID = ChunkIdentifier.Particle_System;

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

    private readonly P3DString _factoryName;
    public string FactoryName
    {
        get => _factoryName?.Value ?? string.Empty;
        set => _factoryName.Value = value;
    }

    public override byte[] DataBytes
    {
        get
        {
            var data = new List<byte>((int)DataLength);

            data.AddRange(BitConverter.GetBytes(Version));
            data.AddRange(BinaryExtensions.GetP3DStringBytes(Name));
            data.AddRange(BinaryExtensions.GetP3DStringBytes(FactoryName));

            return [.. data];
        }
    }
    public override uint DataLength => sizeof(uint) + BinaryExtensions.GetP3DStringLength(Name) + BinaryExtensions.GetP3DStringLength(FactoryName);

    public ParticleSystemChunk(EndianAwareBinaryReader br) : this(br.ReadUInt32(), br.ReadP3DString(), br.ReadP3DString())
    {
    }

    public ParticleSystemChunk(uint version, string name, string factoryName) : base(ChunkID, name)
    {
        _version = version;
        _factoryName = new(this, factoryName, nameof(FactoryName));
    }

    public override IEnumerable<InvalidP3DException> ValidateChunk()
    {
        foreach (var error in base.ValidateChunk())
            yield return error;

        if (!FactoryName.IsValidP3DString())
            yield return new InvalidP3DStringException(this, nameof(FactoryName), FactoryName);

        if ((ParentChunk != null || ParentFile != null) && FindNamedChunkInParentHierarchy<ParticleSystemFactoryChunk>(FactoryName) == null)
            yield return new InvalidP3DException(this, $"Could not find factory with name \"{FactoryName}\" in the parent hierarchy.");
    }

    protected override void WriteData(EndianAwareBinaryWriter bw)
    {
        bw.Write(Version);
        bw.WriteP3DString(Name);
        bw.WriteP3DString(FactoryName);
    }

    protected override Chunk CloneSelf() => new ParticleSystemChunk(Version, Name, FactoryName);
}
