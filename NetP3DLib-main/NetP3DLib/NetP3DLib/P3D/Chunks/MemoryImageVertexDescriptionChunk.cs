using NetP3DLib.IO;
using NetP3DLib.P3D.Attributes;
using NetP3DLib.P3D.Collections;
using NetP3DLib.P3D.Enums;
using NetP3DLib.P3D.Extensions;
using System;
using System.Collections.Generic;

namespace NetP3DLib.P3D.Chunks;

[ChunkAttributes(ChunkID)]
public class MemoryImageVertexDescriptionChunk : Chunk
{
    public const ChunkIdentifier ChunkID = ChunkIdentifier.Memory_Image_Vertex_Description;

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
    
    public uint DescriptionSize
    {
        get => (uint)(Description?.Count ?? 0);
        set
        {
            if (value == DescriptionSize)
                return;

            if (value < DescriptionSize)
            {
                Description.RemoveRange((int)value, (int)(DescriptionSize - value));
            }
            else
            {
                int count = (int)(value - DescriptionSize);
                var newDescriptions = new byte[count];

                for (var i = 0; i < count; i++)
                    newDescriptions[i] = default;

                Description.AddRange(newDescriptions);
            }
        }
    }
    public SizeAwareList<byte> Description { get; }

    public override byte[] DataBytes
    {
        get
        {
            var data = new List<byte>((int)DataLength);

            data.AddRange(BitConverter.GetBytes(Version));
            data.AddRange(BitConverter.GetBytes(Param));
            data.AddRange(BitConverter.GetBytes(DescriptionSize));
            data.AddRange(Description);

            return [.. data];
        }
    }
    public override uint DataLength => sizeof(uint) + sizeof(uint) + sizeof(uint) + DescriptionSize;

    public MemoryImageVertexDescriptionChunk(EndianAwareBinaryReader br) : this(br.ReadUInt32(), br.ReadUInt32(), br.ReadByteArray(out _))
    {
    }

    public MemoryImageVertexDescriptionChunk(uint version, uint param, IList<byte> description) : base(ChunkID)
    {
        _version = version;
        _param = param;
        Description = CreateSizeAwareList(description, Description_CollectionChanged);
    }
    
    private void Description_CollectionChanged(object sender, System.Collections.Specialized.NotifyCollectionChangedEventArgs e) => OnPropertyChanged(nameof(Description));

    protected override void WriteData(EndianAwareBinaryWriter bw)
    {
        bw.Write(Version);
        bw.Write(Param);
        bw.Write(DescriptionSize);
        bw.Write([.. Description]);
    }

    protected override Chunk CloneSelf() => new MemoryImageVertexDescriptionChunk(Version, Param, Description);
}
