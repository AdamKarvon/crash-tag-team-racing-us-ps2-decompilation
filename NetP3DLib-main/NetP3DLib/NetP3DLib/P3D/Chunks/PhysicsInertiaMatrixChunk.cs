using NetP3DLib.IO;
using NetP3DLib.Numerics;
using NetP3DLib.P3D.Attributes;
using NetP3DLib.P3D.Enums;
using NetP3DLib.P3D.Extensions;
using System.Collections.Generic;

namespace NetP3DLib.P3D.Chunks;

[ChunkAttributes(ChunkID)]
public class PhysicsInertiaMatrixChunk : Chunk
{
    public const ChunkIdentifier ChunkID = ChunkIdentifier.Physics_Inertia_Matrix;

    private SymmetricMatrix3x3 _matrix;
    public SymmetricMatrix3x3 Matrix
    {
        get => _matrix;
        set
        {
            if (_matrix == value)
                return;
    
            _matrix = value;
            OnPropertyChanged(nameof(Matrix));
        }
    }
    

    public override byte[] DataBytes
    {
        get
        {
            var data = new List<byte>((int)DataLength);

            data.AddRange(BinaryExtensions.GetBytes(Matrix));

            return [.. data];
        }
    }
    public override uint DataLength => SymmetricMatrix3x3.SizeInBytes;

    public PhysicsInertiaMatrixChunk(EndianAwareBinaryReader br) : this(br.ReadSymmetricMatrix3x3())
    {
    }

    public PhysicsInertiaMatrixChunk(SymmetricMatrix3x3 matrix) : base(ChunkID)
    {
        _matrix = matrix;
    }

    protected override void WriteData(EndianAwareBinaryWriter bw)
    {
        bw.Write(Matrix);
    }

    protected override Chunk CloneSelf() => new PhysicsInertiaMatrixChunk(Matrix);
}
