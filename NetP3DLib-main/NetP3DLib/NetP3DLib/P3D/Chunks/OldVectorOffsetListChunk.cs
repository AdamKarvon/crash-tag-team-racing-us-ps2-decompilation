using NetP3DLib.IO;
using NetP3DLib.P3D.Attributes;
using NetP3DLib.P3D.Collections;
using NetP3DLib.P3D.Enums;
using NetP3DLib.P3D.Extensions;
using System;
using System.Collections.Generic;
using System.ComponentModel;
using System.Numerics;

namespace NetP3DLib.P3D.Chunks;

[ChunkAttributes(ChunkID)]
public class OldVectorOffsetListChunk : ParamChunk
{
    public const ChunkIdentifier ChunkID = ChunkIdentifier.Old_Vector_Offset_List;

    private uint _version;
    [DefaultValue(0)]
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
                var newOffsets = new Vector3[count];

                for (var i = 0; i < count; i++)
                    newOffsets[i] = default;

                Offsets.AddRange(newOffsets);
            }
        }
    }
    public SizeAwareList<Vector3> Offsets { get; }

    public override byte[] DataBytes
    {
        get
        {
            var data = new List<byte>((int)DataLength);

            data.AddRange(BitConverter.GetBytes(Version));
            data.AddRange(BitConverter.GetBytes(NumOffsets));
            data.AddRange(BinaryExtensions.GetFourCCBytes(Param));
            foreach (var offset in Offsets)
                data.AddRange(BinaryExtensions.GetBytes(offset));

            return [.. data];
        }
    }
    public override uint DataLength => sizeof(uint) + sizeof(uint) + 4 + sizeof(float) * 3 * NumOffsets;

    public OldVectorOffsetListChunk(EndianAwareBinaryReader br) : this(br.ReadUInt32(), ReadParam(br, out var numOffsets), br.ReadVector3Array(numOffsets))
    {
    }

    private static string ReadParam(EndianAwareBinaryReader br, out int numOffsets)
    {
        numOffsets = br.ReadInt32();
        return br.ReadFourCC();
    }

    public OldVectorOffsetListChunk(uint version, string param, IList<Vector3> offsets) : base(ChunkID, param)
    {
        _version = version;
        Offsets = CreateSizeAwareList(offsets, Offsets_CollectionChanged);
    }
    
    private void Offsets_CollectionChanged(object sender, System.Collections.Specialized.NotifyCollectionChangedEventArgs e) => OnPropertyChanged(nameof(Offsets));

    protected override void WriteData(EndianAwareBinaryWriter bw)
    {
        bw.Write(Version);
        bw.Write(NumOffsets);
        bw.WriteFourCC(Param);
        foreach (var offset in Offsets)
            bw.Write(offset);
    }

    protected override Chunk CloneSelf() => new OldVectorOffsetListChunk(Version, Param, Offsets);
}
