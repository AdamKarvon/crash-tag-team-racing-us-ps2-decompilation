using NetP3DLib.IO;
using NetP3DLib.P3D.Attributes;
using NetP3DLib.P3D.Collections;
using NetP3DLib.P3D.Enums;
using NetP3DLib.P3D.Extensions;
using System;
using System.Collections.Generic;

namespace NetP3DLib.P3D.Chunks;

[ChunkAttributes(ChunkID)]
public class MemoryImageIndexListChunk : Chunk
{
    public const ChunkIdentifier ChunkID = ChunkIdentifier.Memory_Image_Index_List;

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
                var newIndices = new ushort[count];

                for (var i = 0; i < count; i++)
                    newIndices[i] = default;

                Indices.AddRange(newIndices);
            }
        }
    }
    public SizeAwareList<ushort> Indices { get; }

    public override byte[] DataBytes
    {
        get
        {
            var data = new List<byte>((int)DataLength);

            data.AddRange(BitConverter.GetBytes(Version));
            data.AddRange(BitConverter.GetBytes(Param));
            data.AddRange(BitConverter.GetBytes(sizeof(ushort) * NumIndices));
            foreach (var index in Indices)
                data.AddRange(BitConverter.GetBytes(index));

            return [.. data];
        }
    }
    public override uint DataLength => sizeof(uint) + sizeof(uint) + sizeof(uint) + sizeof(ushort) * NumIndices;

    public MemoryImageIndexListChunk(EndianAwareBinaryReader br) : this(br.ReadUInt32(), br.ReadUInt32(), br.ReadUInt16Array(br.ReadInt32() / sizeof(ushort)))
    {
    }

    public MemoryImageIndexListChunk(uint version, uint param, IList<ushort> indices) : base(ChunkID)
    {
        _version = version;
        _param = param;
        Indices = CreateSizeAwareList(indices, Indices_CollectionChanged);
    }
    
    private void Indices_CollectionChanged(object sender, System.Collections.Specialized.NotifyCollectionChangedEventArgs e) => OnPropertyChanged(nameof(Indices));

    protected override void WriteData(EndianAwareBinaryWriter bw)
    {
        bw.Write(Version);
        bw.Write(Param);
        bw.Write(sizeof(ushort) * NumIndices);
        foreach (var index in Indices)
            bw.Write(index);
    }

    protected override Chunk CloneSelf() => new MemoryImageIndexListChunk(Version, Param, Indices);
}
