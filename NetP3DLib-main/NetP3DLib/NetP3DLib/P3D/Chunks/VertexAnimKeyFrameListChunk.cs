using NetP3DLib.IO;
using NetP3DLib.P3D.Attributes;
using NetP3DLib.P3D.Collections;
using NetP3DLib.P3D.Enums;
using NetP3DLib.P3D.Exceptions;
using NetP3DLib.P3D.Extensions;
using System;
using System.Collections.Generic;

namespace NetP3DLib.P3D.Chunks;

[ChunkAttributes(ChunkID)]
public class VertexAnimKeyFrameListChunk : Chunk
{
    public const ChunkIdentifier ChunkID = ChunkIdentifier.Vertex_Anim_Key_Frame_List;

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
    
    public uint NumKeyFrameIds
    {
        get => (uint)(KeyFrameIds?.Count ?? 0);
        set
        {
            if (value == NumKeyFrameIds)
                return;

            if (value < NumKeyFrameIds)
            {
                KeyFrameIds.RemoveRange((int)value, (int)(NumKeyFrameIds - value));
            }
            else
            {
                int count = (int)(value - NumKeyFrameIds);
                var newKeyFrameIds = new uint[count];

                for (var i = 0; i < count; i++)
                    newKeyFrameIds[i] = default;

                KeyFrameIds.AddRange(newKeyFrameIds);
            }
            NumKeyFrameCounts = value;
        }
    }
    public SizeAwareList<uint> KeyFrameIds { get; }
    public uint NumKeyFrameCounts
    {
        get => (uint)(KeyFrameCounts?.Count ?? 0);
        set
        {
            if (value == NumKeyFrameCounts)
                return;

            if (value < NumKeyFrameCounts)
            {
                KeyFrameCounts.RemoveRange((int)value, (int)(NumKeyFrameCounts - value));
            }
            else
            {
                int count = (int)(value - NumKeyFrameCounts);
                var newKeyFrameCounts = new uint[count];

                for (var i = 0; i < count; i++)
                    newKeyFrameCounts[i] = default;

                KeyFrameCounts.AddRange(newKeyFrameCounts);
            }
            NumKeyFrameIds = value;
        }
    }
    public SizeAwareList<uint> KeyFrameCounts { get; }

    public override byte[] DataBytes
    {
        get
        {
            var data = new List<byte>((int)DataLength);

            data.AddRange(BitConverter.GetBytes(Version));
            data.AddRange(BitConverter.GetBytes(NumKeyFrameIds));
            foreach (var keyFrameId in KeyFrameIds)
                data.AddRange(BitConverter.GetBytes(keyFrameId));
            foreach (var keyFrameCount in KeyFrameCounts)
                data.AddRange(BitConverter.GetBytes(keyFrameCount));

            return [.. data];
        }
    }
    public override uint DataLength => sizeof(uint) + sizeof(uint) + sizeof(uint) * NumKeyFrameIds + sizeof(uint) * NumKeyFrameCounts;

    public VertexAnimKeyFrameListChunk(EndianAwareBinaryReader br) : this(br.ReadUInt32(), br.ReadUInt32Array(out var numKeyFrameIds), br.ReadUInt32Array(numKeyFrameIds))
    {
    }

    public VertexAnimKeyFrameListChunk(uint version, IList<uint> keyFrameIds, IList<uint> keyFrameCounts) : base(ChunkID)
    {
        _version = version;
        KeyFrameIds = CreateSizeAwareList(keyFrameIds, KeyFrameIds_CollectionChanged);
        KeyFrameCounts = CreateSizeAwareList(keyFrameCounts, KeyFrameCounts_CollectionChanged);
    }
    
    private void KeyFrameIds_CollectionChanged(object sender, System.Collections.Specialized.NotifyCollectionChangedEventArgs e) => OnPropertyChanged(nameof(KeyFrameIds));
    
    private void KeyFrameCounts_CollectionChanged(object sender, System.Collections.Specialized.NotifyCollectionChangedEventArgs e) => OnPropertyChanged(nameof(KeyFrameCounts));

    public override IEnumerable<InvalidP3DException> ValidateChunk()
    {
        foreach (var error in base.ValidateChunk())
            yield return error;

        if (KeyFrameIds.Count != KeyFrameCounts.Count)
            yield return new InvalidP3DException(this, $"{nameof(KeyFrameIds)} and {nameof(KeyFrameCounts)} must have equal counts.");
    }

    protected override void WriteData(EndianAwareBinaryWriter bw)
    {
        bw.Write(Version);
        bw.Write(NumKeyFrameIds);
        foreach (var keyFrameId in KeyFrameIds)
            bw.Write(keyFrameId);
        foreach (var keyFrameCount in KeyFrameCounts)
            bw.Write(keyFrameCount);
    }

    protected override Chunk CloneSelf() => new VertexAnimKeyFrameListChunk(Version, KeyFrameIds, KeyFrameCounts);
}
