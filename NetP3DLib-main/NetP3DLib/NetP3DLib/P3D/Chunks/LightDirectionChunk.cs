using NetP3DLib.IO;
using NetP3DLib.P3D.Attributes;
using NetP3DLib.P3D.Enums;
using NetP3DLib.P3D.Extensions;
using System.Collections.Generic;
using System.Numerics;

namespace NetP3DLib.P3D.Chunks;

[ChunkAttributes(ChunkID)]
public class LightDirectionChunk : Chunk
{
    public const ChunkIdentifier ChunkID = ChunkIdentifier.Light_Direction;

    private Vector3 _direction;
    public Vector3 Direction
    {
        get => _direction;
        set
        {
            if (_direction == value)
                return;
    
            _direction = value;
            OnPropertyChanged(nameof(Direction));
        }
    }
    

    public override byte[] DataBytes
    {
        get
        {
            var data = new List<byte>((int)DataLength);

            data.AddRange(BinaryExtensions.GetBytes(Direction));

            return [.. data];
        }
    }
    public override uint DataLength => sizeof(float) * 3;

    public LightDirectionChunk(EndianAwareBinaryReader br) : this(br.ReadVector3())
    {
    }

    public LightDirectionChunk(Vector3 direction) : base(ChunkID)
    {
        _direction = direction;
    }

    protected override void WriteData(EndianAwareBinaryWriter bw)
    {
        bw.Write(Direction);
    }

    protected override Chunk CloneSelf() => new LightDirectionChunk(Direction);
}
