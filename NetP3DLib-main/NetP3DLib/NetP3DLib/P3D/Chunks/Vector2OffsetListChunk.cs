using NetP3DLib.IO;
using NetP3DLib.P3D.Attributes;
using NetP3DLib.P3D.Collections;
using NetP3DLib.P3D.Enums;
using NetP3DLib.P3D.Extensions;
using System;
using System.Collections.Generic;
using System.IO;
using System.Numerics;

namespace NetP3DLib.P3D.Chunks;

[ChunkAttributes(ChunkID)]
public class Vector2OffsetListChunk : ParamChunk
{
    public const ChunkIdentifier ChunkID = ChunkIdentifier.Vector2_Offset_List;

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
                var newOffsets = new Vector2Offset[count];

                for (var i = 0; i < count; i++)
                    newOffsets[i] = new();

                Offsets.AddRange(newOffsets);
            }
        }
    }
    public SizeAwareList<Vector2Offset> Offsets { get; }

    public override byte[] DataBytes
    {
        get
        {
            var data = new List<byte>((int)DataLength);

            data.AddRange(BitConverter.GetBytes(Version));
            data.AddRange(BinaryExtensions.GetFourCCBytes(Param));
            data.AddRange(BitConverter.GetBytes(NumOffsets));
            foreach (var offset in Offsets)
                data.AddRange(offset.DataBytes);

            return [.. data];
        }
    }
    public override uint DataLength => sizeof(uint) + 4 + sizeof(uint) + Vector2Offset.Size * NumOffsets;

    public Vector2OffsetListChunk(EndianAwareBinaryReader br) : this(br.ReadUInt32(), br.ReadFourCC(), br.ReadArray(() => new Vector2Offset(br), out _))
    {
    }

    public Vector2OffsetListChunk(uint version, string param, IList<Vector2Offset> offsets) : base(ChunkID, param)
    {
        _version = version;
        Offsets = CreateSizeAwareList(offsets, Offsets_CollectionChanged);
    }
    
    private void Offsets_CollectionChanged(object sender, System.Collections.Specialized.NotifyCollectionChangedEventArgs e) => OnPropertyChanged(nameof(Offsets));

    protected override void WriteData(EndianAwareBinaryWriter bw)
    {
        bw.Write(Version);
        bw.WriteFourCC(Param);
        bw.Write(NumOffsets);
        foreach (var offset in Offsets)
            offset.Write(bw);
    }

    protected override Chunk CloneSelf()
    {
        var offsets = new Vector2Offset[Offsets.Count];
        for (var i = 0; i < Offsets.Count; i++)
            offsets[i] = Offsets[i].Clone();
        return new Vector2OffsetListChunk(Version, Param, offsets);
    }

    public class Vector2Offset
    {
        public const uint Size = sizeof(uint) + sizeof(float) * 2;

        public uint Index;
        public Vector2 Offset;

        public byte[] DataBytes
        {
            get
            {
                var data = new List<byte>((int)Size);

                data.AddRange(BitConverter.GetBytes(Index));
                data.AddRange(BinaryExtensions.GetBytes(Offset));

                return [.. data];
            }
        }

        public Vector2Offset(BinaryReader br)
        {
            Index = br.ReadUInt32();
            Offset = br.ReadVector2();
        }

        public Vector2Offset(uint index, Vector2 offset)
        {
            Index = index;
            Offset = offset;
        }

        public Vector2Offset()
        {
            Index = 0;
            Offset = new();
        }

        internal void Write(BinaryWriter bw)
        {
            bw.Write(Index);
            bw.Write(Offset);
        }

        internal Vector2Offset Clone() => new(Index, Offset);

        public override string ToString() => $"{Index} | {Offset}";
    }
}
