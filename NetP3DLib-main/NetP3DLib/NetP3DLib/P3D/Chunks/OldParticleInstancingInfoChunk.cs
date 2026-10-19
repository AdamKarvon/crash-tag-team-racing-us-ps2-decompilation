using NetP3DLib.IO;
using NetP3DLib.P3D.Attributes;
using NetP3DLib.P3D.Enums;
using System;
using System.Collections.Generic;
using System.ComponentModel;

namespace NetP3DLib.P3D.Chunks;

[ChunkAttributes(ChunkID)]
public class OldParticleInstancingInfoChunk : Chunk
{
    public const ChunkIdentifier ChunkID = ChunkIdentifier.Old_Particle_Instancing_Info;

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
    
    private uint _maxInstances;
    public uint MaxInstances
    {
        get => _maxInstances;
        set
        {
            if (_maxInstances == value)
                return;
    
            _maxInstances = value;
            OnPropertyChanged(nameof(MaxInstances));
        }
    }
    

    public override byte[] DataBytes
    {
        get
        {
            var data = new List<byte>((int)DataLength);

            data.AddRange(BitConverter.GetBytes(Version));
            data.AddRange(BitConverter.GetBytes(MaxInstances));

            return [.. data];
        }
    }
    public override uint DataLength => sizeof(uint) + sizeof(uint);

    public OldParticleInstancingInfoChunk(EndianAwareBinaryReader br) : this(br.ReadUInt32(), br.ReadUInt32())
    {
    }

    public OldParticleInstancingInfoChunk(uint version, uint maxInstances) : base(ChunkID)
    {
        _version = version;
        _maxInstances = maxInstances;
    }

    protected override void WriteData(EndianAwareBinaryWriter bw)
    {
        bw.Write(Version);
        bw.Write(MaxInstances);
    }

    protected override Chunk CloneSelf() => new OldParticleInstancingInfoChunk(Version, MaxInstances);
}
