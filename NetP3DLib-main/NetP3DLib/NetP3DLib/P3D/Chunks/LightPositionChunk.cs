using NetP3DLib.IO;
using NetP3DLib.P3D.Attributes;
using NetP3DLib.P3D.Enums;
using NetP3DLib.P3D.Extensions;
using System.Collections.Generic;
using System.Numerics;

namespace NetP3DLib.P3D.Chunks;

[ChunkAttributes(ChunkID)]
public class LightPositionChunk : Chunk
{
    public const ChunkIdentifier ChunkID = ChunkIdentifier.Light_Position;

    private Vector3 _position;
    public Vector3 Position
    {
        get => _position;
        set
        {
            if (_position == value)
                return;
    
            _position = value;
            OnPropertyChanged(nameof(Position));
        }
    }
    

    public override byte[] DataBytes
    {
        get
        {
            var data = new List<byte>((int)DataLength);

            data.AddRange(BinaryExtensions.GetBytes(Position));

            return [.. data];
        }
    }
    public override uint DataLength => sizeof(float) * 3;

    public LightPositionChunk(EndianAwareBinaryReader br) : this(br.ReadVector3())
    {
    }

    public LightPositionChunk(Vector3 position) : base(ChunkID)
    {
        _position = position;
    }

    protected override void WriteData(EndianAwareBinaryWriter bw)
    {
        bw.Write(Position);
    }

    protected override Chunk CloneSelf() => new LightPositionChunk(Position);
}
