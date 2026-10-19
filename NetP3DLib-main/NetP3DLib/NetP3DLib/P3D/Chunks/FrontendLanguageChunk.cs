using NetP3DLib.IO;
using NetP3DLib.P3D.Attributes;
using NetP3DLib.P3D.Collections;
using NetP3DLib.P3D.Enums;
using NetP3DLib.P3D.Extensions;
using System;
using System.Collections.Generic;
using System.Text;

namespace NetP3DLib.P3D.Chunks;

[ChunkAttributes(ChunkID)]
public class FrontendLanguageChunk : NamedChunk
{
    public const ChunkIdentifier ChunkID = ChunkIdentifier.Frontend_Language;

    private char _language;
    public char Language
    {
        get => _language;
        set
        {
            if (_language == value)
                return;
    
            _language = value;
            OnPropertyChanged(nameof(Language));
        }
    }
    
    public uint NumEntries
    {
        get => (uint)(Entries?.Count ?? 0);
        set
        {
            if (value == NumEntries)
                return;

            if (value < NumEntries)
            {
                Entries.RemoveRange((int)value, (int)(NumEntries - value));
            }
            else
            {
                int count = (int)(value - NumEntries);
                var newEntries = new Entry[count];

                for (var i = 0; i < count; i++)
                    newEntries[i] = new();

                Entries.AddRange(newEntries);
            }
        }
    }
    
    private uint _modulo;
    public uint Modulo
    {
        get => _modulo;
        set
        {
            if (_modulo == value)
                return;
    
            _modulo = value;
            OnPropertyChanged(nameof(Modulo));
        }
    }
    
    public SizeAwareList<Entry> Entries { get; }

    public override byte[] DataBytes
    {
        get
        {
            var data = new List<byte>((int)DataLength);

            data.AddRange(BinaryExtensions.GetP3DStringBytes(Name));
            data.Add((byte)Language);
            data.AddRange(BitConverter.GetBytes(NumEntries));
            data.AddRange(BitConverter.GetBytes(Modulo));

            (List<uint> Hashes, List<uint> Offsets, List<ushort> Buffer) = BuildData();

            data.AddRange(BitConverter.GetBytes(Buffer.Count * 2));
            foreach (var hash in Hashes)
                data.AddRange(BitConverter.GetBytes(hash));
            foreach (var offset in Offsets)
                data.AddRange(BitConverter.GetBytes(offset));
            foreach (var code in Buffer)
                data.AddRange(BitConverter.GetBytes(code));

            return [.. data];
        }
    }
    public override uint DataLength => BinaryExtensions.GetP3DStringLength(Name) + 1 + sizeof(uint) + sizeof(uint) + sizeof(uint) + (Entries == null ? 0 : sizeof(uint) * (uint)Entries.Count + sizeof(uint) * (uint)Entries.Count + (uint)(BuildData().Buffer.Count * 2));

    public FrontendLanguageChunk(EndianAwareBinaryReader br) : this(br.ReadP3DString(), br.ReadChar(), ReadModuloAndEntries(br, out var entries), entries)
    {
    }

    private static uint ReadModuloAndEntries(EndianAwareBinaryReader br, out Entry[] entries)
    {
        var numEntries = br.ReadInt32();
        var modulo = br.ReadUInt32();
        var bufferSize = br.ReadInt32();

        var hashes = new uint[numEntries];
        for (int i = 0; i < numEntries; i++)
            hashes[i] = br.ReadUInt32();

        var offsets = new uint[numEntries];
        for (int i = 0; i < numEntries; i++)
            offsets[i] = br.ReadUInt32();

        var bufferBytes = br.ReadBytes(bufferSize);
        var buffer = Encoding.Unicode.GetString(bufferBytes);

        entries = new Entry[numEntries];
        for (int i = 0; i < numEntries; i++)
        {
            uint hash = hashes[i];
            int offset = (int)offsets[i] / 2;
            int length = buffer.IndexOf('\0', offset) - offset;
            entries[i] = new(hash, buffer.Substring(offset, length));
        }

        return modulo;
    }

    public FrontendLanguageChunk(string name, char language, uint modulo, IList<Entry> entries) : base(ChunkID, name)
    {
        _language = language;
        _modulo = modulo;
        Entries = CreateSizeAwareList(entries, Entries_CollectionChanged);
    }

    public FrontendLanguageChunk(string name, char language, uint modulo, Dictionary<string, string> entries) : base(ChunkID, name)
    {
        _language = language;
        _modulo = modulo;
        var entries2 = new Entry[entries.Count];
        var i = 0;
        foreach (var entry in entries)
            entries2[i++] = new(GetNameHash(entry.Key), entry.Value);

        Entries = CreateSizeAwareList(entries2, Entries_CollectionChanged);
    }

    private void Entries_CollectionChanged(object sender, System.Collections.Specialized.NotifyCollectionChangedEventArgs e)
    {
        OnPropertyChanged(nameof(Entries));

        if (e.OldItems != null)
        {
            foreach (Entry oldEntry in e.OldItems)
            {
                oldEntry.SizeChanged -= Entry_SizeChanged;
                oldEntry.PropertyChanged -= Entry_PropertyChanged;
            }
        }

        if (e.NewItems != null)
        {
            foreach (Entry newEntry in e.NewItems)
            {
                newEntry.SizeChanged += Entry_SizeChanged;
                newEntry.PropertyChanged += Entry_PropertyChanged;
            }
        }
    }

    private void Entry_SizeChanged(int delta) => RecalculateSize((uint)(HeaderSize - delta));

    private void Entry_PropertyChanged() => OnPropertyChanged(nameof(Entries));

    protected override void WriteData(EndianAwareBinaryWriter bw)
    {
        bw.WriteP3DString(Name);
        bw.Write(Language);
        bw.Write(NumEntries);
        bw.Write(Modulo);

        (List<uint> Hashes, List<uint> Offsets, List<ushort> Buffer) = BuildData();

        bw.Write(Buffer.Count * 2);
        foreach (var hash in Hashes)
            bw.Write(hash);
        foreach (var offset in Offsets)
            bw.Write(offset);
        foreach (var code in Buffer)
            bw.Write(code);
    }

    protected override Chunk CloneSelf()
    {
        var entries = new Entry[Entries.Count];
        for (var i = 0; i < Entries.Count; i++)
            entries[i] = Entries[i].Clone();
        return new FrontendLanguageChunk(Name, Language, Modulo, entries);
    }

    private (List<uint> Hashes, List<uint> Offsets, List<ushort> Buffer) BuildData()
    {
        List<uint> hashes = new(Entries.Count);
        List<uint> offsets = new(Entries.Count);
        StringBuilder sb = new();

        foreach (var entry in Entries)
        {
            hashes.Add(entry.Hash);
            offsets.Add((uint)sb.Length * 2);
            sb.Append(entry.Value);
            sb.Append('\0');
        }

        var bufferBytes = Encoding.Unicode.GetBytes(sb.ToString());
        List<ushort> buffer = new(bufferBytes.Length / 2);
        for (int i = 0; i < bufferBytes.Length; i += 2)
            buffer.Add(BitConverter.ToUInt16(bufferBytes, i));

        return (hashes, offsets, buffer);
    }

    public uint GetNameHash(string name)
    {
        if (string.IsNullOrWhiteSpace(name))
            throw new ArgumentNullException(nameof(name), $"{nameof(name)} cannot be null or empty.");

        uint Hash = 0;
        foreach (var c in name)
        {
            int i = Convert.ToInt32(c);
            Hash = (uint)((i + (Hash << 6)) % Modulo);
        }
        return Hash;
    }

    public string? GetValue(uint hash)
    {
        foreach (var entry in Entries)
            if (entry.Hash == hash)
                return entry.Value;
        return null;
    }

    public string? GetValue(string name) => GetValue(GetNameHash(name));

    public void AddValue(uint hash, string value) => Entries.Add(new(hash, value));

    public void AddValue(string name, string value) => AddValue(GetNameHash(name), value);

    public void SetValue(uint hash, string value)
    {
        var index = Entries.FindIndex(x => x.Hash == hash);
        if (index == -1)
            AddValue(hash, value);
        else
            Entries[index].Value = value;
    }

    public void SetValue(string name, string value) => SetValue(GetNameHash(name), value);

    public class Entry
    {
        public event Action<int>? SizeChanged;
        public event Action? PropertyChanged;

        private uint _hash;
        public uint Hash
        {
            get => _hash;
            set
            {
                if (_hash == value)
                    return;
    
                _hash = value;
                PropertyChanged?.Invoke();
            }
        }
    
        private string _value = string.Empty;
        public string Value
        {
            get => _value;
            set
            {
                if (_value == value)
                    return;

                var oldSize = Encoding.Unicode.GetByteCount(_value);
                _value = value;
                var newSize = Encoding.Unicode.GetByteCount(_value);
                SizeChanged?.Invoke(newSize - oldSize);
                PropertyChanged?.Invoke();
            }
        }

        public Entry(uint hash, string value)
        {
            _hash = hash;
            _value = value;
        }

        public Entry(uint hash)
        {
            _hash = hash;
            _value = string.Empty;
        }

        public Entry()
        {
            _hash = 0;
            _value = string.Empty;
        }

        public Entry Clone() => new(Hash, Value);

        public override string ToString() => $"0x{Hash:X} | {Value}";
    }
}
