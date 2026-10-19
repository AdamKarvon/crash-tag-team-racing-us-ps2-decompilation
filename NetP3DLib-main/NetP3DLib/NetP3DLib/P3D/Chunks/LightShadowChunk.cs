using NetP3DLib.IO;
using NetP3DLib.P3D.Attributes;
using NetP3DLib.P3D.Enums;
using System;
using System.Collections.Generic;

namespace NetP3DLib.P3D.Chunks;

[ChunkAttributes(ChunkID)]
public class LightShadowChunk : Chunk
{
    public const ChunkIdentifier ChunkID = ChunkIdentifier.Light_Shadow;

    private uint _shadow;
    public bool Shadow
    {
        get => _shadow == 1;
        set
        {
            if (Shadow == value)
                return;

            _shadow = value ? 1u : 0u;
            OnPropertyChanged(nameof(Shadow));
        }
    }

    public override byte[] DataBytes
    {
        get
        {
            var data = new List<byte>((int)DataLength);

            data.AddRange(BitConverter.GetBytes(_shadow));

            return [.. data];
        }
    }
    public override uint DataLength => sizeof(uint);

    public LightShadowChunk(EndianAwareBinaryReader br) : this(br.ReadUInt32())
    {
    }

    public LightShadowChunk(bool shadow) : this(shadow ? 1u : 0u)
    {
    }

    public LightShadowChunk(uint shadow) : base(ChunkID)
    {
        _shadow = shadow;
    }

    protected override void WriteData(EndianAwareBinaryWriter bw)
    {
        bw.Write(_shadow);
    }

    protected override Chunk CloneSelf() => new LightShadowChunk(Shadow);
}