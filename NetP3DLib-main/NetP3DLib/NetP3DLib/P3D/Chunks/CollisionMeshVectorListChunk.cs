using NetP3DLib.IO;
using NetP3DLib.P3D.Attributes;
using NetP3DLib.P3D.Collections;
using NetP3DLib.P3D.Enums;
using NetP3DLib.P3D.Extensions;
using System;
using System.Collections.Generic;
using System.Numerics;

namespace NetP3DLib.P3D.Chunks;

[ChunkAttributes(ChunkID)]
public class CollisionMeshVectorListChunk : Chunk
{
    public const ChunkIdentifier ChunkID = ChunkIdentifier.Collision_Mesh_Vector_list;

    public uint NumVectors
    {
        get => (uint)(Vectors?.Count ?? 0);
        set
        {
            if (value == NumVectors)
                return;

            if (value < NumVectors)
            {
                Vectors.RemoveRange((int)value, (int)(NumVectors - value));
            }
            else
            {
                int count = (int)(value - NumVectors);
                var newVectors = new Vector3[count];

                for (var i = 0; i < count; i++)
                    newVectors[i] = default;

                Vectors.AddRange(newVectors);
            }
        }
    }
    public SizeAwareList<Vector3> Vectors { get; }

    public override byte[] DataBytes
    {
        get
        {
            var data = new List<byte>((int)DataLength);

            data.AddRange(BitConverter.GetBytes(NumVectors));
            foreach (var uv in Vectors)
                data.AddRange(BinaryExtensions.GetBytes(uv));

            return [.. data];
        }
    }
    public override uint DataLength => sizeof(uint) + sizeof(float) * 3 * NumVectors;

    public CollisionMeshVectorListChunk(EndianAwareBinaryReader br) : this(br.ReadVector3Array(out _))
    {
    }

    public CollisionMeshVectorListChunk(IList<Vector3> vectors) : base(ChunkID)
    {
        Vectors = CreateSizeAwareList(vectors, Vectors_CollectionChanged);
    }
    
    private void Vectors_CollectionChanged(object sender, System.Collections.Specialized.NotifyCollectionChangedEventArgs e) => OnPropertyChanged(nameof(Vectors));

    protected override void WriteData(EndianAwareBinaryWriter bw)
    {
        bw.Write(NumVectors);
        foreach (var pos in Vectors)
            bw.Write(pos);
    }

    protected override Chunk CloneSelf() => new CollisionMeshVectorListChunk(Vectors);
}
