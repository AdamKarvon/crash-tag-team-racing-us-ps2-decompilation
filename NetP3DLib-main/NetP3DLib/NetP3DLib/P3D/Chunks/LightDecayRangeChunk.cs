using NetP3DLib.IO;
using NetP3DLib.P3D.Attributes;
using NetP3DLib.P3D.Enums;
using System;
using System.Collections.Generic;

namespace NetP3DLib.P3D.Chunks;

[ChunkAttributes(ChunkID)]
public class LightDecayRangeChunk : Chunk
{
    public const ChunkIdentifier ChunkID = ChunkIdentifier.Light_Decay_Range;

    public enum DecayRange : uint
    {
        NoDecay,
        Sphere,
        Cuboid,
        Ellipsoid,
    }

    private DecayRange _decayType;
    public DecayRange DecayType
    {
        get => _decayType;
        set
        {
            if (_decayType == value)
                return;
    
            _decayType = value;
            OnPropertyChanged(nameof(DecayType));
        }
    }
    

    public override byte[] DataBytes
    {
        get
        {
            var data = new List<byte>((int)DataLength);

            data.AddRange(BitConverter.GetBytes((uint)DecayType));

            return [.. data];
        }
    }
    public override uint DataLength => sizeof(uint);

    public LightDecayRangeChunk(EndianAwareBinaryReader br) : this((DecayRange)br.ReadUInt32())
    {
    }

    public LightDecayRangeChunk(DecayRange decayType) : base(ChunkID)
    {
        _decayType = decayType;
    }

    protected override void WriteData(EndianAwareBinaryWriter bw)
    {
        bw.Write((uint)DecayType);
    }

    protected override Chunk CloneSelf() => new LightDecayRangeChunk(DecayType);
}
