using NetP3DLib.IO;
using NetP3DLib.P3D.Attributes;
using NetP3DLib.P3D.Enums;
using NetP3DLib.P3D.Extensions;
using System.Collections.Generic;
using System.Numerics;

namespace NetP3DLib.P3D.Chunks;

[ChunkAttributes(ChunkID)]
public class LocatorMatrixChunk : Chunk
{
    public const ChunkIdentifier ChunkID = ChunkIdentifier.Locator_Matrix;

    private Matrix4x4 _matrix;
    public Matrix4x4 Matrix
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
    public override uint DataLength => sizeof(float) * 16;

    public LocatorMatrixChunk(EndianAwareBinaryReader br) : this(br.ReadMatrix4x4())
    {
    }

    public LocatorMatrixChunk(Matrix4x4 matrix) : base(ChunkID)
    {
        _matrix = matrix;
    }

    protected override void WriteData(EndianAwareBinaryWriter bw)
    {
        bw.Write(Matrix);
    }

    protected override Chunk CloneSelf() => new LocatorMatrixChunk(Matrix);
}
