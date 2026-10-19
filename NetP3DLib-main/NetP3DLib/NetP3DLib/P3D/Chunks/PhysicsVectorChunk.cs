using NetP3DLib.IO;
using NetP3DLib.P3D.Attributes;
using NetP3DLib.P3D.Enums;
using NetP3DLib.P3D.Extensions;
using System.Collections.Generic;
using System.Numerics;

namespace NetP3DLib.P3D.Chunks;

[ChunkAttributes(ChunkID)]
public class PhysicsVectorChunk : Chunk
{
    public const ChunkIdentifier ChunkID = ChunkIdentifier.Physics_Vector;

    private Vector3 _vector;
    public Vector3 Vector
    {
        get => _vector;
        set
        {
            if (_vector == value)
                return;
    
            _vector = value;
            OnPropertyChanged(nameof(Vector));
        }
    }
    

    public override byte[] DataBytes
    {
        get
        {
            var data = new List<byte>((int)DataLength);

            data.AddRange(BinaryExtensions.GetBytes(Vector));

            return [.. data];
        }
    }
    public override uint DataLength => sizeof(float) * 3;

    public PhysicsVectorChunk(EndianAwareBinaryReader br) : this(br.ReadVector3())
    {
    }

    public PhysicsVectorChunk(Vector3 vector) : base(ChunkID)
    {
        _vector = vector;
    }

    protected override void WriteData(EndianAwareBinaryWriter bw)
    {
        bw.Write(Vector);
    }

    protected override Chunk CloneSelf() => new PhysicsVectorChunk(Vector);
}
