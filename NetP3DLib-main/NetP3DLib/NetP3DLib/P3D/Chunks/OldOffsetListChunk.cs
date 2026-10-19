using NetP3DLib.IO;
using NetP3DLib.P3D.Attributes;
using NetP3DLib.P3D.Collections;
using NetP3DLib.P3D.Enums;
using NetP3DLib.P3D.Exceptions;
using NetP3DLib.P3D.Extensions;
using System;
using System.Collections.Generic;
using System.IO;
using System.Numerics;

namespace NetP3DLib.P3D.Chunks;

[ChunkAttributes(ChunkID)]
public class OldOffsetListChunk : Chunk
{
    public const ChunkIdentifier ChunkID = ChunkIdentifier.Old_Offset_List;

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
                var newOffsets = new OffsetEntry[count];

                for (var i = 0; i < count; i++)
                    newOffsets[i] = new();

                Offsets.AddRange(newOffsets);
            }
        }
    }
    
    private uint _keyIndex;
    public uint KeyIndex
    {
        get => _keyIndex;
        set
        {
            if (_keyIndex == value)
                return;
    
            _keyIndex = value;
            OnPropertyChanged(nameof(KeyIndex));
        }
    }
    
    public SizeAwareList<OffsetEntry> Offsets { get; }
    private bool _hasPrimGroupIndex = false;
    public bool HasPrimGroupIndex
    {
        get => _hasPrimGroupIndex;
        set
        {
            if (_hasPrimGroupIndex == value)
                return;

            var oldSize = HeaderSize;
            _hasPrimGroupIndex = value;
            RecalculateSize(oldSize);
            OnPropertyChanged(nameof(HasPrimGroupIndex));
        }
    }
    
    private uint _primGroupIndex;
    public uint PrimGroupIndex
    {
        get => _primGroupIndex;
        set
        {
            if (_primGroupIndex == value)
                return;
    
            _primGroupIndex = value;
            OnPropertyChanged(nameof(PrimGroupIndex));
        }
    }
    

    public override byte[] DataBytes
    {
        get
        {
            var data = new List<byte>((int)DataLength);

            data.AddRange(BitConverter.GetBytes(NumOffsets));
            data.AddRange(BitConverter.GetBytes(KeyIndex));
            foreach (var offset in Offsets)
                data.AddRange(offset.DataBytes);
            if (HasPrimGroupIndex)
                data.AddRange(BitConverter.GetBytes(PrimGroupIndex));

            return [.. data];
        }
    }
    public override uint DataLength => sizeof(uint) + sizeof(uint) + OffsetEntry.Size * NumOffsets + (HasPrimGroupIndex ? sizeof(uint) : 0u);

    public OldOffsetListChunk(EndianAwareBinaryReader br, uint headerSize) : this(ReadKeyIndex(br, out var numOffsets), br.ReadArray(numOffsets, () => new OffsetEntry(br)), headerSize >= (sizeof(uint) + sizeof(uint) + OffsetEntry.Size * numOffsets + sizeof(uint)) ? br.ReadUInt32() : null)
    {
    }

    private static uint ReadKeyIndex(EndianAwareBinaryReader br, out int numOffsets)
    {
        numOffsets = br.ReadInt32();
        return br.ReadUInt32();
    }

    public OldOffsetListChunk(uint keyIndex, IList<OffsetEntry> offsets) : this(keyIndex, offsets, null)
    {
    }

    public OldOffsetListChunk(uint keyIndex, IList<OffsetEntry> offsets, uint? primGroupIndex = null) : base(ChunkID)
    {
        _keyIndex = keyIndex;
        Offsets = CreateSizeAwareList(offsets, Offsets_CollectionChanged);
        _hasPrimGroupIndex = primGroupIndex.HasValue;
        if (_hasPrimGroupIndex)
            _primGroupIndex = primGroupIndex!.Value;
    }
    
    private void Offsets_CollectionChanged(object sender, System.Collections.Specialized.NotifyCollectionChangedEventArgs e)
    {
        OnPropertyChanged(nameof(Offsets));

        if (e.OldItems != null)
            foreach (OffsetEntry oldItem in e.OldItems)
                oldItem.PropertyChanged -= Offsets_PropertyChanged;
    
        if (e.NewItems != null)
            foreach (OffsetEntry newItem in e.NewItems)
                newItem.PropertyChanged += Offsets_PropertyChanged;
    }
    
    private void Offsets_PropertyChanged() => OnPropertyChanged(nameof(Offsets));

    public override IEnumerable<InvalidP3DException> ValidateChunk()
    {
        foreach (var error in base.ValidateChunk())
            yield return error;

        if (Children.Count != GetChildCount(ChunkIdentifier.Old_Index_Offset_List))
            yield return new InvalidP3DException(this, $"Invalid children. Must be only {nameof(OldIndexOffsetListChunk)}.");
    }

    protected override void WriteData(EndianAwareBinaryWriter bw)
    {
        bw.Write(NumOffsets);
        bw.Write(KeyIndex);
        foreach (var offset in Offsets)
            offset.Write(bw);
        if (HasPrimGroupIndex)
            bw.Write(PrimGroupIndex);
    }

    protected override Chunk CloneSelf()
    {
        var offsets = new OffsetEntry[Offsets.Count];
        for (var i = 0; i < Offsets.Count; i++)
            offsets[i] = Offsets[i].Clone();
        return new OldOffsetListChunk(KeyIndex, offsets);
    }

    public class OffsetEntry
    {
        public const uint Size = sizeof(uint) + sizeof(float) * 3;

        public event Action? PropertyChanged;

        private uint _index;
        public uint Index
        {
            get => _index;
            set
            {
                if (_index == value)
                    return;
    
                _index = value;
                PropertyChanged?.Invoke();
            }
        }
    
        private Vector3 _offset;
        public Vector3 Offset
        {
            get => _offset;
            set
            {
                if (_offset == value)
                    return;
    
                _offset = value;
                PropertyChanged?.Invoke();
            }
        }
    

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

        public OffsetEntry(BinaryReader br)
        {
            _index = br.ReadUInt32();
            _offset = br.ReadVector3();
        }

        public OffsetEntry(uint index, Vector3 offset)
        {
            _index = index;
            _offset = offset;
        }

        public OffsetEntry()
        {
            _index = 0;
            _offset = Vector3.Zero;
        }

        internal void Write(BinaryWriter bw)
        {
            bw.Write(Index);
            bw.Write(Offset);
        }

        internal OffsetEntry Clone() => new(Index, Offset);

        public override string ToString() => $"{Index} | {Offset}";
    }
}
