using NetP3DLib.IO;
using NetP3DLib.P3D.Attributes;
using NetP3DLib.P3D.Enums;
using System;
using System.Collections.Generic;

namespace NetP3DLib.P3D.Chunks;

[ChunkAttributes(ChunkID)]
public class InstParticleSystemChunk : Chunk
{
    public const ChunkIdentifier ChunkID = ChunkIdentifier.Inst_Particle_System;

    public enum ParticleTypes
    {
        Null = -1,
        Shrub = 3,
        Garbage = 4,
        OakTreeLeaves = 5,
        Mail = 6,
        PineTreeNeedles = 7,
        Stars = 8,
        SmokeSpray = 9,
        DirtSpray = 10,
        GrassSpray = 11,
        WaterSpray = 12,
        EngineSmokeLight = 13,
        EngineSmokeHeavy = 14,
        EngineSmokeMedium = 16,
        PowerBoxExplosion = 17,
        FrinksCarSpecialEffect = 18,
        FireSpray = 19,
        AlienCameraExplosion = 20,
        HoverBikeFlame = 21,
        CoconutsDroppingShort = 22,
        CoconutsDroppingTall = 23,
        ParkingMeter = 24,
        CarExplosion = 25,
        Popsicles = 26,
    }

    private ParticleTypes _particleType;
    public ParticleTypes ParticleType
    {
        get => _particleType;
        set
        {
            if (_particleType == value)
                return;
    
            _particleType = value;
            OnPropertyChanged(nameof(ParticleType));
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

            data.AddRange(BitConverter.GetBytes((int)ParticleType));
            data.AddRange(BitConverter.GetBytes(MaxInstances));

            return [.. data];
        }
    }
    public override uint DataLength => sizeof(int) + sizeof(uint);

    public InstParticleSystemChunk(EndianAwareBinaryReader br) : this((ParticleTypes)br.ReadInt32(), br.ReadUInt32())
    {
    }

    public InstParticleSystemChunk(ParticleTypes particleType, uint maxInstances) : base(ChunkID)
    {
        _particleType = particleType;
        _maxInstances = maxInstances;
    }

    protected override void WriteData(EndianAwareBinaryWriter bw)
    {
        bw.Write((int)ParticleType);
        bw.Write(MaxInstances);
    }

    protected override Chunk CloneSelf() => new InstParticleSystemChunk(ParticleType, MaxInstances);

    public override string ToString() => $"{ParticleType} ({GetChunkType(this)} (0x{ID:X}))";
}
