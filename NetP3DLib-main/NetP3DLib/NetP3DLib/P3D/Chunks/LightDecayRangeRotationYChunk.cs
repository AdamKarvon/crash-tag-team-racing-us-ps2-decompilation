using NetP3DLib.IO;
using NetP3DLib.P3D.Attributes;
using NetP3DLib.P3D.Enums;
using System;
using System.Collections.Generic;

namespace NetP3DLib.P3D.Chunks;

[ChunkAttributes(ChunkID)]
public class LightDecayRangeRotationYChunk : Chunk
{
    public const ChunkIdentifier ChunkID = ChunkIdentifier.Light_Decay_Range_Rotation_Y;

    private float _rotationY;
    public float RotationY
    {
        get => _rotationY;
        set
        {
            if (_rotationY == value)
                return;
    
            _rotationY = value;
            OnPropertyChanged(nameof(RotationY));
        }
    }
    

    public override byte[] DataBytes
    {
        get
        {
            var data = new List<byte>((int)DataLength);

            data.AddRange(BitConverter.GetBytes(RotationY));

            return [.. data];
        }
    }
    public override uint DataLength => sizeof(uint);

    public LightDecayRangeRotationYChunk(EndianAwareBinaryReader br) : this(br.ReadSingle())
    {
    }

    public LightDecayRangeRotationYChunk(float rotationY) : base(ChunkID)
    {
        _rotationY = rotationY;
    }

    protected override void WriteData(EndianAwareBinaryWriter bw)
    {
        bw.Write(RotationY);
    }

    protected override Chunk CloneSelf() => new LightDecayRangeRotationYChunk(RotationY);
}
