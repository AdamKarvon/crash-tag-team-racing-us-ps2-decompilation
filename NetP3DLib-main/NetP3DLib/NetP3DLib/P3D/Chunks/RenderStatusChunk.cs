using NetP3DLib.IO;
using NetP3DLib.P3D.Attributes;
using NetP3DLib.P3D.Enums;
using System;
using System.Collections.Generic;

namespace NetP3DLib.P3D.Chunks;

[ChunkAttributes(ChunkID)]
public class RenderStatusChunk : Chunk
{
    public const ChunkIdentifier ChunkID = ChunkIdentifier.Render_Status;

    private uint _castShadow;
    public bool CastShadow
    {
        get => _castShadow == 0;
        set
        {
            if (CastShadow == value)
                return;

            _castShadow = value ? 0u : 1u;
            OnPropertyChanged(nameof(CastShadow));
        }
    }

    public override byte[] DataBytes
    {
        get
        {
            var data = new List<byte>((int)DataLength);

            data.AddRange(BitConverter.GetBytes(_castShadow));

            return [.. data];
        }
    }
    public override uint DataLength => sizeof(uint);

    public RenderStatusChunk(EndianAwareBinaryReader br) : this(br.ReadUInt32())
    {
    }

    public RenderStatusChunk(bool castShadow) : this(castShadow ? 0u : 1u)
    {
    }

    public RenderStatusChunk(uint castShadow) : base(ChunkID)
    {
        _castShadow = castShadow;
    }

    protected override void WriteData(EndianAwareBinaryWriter bw)
    {
        bw.Write(_castShadow);
    }

    protected override Chunk CloneSelf() => new RenderStatusChunk(CastShadow);
}