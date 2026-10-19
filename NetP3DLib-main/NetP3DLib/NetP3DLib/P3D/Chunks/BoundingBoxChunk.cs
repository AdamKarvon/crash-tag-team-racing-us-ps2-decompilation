using NetP3DLib.IO;
using NetP3DLib.P3D.Attributes;
using NetP3DLib.P3D.Enums;
using NetP3DLib.P3D.Extensions;
using System.Collections.Generic;
using System.Numerics;

namespace NetP3DLib.P3D.Chunks;

[ChunkAttributes(ChunkID)]
public class BoundingBoxChunk : Chunk
{
    public const ChunkIdentifier ChunkID = ChunkIdentifier.Bounding_Box;

    private Vector3 _low;
    public Vector3 Low
    {
        get => _low;
        set
        {
            if (_low == value)
                return;
    
            _low = value;
            OnPropertyChanged(nameof(Low));
        }
    }
    
    private Vector3 _high;
    public Vector3 High
    {
        get => _high;
        set
        {
            if (_high == value)
                return;
    
            _high = value;
            OnPropertyChanged(nameof(High));
        }
    }
    

    public override byte[] DataBytes
    {
        get
        {
            var data = new List<byte>((int)DataLength);

            data.AddRange(BinaryExtensions.GetBytes(Low));
            data.AddRange(BinaryExtensions.GetBytes(High));

            return [.. data];
        }
    }
    public override uint DataLength => sizeof(float) * 3 + sizeof(float) * 3;

    public BoundingBoxChunk(EndianAwareBinaryReader br) : this(br.ReadVector3(), br.ReadVector3())
    {
    }

    public BoundingBoxChunk(Vector3 low, Vector3 high) : base(ChunkID)
    {
        _low = low;
        _high = high;
    }

    protected override void WriteData(EndianAwareBinaryWriter bw)
    {
        bw.Write(Low);
        bw.Write(High);
    }

    protected override Chunk CloneSelf() => new BoundingBoxChunk(Low, High);
}
