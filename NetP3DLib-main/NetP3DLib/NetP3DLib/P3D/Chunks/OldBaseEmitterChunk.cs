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
public class OldBaseEmitterChunk : NamedChunk
{
    public const ChunkIdentifier ChunkID = ChunkIdentifier.Old_Base_Emitter;

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
    
    private readonly FourCC _particleType;
    [MaxLength(4)]
    public string ParticleType
    {
        get => _particleType?.Value ?? string.Empty;
        set => _particleType.Value = value;
    }
    private readonly FourCC _generatorType;
    [MaxLength(4)]
    public string GeneratorType
    {
        get => _generatorType?.Value ?? string.Empty;
        set => _generatorType.Value = value;
    }
    private uint _zTest;
    public bool ZTest
    {
        get => _zTest == 1;
        set
        {
            if (ZTest == value)
                return;

            _zTest = value ? 1u : 0u;
            OnPropertyChanged(nameof(ZTest));
        }
    }
    private uint _zWrite;
    public bool ZWrite
    {
        get => _zWrite == 1;
        set
        {
            if (ZWrite == value)
                return;

            _zWrite = value ? 1u : 0u;
            OnPropertyChanged(nameof(ZWrite));
        }
    }
    private uint _fog;
    public bool Fog
    {
        get => _fog == 1;
        set
        {
            if (Fog == value)
                return;

            _fog = value ? 1u : 0u;
            OnPropertyChanged(nameof(Fog));
        }
    }
    
    private uint _maxParticles;
    public uint MaxParticles
    {
        get => _maxParticles;
        set
        {
            if (_maxParticles == value)
                return;
    
            _maxParticles = value;
            OnPropertyChanged(nameof(MaxParticles));
        }
    }
    
    private uint _infiniteLife;
    public bool InfiniteLife
    {
        get => _infiniteLife == 1;
        set
        {
            if (InfiniteLife == value)
                return;

            _infiniteLife = value ? 1u : 0u;
            OnPropertyChanged(nameof(InfiniteLife));
        }
    }
    
    private float _rotationalCohesion;
    public float RotationalCohesion
    {
        get => _rotationalCohesion;
        set
        {
            if (_rotationalCohesion == value)
                return;
    
            _rotationalCohesion = value;
            OnPropertyChanged(nameof(RotationalCohesion));
        }
    }
    
    private float _translationCohesion;
    public float TranslationCohesion
    {
        get => _translationCohesion;
        set
        {
            if (_translationCohesion == value)
                return;
    
            _translationCohesion = value;
            OnPropertyChanged(nameof(TranslationCohesion));
        }
    }
    

    public override byte[] DataBytes
    {
        get
        {
            var data = new List<byte>((int)DataLength);

            data.AddRange(BitConverter.GetBytes(Version));
            data.AddRange(BinaryExtensions.GetP3DStringBytes(Name));
            data.AddRange(BinaryExtensions.GetFourCCBytes(ParticleType));
            data.AddRange(BinaryExtensions.GetFourCCBytes(GeneratorType));
            data.AddRange(BitConverter.GetBytes(_zTest));
            data.AddRange(BitConverter.GetBytes(_zWrite));
            data.AddRange(BitConverter.GetBytes(_fog));
            data.AddRange(BitConverter.GetBytes(MaxParticles));
            data.AddRange(BitConverter.GetBytes(_infiniteLife));
            data.AddRange(BitConverter.GetBytes(RotationalCohesion));
            data.AddRange(BitConverter.GetBytes(TranslationCohesion));

            return [.. data];
        }
    }
    public override uint DataLength => sizeof(uint) + BinaryExtensions.GetP3DStringLength(Name) + 4 + 4 + sizeof(uint) + sizeof(uint) + sizeof(uint) + sizeof(uint) + sizeof(uint) + sizeof(float) + sizeof(float);

    public OldBaseEmitterChunk(EndianAwareBinaryReader br) : this(br.ReadUInt32(), br.ReadP3DString(), br.ReadFourCC(), br.ReadFourCC(), br.ReadUInt32(), br.ReadUInt32(), br.ReadUInt32(), br.ReadUInt32(), br.ReadUInt32(), br.ReadSingle(), br.ReadSingle())
    {
    }

    public OldBaseEmitterChunk(uint version, string name, string particleType, string generatorType, bool zTest, bool zWrite, bool fog, uint maxParticles, bool infiniteLife, float rotationalCohesion, float translationCohesion) : this(version, name, particleType, generatorType, zTest ? 1u : 0u, zWrite ? 1u : 0u, fog ? 1u : 0u, maxParticles, infiniteLife ? 1u : 0u, rotationalCohesion, translationCohesion)
    {
    }

    public OldBaseEmitterChunk(uint version, string name, string particleType, string generatorType, uint zTest, uint zWrite, uint fog, uint maxParticles, uint infiniteLife, float rotationalCohesion, float translationCohesion) : base(ChunkID, name)
    {
        _version = version;
        _particleType = new(this, particleType, nameof(ParticleType));
        _generatorType = new(this, generatorType, nameof(GeneratorType));
        _zTest = zTest;
        _zWrite = zWrite;
        _fog = fog;
        _maxParticles = maxParticles;
        _infiniteLife = infiniteLife;
        _rotationalCohesion = rotationalCohesion;
        _translationCohesion = translationCohesion;
    }

    public override IEnumerable<InvalidP3DException> ValidateChunk()
    {
        foreach (var error in base.ValidateChunk())
            yield return error;

        if (!ParticleType.IsValidFourCC())
            yield return new InvalidP3DFourCCException(this, nameof(ParticleType), ParticleType);

        if (!GeneratorType.IsValidFourCC())
            yield return new InvalidP3DFourCCException(this, nameof(GeneratorType), GeneratorType);
    }

    protected override void WriteData(EndianAwareBinaryWriter bw)
    {
        bw.Write(Version);
        bw.WriteP3DString(Name);
        bw.WriteFourCC(ParticleType);
        bw.WriteFourCC(GeneratorType);
        bw.Write(_zTest);
        bw.Write(_zWrite);
        bw.Write(_fog);
        bw.Write(MaxParticles);
        bw.Write(_infiniteLife);
        bw.Write(RotationalCohesion);
        bw.Write(TranslationCohesion);
    }

    protected override Chunk CloneSelf() => new OldBaseEmitterChunk(Version, Name, ParticleType, GeneratorType, ZTest, ZWrite, Fog, MaxParticles, InfiniteLife, RotationalCohesion, TranslationCohesion);
}
