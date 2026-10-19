using NetP3DLib.IO;
using NetP3DLib.P3D.Attributes;
using NetP3DLib.P3D.Enums;
using System;
using System.Collections.Generic;

namespace NetP3DLib.P3D.Chunks;

[ChunkAttributes(ChunkID)]
public class ParticlePointGeneratorChunk : Chunk
{
    public const ChunkIdentifier ChunkID = ChunkIdentifier.Particle_Point_Generator;

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
    
    private float _horizontalSpread;
    public float HorizontalSpread
    {
        get => _horizontalSpread;
        set
        {
            if (_horizontalSpread == value)
                return;
    
            _horizontalSpread = value;
            OnPropertyChanged(nameof(HorizontalSpread));
        }
    }
    
    private float _verticalSpread;
    public float VerticalSpread
    {
        get => _verticalSpread;
        set
        {
            if (_verticalSpread == value)
                return;
    
            _verticalSpread = value;
            OnPropertyChanged(nameof(VerticalSpread));
        }
    }
    
    private float _radialVar;
    public float RadialVar
    {
        get => _radialVar;
        set
        {
            if (_radialVar == value)
                return;
    
            _radialVar = value;
            OnPropertyChanged(nameof(RadialVar));
        }
    }
    

    public override byte[] DataBytes
    {
        get
        {
            var data = new List<byte>((int)DataLength);

            data.AddRange(BitConverter.GetBytes(Version));
            data.AddRange(BitConverter.GetBytes(HorizontalSpread));
            data.AddRange(BitConverter.GetBytes(VerticalSpread));
            data.AddRange(BitConverter.GetBytes(RadialVar));

            return [.. data];
        }
    }
    public override uint DataLength => sizeof(uint) + sizeof(float) + sizeof(float) + sizeof(float);

    public ParticlePointGeneratorChunk(EndianAwareBinaryReader br) : this(br.ReadUInt32(), br.ReadSingle(), br.ReadSingle(), br.ReadSingle())
    {
    }

    public ParticlePointGeneratorChunk(uint version, float horizontalSpread, float verticalSpread, float radialVar) : base(ChunkID)
    {
        _version = version;
        _horizontalSpread = horizontalSpread;
        _verticalSpread = verticalSpread;
        _radialVar = radialVar;
    }

    protected override void WriteData(EndianAwareBinaryWriter bw)
    {
        bw.Write(Version);
        bw.Write(HorizontalSpread);
        bw.Write(VerticalSpread);
        bw.Write(RadialVar);
    }

    protected override Chunk CloneSelf() => new ParticlePointGeneratorChunk(Version, HorizontalSpread, VerticalSpread, RadialVar);
}
