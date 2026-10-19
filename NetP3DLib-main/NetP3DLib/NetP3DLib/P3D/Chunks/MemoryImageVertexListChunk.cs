using NetP3DLib.IO;
using NetP3DLib.P3D.Attributes;
using NetP3DLib.P3D.Collections;
using NetP3DLib.P3D.Enums;
using NetP3DLib.P3D.Extensions;
using System;
using System.Collections.Generic;

namespace NetP3DLib.P3D.Chunks;

[ChunkAttributes(ChunkID)]
public class MemoryImageVertexListChunk : Chunk
{
    public const ChunkIdentifier ChunkID = ChunkIdentifier.Memory_Image_Vertex_List;

    private uint _version;
    public uint Version
    {
        get => _version;
        set
        {
            if (_version == value)
                return;
    
            _version = value;
            OnPropertyChanged(nameof(Version));
        }
    }
    
    private uint _param;
    public uint Param
    {
        get => _param;
        set
        {
            if (_param == value)
                return;
    
            _param = value;
            OnPropertyChanged(nameof(Param));
        }
    }
    
    public uint VertexSize
    {
        get => (uint)(Vertex?.Count ?? 0);
        set
        {
            if (value == VertexSize)
                return;

            if (value < VertexSize)
            {
                Vertex.RemoveRange((int)value, (int)(VertexSize - value));
            }
            else
            {
                int count = (int)(value - VertexSize);
                var newVertices = new byte[count];

                for (var i = 0; i < count; i++)
                    newVertices[i] = default;

                Vertex.AddRange(newVertices);
            }
        }
    }
    public SizeAwareList<byte> Vertex { get; }

    public override byte[] DataBytes
    {
        get
        {
            var data = new List<byte>((int)DataLength);

            data.AddRange(BitConverter.GetBytes(Version));
            data.AddRange(BitConverter.GetBytes(Param));
            data.AddRange(BitConverter.GetBytes(VertexSize));
            data.AddRange(Vertex);

            return [.. data];
        }
    }
    public override uint DataLength => sizeof(uint) + sizeof(uint) + sizeof(uint) + VertexSize;

    public MemoryImageVertexListChunk(EndianAwareBinaryReader br) : this(br.ReadUInt32(), br.ReadUInt32(), br.ReadByteArray(out _))
    {
    }

    public MemoryImageVertexListChunk(uint version, uint param, IList<byte> vertex) : base(ChunkID)
    {
        _version = version;
        _param = param;
        Vertex = CreateSizeAwareList(vertex, Vertex_CollectionChanged);
    }
    
    private void Vertex_CollectionChanged(object sender, System.Collections.Specialized.NotifyCollectionChangedEventArgs e) => OnPropertyChanged(nameof(Vertex));

    protected override void WriteData(EndianAwareBinaryWriter bw)
    {
        bw.Write(Version);
        bw.Write(Param);
        bw.Write(VertexSize);
        bw.Write([.. Vertex]);
    }

    protected override Chunk CloneSelf() => new MemoryImageVertexListChunk(Version, Param, Vertex);
}
