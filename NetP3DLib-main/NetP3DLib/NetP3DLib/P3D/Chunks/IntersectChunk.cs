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
public class IntersectChunk : Chunk
{
    public const ChunkIdentifier ChunkID = ChunkIdentifier.Intersect;

    public uint NumIndices
    {
        get => (uint)(Indices?.Count ?? 0);
        set
        {
            if (value == NumIndices)
                return;

            if (value < NumIndices)
            {
                Indices.RemoveRange((int)value, (int)(NumIndices - value));
            }
            else
            {
                int count = (int)(value - NumIndices);
                var newIndices = new uint[count];

                for (var i = 0; i < count; i++)
                    newIndices[i] = default;

                Indices.AddRange(newIndices);
            }
        }
    }
    public SizeAwareList<uint> Indices { get; }
    public uint NumPositions
    {
        get => (uint)(Positions?.Count ?? 0);
        set
        {
            if (value == NumPositions)
                return;

            if (value < NumPositions)
            {
                Positions.RemoveRange((int)value, (int)(NumPositions - value));
            }
            else
            {
                int count = (int)(value - NumPositions);
                var newPositions = new Vector3[count];

                for (var i = 0; i < count; i++)
                    newPositions[i] = default;

                Positions.AddRange(newPositions);
            }
        }
    }
    public SizeAwareList<Vector3> Positions { get; }
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

            data.AddRange(BitConverter.GetBytes(NumIndices));
            foreach (var i in Indices)
                data.AddRange(BitConverter.GetBytes(i));
            data.AddRange(BitConverter.GetBytes(NumPositions));
            foreach (var pos in Positions)
                data.AddRange(BinaryExtensions.GetBytes(pos));
            data.AddRange(BitConverter.GetBytes(NumNormals));
            foreach (var normal in Normals)
                data.AddRange(BinaryExtensions.GetBytes(normal));

            return [.. data];
        }
    }
    public override uint DataLength => sizeof(uint) + sizeof(uint) * NumIndices + sizeof(uint) + sizeof(float) * 3 * NumPositions + sizeof(uint) + sizeof(float) * 3 * NumNormals;

    public IntersectChunk(EndianAwareBinaryReader br) : this(br.ReadUInt32Array(out _), br.ReadVector3Array(out _), br.ReadVector3Array(out _))
    {
    }

    public IntersectChunk(IList<uint> indices, IList<Vector3> positions, IList<Vector3> normals) : base(ChunkID)
    {
        Indices = CreateSizeAwareList(indices, Indices_CollectionChanged);
        Positions = CreateSizeAwareList(positions, Positions_CollectionChanged);
        Normals = CreateSizeAwareList(normals, Normals_CollectionChanged);
    }
    
    private void Indices_CollectionChanged(object sender, System.Collections.Specialized.NotifyCollectionChangedEventArgs e) => OnPropertyChanged(nameof(Indices));
    
    private void Positions_CollectionChanged(object sender, System.Collections.Specialized.NotifyCollectionChangedEventArgs e) => OnPropertyChanged(nameof(Positions));
    
    private void Normals_CollectionChanged(object sender, System.Collections.Specialized.NotifyCollectionChangedEventArgs e) => OnPropertyChanged(nameof(Normals));

    public override IEnumerable<InvalidP3DException> ValidateChunk()
    {
        foreach (var error in base.ValidateChunk())
            yield return error;

        if (Indices.Count % 3 != 0)
            yield return new InvalidP3DException(this, $"The number of {nameof(Indices)} must be divisible by 3.");

        for (var i = 0; i < Indices.Count; i++)
            if (Indices[i] >= Positions.Count)
                yield return new InvalidP3DException(this, $"The {nameof(Indices)} at index {i} is out of bound of the {nameof(Positions)} list.");

        var numTriangles = Indices.Count / 3;
        if (Normals.Count != numTriangles)
            yield return new InvalidP3DException(this, $"The number of {nameof(Normals)} does not match the number of triangles ({numTriangles}).");

        var terrainTypeListChunk = GetFirstChunkOfType<TerrainTypeListChunk>();
        if (terrainTypeListChunk != null && terrainTypeListChunk.NumTypes != NumNormals)
            yield return new InvalidP3DException(this, $"The number of {nameof(Normals)} does not match the number of terrain types ({terrainTypeListChunk.NumTypes}).");
    }

    protected override void WriteData(EndianAwareBinaryWriter bw)
    {
        bw.Write(NumIndices);
        foreach (var i in Indices)
            bw.Write(i);
        bw.Write(NumPositions);
        foreach (var pos in Positions)
            bw.Write(pos);
        bw.Write(NumNormals);
        foreach (var normal in Normals)
            bw.Write(normal);
    }

    protected override Chunk CloneSelf() => new IntersectChunk(Indices, Positions, Normals);
}
