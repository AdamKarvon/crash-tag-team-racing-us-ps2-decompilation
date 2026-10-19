using NetP3DLib.IO;
using NetP3DLib.P3D.Attributes;
using NetP3DLib.P3D.Collections;
using NetP3DLib.P3D.Enums;
using NetP3DLib.P3D.Extensions;
using System;
using System.Collections.Generic;
using System.Drawing;

namespace NetP3DLib.P3D.Chunks;

[ChunkAttributes(ChunkID)]
public class OldColourOffsetListChunk : Chunk
{
    public const ChunkIdentifier ChunkID = ChunkIdentifier.Old_Colour_Offset_List;

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
    
    public uint NumOffsets
    {
        get => (uint)(Offsets?.Count ?? 0);
        set
        {
            if (value == NumOffsets)
                return;

            if (value < NumOffsets)
            {
                Offsets.RemoveRange((int)value, (int)(NumOffsets - value));
            }
            else
            {
                int count = (int)(value - NumOffsets);
                var newOffsets = new Color[count];

                for (var i = 0; i < count; i++)
                    newOffsets[i] = default;

                Offsets.AddRange(newOffsets);
            }
        }
    }
    public SizeAwareList<Color> Offsets { get; }

    public override byte[] DataBytes
    {
        get
        {
            var data = new List<byte>((int)DataLength);

            data.AddRange(BitConverter.GetBytes(Version));
            data.AddRange(BitConverter.GetBytes(NumOffsets));
            foreach (var offset in Offsets)
                data.AddRange(BinaryExtensions.GetBytes(offset));

            return [.. data];
        }
    }
    public override uint DataLength => sizeof(uint) + sizeof(uint) + sizeof(uint) * NumOffsets;

    public OldColourOffsetListChunk(EndianAwareBinaryReader br) : this(br.ReadUInt32(), br.ReadColorArray(out _))
    {
    }

    public OldColourOffsetListChunk(uint version, IList<Color> offsets) : base(ChunkID)
    {
        _version = version;
        Offsets = CreateSizeAwareList(offsets, Offsets_CollectionChanged);
    }
    
    private void Offsets_CollectionChanged(object sender, System.Collections.Specialized.NotifyCollectionChangedEventArgs e) => OnPropertyChanged(nameof(Offsets));

    protected override void WriteData(EndianAwareBinaryWriter bw)
    {
        bw.Write(Version);
        bw.Write(NumOffsets);
        foreach (var offset in Offsets)
            bw.Write(offset);
    }

    protected override Chunk CloneSelf() => new OldColourOffsetListChunk(Version, Offsets);
}
