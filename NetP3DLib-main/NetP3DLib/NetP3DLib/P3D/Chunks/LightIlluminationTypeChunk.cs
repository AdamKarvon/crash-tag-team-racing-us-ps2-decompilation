using NetP3DLib.IO;
using NetP3DLib.P3D.Attributes;
using NetP3DLib.P3D.Enums;
using System;
using System.Collections.Generic;

namespace NetP3DLib.P3D.Chunks;

[ChunkAttributes(ChunkID)]
public class LightIlluminationTypeChunk : Chunk
{
    public const ChunkIdentifier ChunkID = ChunkIdentifier.Light_Illumination_Type;

    public enum IlluminationTypes : uint
    {
        ZeroIlluminant = 1,
        NegativeIlluminant = 2,
        PositiveIlluminant = 3,
    }

    private IlluminationTypes _illuminationType;
    public IlluminationTypes IlluminationType
    {
        get => _illuminationType;
        set
        {
            if (_illuminationType == value)
                return;
    
            _illuminationType = value;
            OnPropertyChanged(nameof(IlluminationType));
        }
    }
    

    public override byte[] DataBytes
    {
        get
        {
            var data = new List<byte>((int)DataLength);

            data.AddRange(BitConverter.GetBytes((uint)IlluminationType));

            return [.. data];
        }
    }
    public override uint DataLength => sizeof(uint);

    public LightIlluminationTypeChunk(EndianAwareBinaryReader br) : this((IlluminationTypes)br.ReadUInt32())
    {
    }

    public LightIlluminationTypeChunk(IlluminationTypes illuminationType) : base(ChunkID)
    {
        _illuminationType = illuminationType;
    }

    protected override void WriteData(EndianAwareBinaryWriter bw)
    {
        bw.Write((uint)IlluminationType);
    }

    protected override Chunk CloneSelf() => new LightIlluminationTypeChunk(IlluminationType);
}
