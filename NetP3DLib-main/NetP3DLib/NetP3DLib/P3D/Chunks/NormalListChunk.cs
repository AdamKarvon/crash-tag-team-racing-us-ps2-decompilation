using NetP3DLib.IO;
using NetP3DLib.P3D.Attributes;
using NetP3DLib.P3D.Collections;
using NetP3DLib.P3D.Enums;
using NetP3DLib.P3D.Exceptions;
using NetP3DLib.P3D.Extensions;
using System;
using System.Collections.Generic;
using System.Numerics;

namespace NetP3DLib.P3D.Chunks;

[ChunkAttributes(ChunkID)]
public class NormalListChunk : Chunk
{
    public const ChunkIdentifier ChunkID = ChunkIdentifier.Normal_List;

    public uint NumNormals
    {
        get => (uint)(Normals?.Count ?? 0);
        set
        {
            if (value == NumNormals)
                return;

            if (value < NumNormals)
            {
                Normals.RemoveRange((int)value, (int)(NumNormals - value));
            }
            else
            {
                int count = (int)(value - NumNormals);
                var newNormals = new Vector3[count];

                for (var i = 0; i < count; i++)
                    newNormals[i] = default;

                Normals.AddRange(newNormals);
            }
        }
    }
    public SizeAwareList<Vector3> Normals { get; }

    public override byte[] DataBytes
    {
        get
        {
            var data = new List<byte>((int)DataLength);

            data.AddRange(BitConverter.GetBytes(NumNormals));
            foreach (var normal in Normals)
                data.AddRange(BinaryExtensions.GetBytes(normal));

            return [.. data];
        }
    }
    public override uint DataLength => sizeof(uint) + sizeof(float) * 3 * NumNormals;

    public NormalListChunk(EndianAwareBinaryReader br) : this(br.ReadVector3Array(out _))
    {
    }

    public NormalListChunk(IList<Vector3> normals) : base(ChunkID)
    {
        Normals = CreateSizeAwareList(normals, Normals_CollectionChanged);
    }
    
    private void Normals_CollectionChanged(object sender, System.Collections.Specialized.NotifyCollectionChangedEventArgs e) => OnPropertyChanged(nameof(Normals));

    public override IEnumerable<InvalidP3DException> ValidateChunk()
    {
        foreach (var error in base.ValidateChunk())
            yield return error;

        if (ParentChunk is OldPrimitiveGroupChunk oldPrimitiveGroup && oldPrimitiveGroup.NumVertices != NumNormals)
            yield return new InvalidP3DException(this, $"Num Normals value {NumNormals} does not match parent Num Vertices value {oldPrimitiveGroup.NumVertices}.");
    }

    protected override void WriteData(EndianAwareBinaryWriter bw)
    {
        bw.Write(NumNormals);
        foreach (var pos in Normals)
            bw.Write(pos);
    }

    protected override Chunk CloneSelf() => new NormalListChunk(Normals);
}
