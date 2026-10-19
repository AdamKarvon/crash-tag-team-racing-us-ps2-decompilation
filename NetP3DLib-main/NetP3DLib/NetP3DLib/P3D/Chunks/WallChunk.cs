using NetP3DLib.IO;
using NetP3DLib.P3D.Attributes;
using NetP3DLib.P3D.Enums;
using NetP3DLib.P3D.Extensions;
using System.Collections.Generic;
using System.Numerics;

namespace NetP3DLib.P3D.Chunks;

[ChunkAttributes(ChunkID)]
public class WallChunk : Chunk
{
    public const ChunkIdentifier ChunkID = ChunkIdentifier.Wall;

    private Vector3 _start;
    public Vector3 Start
    {
        get => _start;
        set
        {
            if (_start == value)
                return;
    
            _start = value;
            OnPropertyChanged(nameof(Start));
        }
    }
    
    private Vector3 _end;
    public Vector3 End
    {
        get => _end;
        set
        {
            if (_end == value)
                return;
    
            _end = value;
            OnPropertyChanged(nameof(End));
        }
    }
    
    private Vector3 _normal;
    public Vector3 Normal
    {
        get => _normal;
        set
        {
            if (_normal == value)
                return;
    
            _normal = value;
            OnPropertyChanged(nameof(Normal));
        }
    }
    

    public override byte[] DataBytes
    {
        get
        {
            var data = new List<byte>((int)DataLength);

            data.AddRange(BinaryExtensions.GetBytes(Start));
            data.AddRange(BinaryExtensions.GetBytes(End));
            data.AddRange(BinaryExtensions.GetBytes(Normal));

            return [.. data];
        }
    }
    public override uint DataLength => sizeof(float) * 3 + sizeof(float) * 3 + sizeof(float) * 3;

    public WallChunk(EndianAwareBinaryReader br) : this(br.ReadVector3(), br.ReadVector3(), br.ReadVector3())
    {
    }

    public WallChunk(Vector3 start, Vector3 end, Vector3 normal) : base(ChunkID)
    {
        _start = start;
        _end = end;
        _normal = normal;
    }

    protected override void WriteData(EndianAwareBinaryWriter bw)
    {
        bw.Write(Start);
        bw.Write(End);
        bw.Write(Normal);
    }

    protected override Chunk CloneSelf() => new WallChunk(Start, End, Normal);
}
