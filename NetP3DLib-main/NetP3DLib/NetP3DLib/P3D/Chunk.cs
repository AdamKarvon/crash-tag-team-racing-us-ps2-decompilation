using NetP3DLib.IO;
using NetP3DLib.P3D.Chunks;
using NetP3DLib.P3D.Collections;
using NetP3DLib.P3D.Comparers;
using NetP3DLib.P3D.Enums;
using NetP3DLib.P3D.Exceptions;
using NetP3DLib.P3D.Helpers;
using System;
using System.Collections.Generic;
using System.Collections.ObjectModel;
#if DEBUG
using System.Diagnostics;
#endif

namespace NetP3DLib.P3D;

/// <summary>
/// Class <c>Chunk</c> is the base chunk used for any unknown chunks.
/// </summary>
public abstract class Chunk
{
    /// <summary>
    /// Property <c>ID</c> is the chunk's identifier.
    /// </summary>
    public uint ID { get; set; }
    /// <summary>
    /// The parent file of this chunk. If <c>null</c>, chunk is not in a file.
    /// </summary>
    public P3DFile? ParentFile { get; internal set; } = null;
    /// <summary>
    /// The parent of this chunk. If <c>null</c>, either not in a hierarchy or in root of file (see <see cref="ParentFile"/>).
    /// </summary>
    public Chunk? ParentChunk { get; internal set; } = null;
    /// <summary>
    /// The index in <see cref="ParentChunk"/> or <see cref="ParentFile"/>. <c>-1</c> if no parent.
    /// </summary>
    public int IndexInParent { get; internal set; } = -1;
    /// <summary>
    /// Property <c>Children</c> is a collection of a chunk's children.
    /// </summary>
    public ChunkCollection Children { get; internal set; }
    /// <summary>
    /// Property <c>AllChildren</c> is all the chunk's children flattened.
    /// </summary>
    public ReadOnlyCollection<Chunk> AllChildren
    {
        get
        {
            var result = new List<Chunk>(Children.Count * 4);

            var stack = new Stack<Chunk>(Children.Count);
            for (int i = Children.Count - 1; i >= 0; i--)
                stack.Push(Children[i]);

            while (stack.Count > 0)
            {
                var chunk = stack.Pop();
                result.Add(chunk);

                var children = chunk.Children;
                for (int i = children.Count - 1; i >= 0; i--)
                    stack.Push(children[i]);
            }

            return result.AsReadOnly();
        }
    }

    /// <summary>
    /// Property <c>DataBytes</c> is the chunk's header data, built from the chunk's properties.
    /// <para>NOTE: This will use the system's default <see cref="Endianness"/>.</para>
    /// </summary>
    public abstract byte[] DataBytes { get; }
    /// <summary>
    /// Property <c>DataLength</c> should be the number of bytes of <see cref="DataBytes"/>.
    /// <para>NOTE: This can be calculated by <see cref="DataBytes"/>.<c>Length</c>, however hardcoding is faster.</para>
    /// </summary>
    public virtual uint DataLength => (uint)DataBytes.Length;
    /// <summary>
    /// Property <c>HeaderSize</c> is the chunk's header size.
    /// <para>This is usually <see cref="P3DFile.HEADER_SIZE"/> + <see cref="DataBytes"/><c>.Length</c>.</para>
    /// </summary>
    public uint HeaderSize => P3DFile.HEADER_SIZE + DataLength;

    public event Action<Chunk, int>? SizeChanged;
    protected SizeAwareList<T> CreateSizeAwareList<T>(int capacity = 0) => new(this, RecalculateSize, capacity);
    protected SizeAwareList<T> CreateSizeAwareList<T>(IList<T> values, System.Collections.Specialized.NotifyCollectionChangedEventHandler collectionChangedHandler)
    {
        var list = new SizeAwareList<T>(this, RecalculateSize, values.Count);
        list.CollectionChanged += collectionChangedHandler;
        list.AddRange(values);
        return list;
    }
    internal void RecalculateSize(uint oldSize)
    {
        int delta = (int)(HeaderSize - oldSize);
        if (delta == 0)
            return;

        SizeChanged?.Invoke(this, delta);
    }
    /// <summary>
    /// Property <c>Size</c> is the chunk's size.
    /// <para>This is usually <see cref="HeaderSize"/> + SUM(<see cref="Children"/>.<see cref="Size"/>).</para>
    /// </summary>
    public uint Size => HeaderSize + Children.TotalSize;
    /// <summary>
    /// Property <c>Bytes</c> is the chunk's data, built from the chunk's properties and children.
    /// <para>NOTE: This will use the system's default <see cref="Endianness"/>.</para>
    /// </summary>
    public byte[] Bytes
    {
        get
        {
            List<byte> bytes = [];
            bytes.AddRange(BitConverter.GetBytes(ID));
            bytes.AddRange(BitConverter.GetBytes(HeaderSize));
            bytes.AddRange(BitConverter.GetBytes(Size));
            bytes.AddRange(DataBytes);
            foreach (var chunk in Children)
                bytes.AddRange(chunk.Bytes);
            return [.. bytes];
        }
    }

    /// <summary>
    /// This constructor initialises an unknown chunk with the given <paramref name="chunkId"/>.
    /// </summary>
    /// <param name="chunkId">The chunk's identifier. See <seealso cref="ChunkIdentifier"/> for known identifiers.</param>
    protected Chunk(uint chunkId)
    {
        ID = chunkId;
        Children = new(this);
    }

    /// <summary>
    /// This constructor initialises an unknown chunk with the given <paramref name="chunkId"/>.
    /// </summary>
    /// <param name="chunkId">The chunk's identifier. See <seealso cref="ChunkIdentifier"/> for known identifiers.</param>
    protected Chunk(ChunkIdentifier chunkId) : this((uint)chunkId) { }

    /// <summary>
    /// Validates this chunk.
    /// </summary>
    /// <returns>
    /// An enumerable of <see cref="InvalidP3DException"/> representing validation errors.
    /// If the chunk is valid, returns an empty enumerable.
    /// </returns>
    public virtual IEnumerable<InvalidP3DException> ValidateChunk()
    {
        yield break;
    }

    /// <summary>
    /// Recursively validates this chunk and its children.
    /// </summary>
    /// <returns>
    /// An enumerable of <see cref="InvalidP3DException"/> representing validation errors.
    /// If the chunk and all children are valid, returns an empty enumerable.
    /// </returns>
    public IEnumerable<InvalidP3DException> ValidateChunks()
    {
        foreach (var error in ValidateChunk())
            yield return error;

        foreach (var child in Children)
            foreach (var error in child.ValidateChunks())
                yield return error;
    }

    /// <summary>
    /// Writes this chunk, including its <see cref="Children"/>, to <paramref name="bw"/>.
    /// </summary>
    /// <param name="bw">The <c>BinaryWriter</c> to write to.</param>
    public void Write(EndianAwareBinaryWriter bw)
    {
        bw.Write(ID);
        bw.Write(HeaderSize);
        bw.Write(Size);

#if DEBUG
        var startPos = bw.BaseStream.Position;
#endif
        WriteData(bw);
#if DEBUG
        var length = bw.BaseStream.Position - startPos;
        if (length != HeaderSize - 12)
        {
            ChunkIdentifier chunkIdentifier = (ChunkIdentifier)ID;
            Debugger.Break();
        }
#endif

        foreach (var chunk in Children)
            chunk.Write(bw);
    }

    public Chunk Clone()
    {
        var chunk = CloneSelf();
        foreach (var child in Children)
            chunk.Children.Add(child.Clone());
        return chunk;
    }

    private readonly Dictionary<Type, List<Chunk>> _chunksByType = [];
    private readonly Dictionary<(Type, string name), List<NamedChunk>> _namedChunks = new(ChunkKeyComparer.Instance);
    private readonly Dictionary<(Type, string name), List<ParamChunk>> _paramChunks = new(ChunkKeyComparer.Instance);

    public Chunk? GetFirstChunkOfType(Type chunkType)
    {
        if (chunkType == null)
            throw new ArgumentNullException(nameof(chunkType));

        if (!_chunksByType.TryGetValue(chunkType, out var chunks))
            return null;

        if (!typeof(Chunk).IsAssignableFrom(chunkType))
            throw new ArgumentException($"{chunkType.Name} must inherit from {nameof(Chunk)}", nameof(chunkType));

        return chunks[0];
    }

    public T? GetFirstChunkOfType<T>() where T : Chunk => _chunksByType.TryGetValue(typeof(T), out var chunks) ? (T)chunks[0] : null;

    public NamedChunk? GetFirstChunkOfType(Type chunkType, string name)
    {
        if (chunkType == null)
            throw new ArgumentNullException(nameof(chunkType));

        if (!_namedChunks.TryGetValue((chunkType, name), out var namedChunks))
            return null;

        if (!typeof(NamedChunk).IsAssignableFrom(chunkType))
            throw new ArgumentException($"{chunkType.Name} must inherit from {nameof(NamedChunk)}", nameof(chunkType));

        return namedChunks[0];
    }

    public T? GetFirstChunkOfType<T>(string name) where T : NamedChunk => _namedChunks.TryGetValue((typeof(T), name), out var namedChunks) ? (T)namedChunks[0] : null;

    public ParamChunk? GetFirstParamOfType(Type chunkType, string param)
    {
        if (chunkType == null)
            throw new ArgumentNullException(nameof(chunkType));

        if (!_paramChunks.TryGetValue((chunkType, param), out var paramChunks))
            return null;

        if (!typeof(ParamChunk).IsAssignableFrom(chunkType))
            throw new ArgumentException($"{chunkType.Name} must inherit from {nameof(ParamChunk)}", nameof(chunkType));

        return paramChunks[0];
    }

    public T? GetFirstParamOfType<T>(string param) where T : ParamChunk => _paramChunks.TryGetValue((typeof(T), param), out var paramChunks) ? (T)paramChunks[0] : null;

    public Chunk? GetLastChunkOfType(Type chunkType)
    {
        if (chunkType == null)
            throw new ArgumentNullException(nameof(chunkType));

        if (!_chunksByType.TryGetValue(chunkType, out var chunks))
            return null;

        if (!typeof(Chunk).IsAssignableFrom(chunkType))
            throw new ArgumentException($"{chunkType.Name} must inherit from {nameof(Chunk)}", nameof(chunkType));

        return chunks[chunks.Count - 1];
    }

    public T? GetLastChunkOfType<T>() where T : Chunk => _chunksByType.TryGetValue(typeof(T), out var chunks) ? (T)chunks[chunks.Count - 1] : null;

    public NamedChunk? GetLastChunkOfType(Type chunkType, string name)
    {
        if (chunkType == null)
            throw new ArgumentNullException(nameof(chunkType));

        if (!_namedChunks.TryGetValue((chunkType, name), out var namedChunks))
            return null;

        if (!typeof(NamedChunk).IsAssignableFrom(chunkType))
            throw new ArgumentException($"{chunkType.Name} must inherit from {nameof(NamedChunk)}", nameof(chunkType));

        return namedChunks[namedChunks.Count - 1];
    }

    public T? GetLastChunkOfType<T>(string name) where T : NamedChunk => _namedChunks.TryGetValue((typeof(T), name), out var namedChunks) ? (T)namedChunks[namedChunks.Count - 1] : null;

    public ParamChunk? GetLastParamOfType(Type chunkType, string param)
    {
        if (chunkType == null)
            throw new ArgumentNullException(nameof(chunkType));

        if (!_paramChunks.TryGetValue((chunkType, param), out var paramChunks))
            return null;

        if (!typeof(ParamChunk).IsAssignableFrom(chunkType))
            throw new ArgumentException($"{chunkType.Name} must inherit from {nameof(ParamChunk)}", nameof(chunkType));

        return paramChunks[paramChunks.Count - 1];
    }

    public T? GetLastParamOfType<T>(string param) where T : ParamChunk => _paramChunks.TryGetValue((typeof(T), param), out var paramChunks) ? (T)paramChunks[paramChunks.Count - 1] : null;

    public IReadOnlyList<T> GetChunksOfType<T>() where T : Chunk
    {
        if (!_chunksByType.TryGetValue(typeof(T), out var chunks))
            return [];

        return new CastedReadOnlyList<Chunk, T>(chunks);
    }

    public IReadOnlyList<T> GetChunksOfType<T>(string name) where T : NamedChunk
    {
        if (!_namedChunks.TryGetValue((typeof(T), name), out var namedChunks))
            return [];

        return new CastedReadOnlyList<NamedChunk, T>(namedChunks);
    }

    public IReadOnlyList<T> GetParamsOfType<T>(string param) where T : ParamChunk
    {
        if (!_paramChunks.TryGetValue((typeof(T), param), out var paramChunks))
            return [];

        return new CastedReadOnlyList<ParamChunk, T>(paramChunks);
    }

    public NamedChunk? FindNamedChunkInParentHierarchy(Type chunkType, string name)
    {
        if (chunkType == null)
            throw new ArgumentNullException(nameof(chunkType));

        if (!typeof(NamedChunk).IsAssignableFrom(chunkType))
            throw new ArgumentException($"{chunkType.Name} must inherit from {nameof(NamedChunk)}", nameof(chunkType));

        var currentParentFile = ParentFile;
        var currentIndex = IndexInParent;
        if (currentParentFile != null)
        {
            var chunk = currentParentFile.GetFirstChunkOfType(chunkType, name);
            if (chunk != null && chunk.IndexInParent < currentIndex)
                return chunk;

            var sets = currentParentFile.GetChunksOfType<SetChunk>(name);
            foreach (var set in sets)
            {
                if (set.IndexInParent > currentIndex)
                    break;

                if (set.GetSetType() == chunkType)
                    return (NamedChunk)set.Children[0];
            }

            return null;
        }

        var current = ParentChunk;
        while (current != null)
        {
            var chunk = current.GetFirstChunkOfType(chunkType, name);
            if (chunk != null && chunk.IndexInParent < currentIndex)
                return chunk;

            currentParentFile = current.ParentFile;
            currentIndex = current.IndexInParent;

            if (currentParentFile != null)
            {
                chunk = currentParentFile.GetFirstChunkOfType(chunkType, name);
                if (chunk != null && chunk.IndexInParent < currentIndex)
                    return chunk;

                var sets = currentParentFile.GetChunksOfType<SetChunk>(name);
                foreach (var set in sets)
                {
                    if (set.IndexInParent > currentIndex)
                        break;

                    if (set.GetSetType() == chunkType)
                        return (NamedChunk)set.Children[0];
                }

                return null;
            }

            current = current.ParentChunk;
        }

        return null;
    }

    public T? FindNamedChunkInParentHierarchy<T>(string name) where T : NamedChunk
    {
        var chunkType = typeof(T);

        var currentParentFile = ParentFile;
        var currentIndex = IndexInParent;
        if (currentParentFile != null)
        {
            var chunk = currentParentFile.GetFirstChunkOfType<T>(name);
            if (chunk != null && chunk.IndexInParent < currentIndex)
                return chunk;

            var sets = currentParentFile.GetChunksOfType<SetChunk>(name);
            foreach (var set in sets)
            {
                if (set.IndexInParent > currentIndex)
                    break;

                if (set.GetSetType() == chunkType)
                    return (T)set.Children[0];
            }

            return null;
        }

        var current = ParentChunk;
        while (current != null)
        {
            var chunk = current.GetFirstChunkOfType<T>(name);
            if (chunk != null && chunk.IndexInParent < currentIndex)
                return chunk;

            currentParentFile = current.ParentFile;
            currentIndex = current.IndexInParent;

            if (currentParentFile != null)
            {
                chunk = currentParentFile.GetFirstChunkOfType<T>(name);
                if (chunk != null && chunk.IndexInParent < currentIndex)
                    return chunk;

                var sets = currentParentFile.GetChunksOfType<SetChunk>(name);
                foreach (var set in sets)
                {
                    if (set.IndexInParent > currentIndex)
                        break;

                    if (set.GetSetType() == chunkType)
                        return (T)set.Children[0];
                }

                return null;
            }

            current = current.ParentChunk;
        }

        return null;
    }

    private readonly Dictionary<uint, uint> _childCounts = [];
    public uint GetChildCount() => (uint)Children.Count;

    public uint GetChildCount(ChunkIdentifier chunkIdentifier) => GetChildCount((uint)chunkIdentifier);

    public uint GetChildCount(uint chunkID) => _childCounts.TryGetValue(chunkID, out var count) ? count : 0;

    public P3DFile? GetP3DFile()
    {
        var chunk = this;
        while (chunk.ParentChunk != null)
            chunk = chunk.ParentChunk;
        return chunk.ParentFile;
    }

    public List<int>? GetChunkHierarchy()
    {
        if (ParentFile == null && ParentChunk == null)
            return null;

        var indices = new List<int>();
        var current = this;
        while (current != null && current.ParentChunk != null)
        {
            indices.Add(current.IndexInParent);
            current = current.ParentChunk;
        }

        if (current?.ParentFile == null)
            return null;
        indices.Add(current.IndexInParent);

        return indices;
    }

    /// <summary>
    /// Writes the chunk's properties to <paramref name="bw"/>.
    /// <para>This should be overwritten for known chunks.</para>
    /// </summary>
    /// <param name="bw">The <c>BinaryWriter</c> to write to.</param>
    protected abstract void WriteData(EndianAwareBinaryWriter bw);

    private void ProcessAddedChild(Chunk child)
    {
        if (_childCounts.ContainsKey(child.ID))
            _childCounts[child.ID]++;
        else
            _childCounts[child.ID] = 1;

        var childType = child.GetType();
        ListHelper.InsertSorted(_chunksByType, childType, child);

        if (child is NamedChunk namedChunk)
            ListHelper.InsertSorted(_namedChunks, (childType, namedChunk.Name), namedChunk);
        else if (child is ParamChunk paramChunk)
            ListHelper.InsertSorted(_paramChunks, (childType, paramChunk.Param), paramChunk);
    }

    private void ProcessRemovedChild(Chunk child)
    {
        if (_childCounts[child.ID] == 1)
            _childCounts.Remove(child.ID);
        else
            _childCounts[child.ID]--;

        var childType = child.GetType();
        ListHelper.CleanListInDictionary(_chunksByType, childType);

        if (child is NamedChunk namedChunk)
            ListHelper.CleanListInDictionary(_namedChunks, (childType, namedChunk.Name));
        else if (child is ParamChunk paramChunk)
            ListHelper.CleanListInDictionary(_paramChunks, (childType, paramChunk.Param));
    }

    public event Action<string>? PropertyChanged;
    internal void OnPropertyChanged(string propertyName) => PropertyChanged?.Invoke(propertyName);

    public event Action<Chunk>? ChildAdded;
    internal void OnChildAdded(Chunk child)
    {
        ProcessAddedChild(child);

        ChildAdded?.Invoke(child);
        PropertyChanged?.Invoke(nameof(Children));
    }

    public event Action<Chunk, int>? ChildRemoved;
    internal void OnChildRemoved(Chunk child, int oldIndex)
    {
        ProcessRemovedChild(child);

        ChildRemoved?.Invoke(child, oldIndex);
        PropertyChanged?.Invoke(nameof(Children));
    }

    public event Action<IReadOnlyList<Chunk>>? ChildrenAdded;
    internal void OnChildrenAdded(IReadOnlyList<Chunk> children)
    {
        foreach (var child in children)
            ProcessAddedChild(child);

        ChildrenAdded?.Invoke(children);
        PropertyChanged?.Invoke(nameof(Children));
    }

    public event Action<IReadOnlyList<(Chunk child, int oldIndex)>>? ChildrenRemoved;
    internal void OnChildrenRemoved(IReadOnlyList<(Chunk child, int oldIndex)> children)
    {
        foreach (var (child, _) in children)
            ProcessRemovedChild(child);

        ChildrenRemoved?.Invoke(children);
        PropertyChanged?.Invoke(nameof(Children));
    }

    public event Action<IReadOnlyList<(Chunk chunk, int oldIndex)>>? ChildrenCleared;
    internal void OnChildrenCleared(IReadOnlyList<(Chunk chunk, int oldIndex)> children)
    {
        _childCounts.Clear();
        _chunksByType.Clear();
        _namedChunks.Clear();
        _paramChunks.Clear();
        ChildrenCleared?.Invoke(children);
        PropertyChanged?.Invoke(nameof(Children));
    }

    /// <summary>
    /// Creates a clone of the current chunk.
    /// </summary>
    /// <returns>A duplicate of the current chunk.</returns>
    protected abstract Chunk CloneSelf();

    public override string ToString() => $"{GetChunkType(this)} (0x{ID:X})";

    /// <summary>
    /// This compares the current <see cref="Chunk"/> to another.
    /// </summary>
    /// <param name="obj">The <see cref="Chunk"/> to compare to.</param>
    /// <returns>Returns <c>true</c> if <paramref name="obj"/> is a <see cref="Chunk"/>, with the same <see cref="ID"/>, <see cref="DataBytes"/> and <see cref="Children"/>.</returns>
    public override bool Equals(object obj)
    {
        if (obj is not Chunk chunk2)
            return false;

        if (ID != chunk2.ID)
            return false;

        var dataLength = DataLength;
        if (dataLength != chunk2.DataLength)
            return false;

        var childCount = GetChildCount();
        if (childCount != chunk2.Children.Count)
            return false;

        var dataBytes = DataBytes;
        var dataBytes2 = chunk2.DataBytes;
        for (int i = 0; i < dataLength; i++)
            if (dataBytes[i] != dataBytes2[i])
                return false;

        for (int i = 0; i < childCount; i++)
        {
            var subChunk = Children[i];
            var subChunk2 = chunk2.Children[i];

            if (!subChunk.Equals(subChunk2))
                return false;
        }

        return true;
    }

    /// <summary>
    /// Generates a unique hash code, taking note of <see cref="ID"/>, <see cref="DataBytes"/> and <see cref="Children"/>.
    /// </summary>
    /// <returns>An <c>int</c> with the hash code.</returns>
    public override int GetHashCode()
    {
        int hash = ID.GetHashCode();
        foreach (var b in DataBytes)
            hash = (hash * 31) + b;
        foreach (var child in Children)
            hash = (hash * 31) + child.GetHashCode();
        return hash;
    }

    /// <summary>
    /// Checks if <paramref name="left"/> is equal to <paramref name="right"/> using <see cref="Equals(object)"/>.
    /// </summary>
    /// <param name="left">The <see cref="Chunk"/> to compare against.</param>
    /// <param name="right">The <see cref="Chunk"/> to compare to.</param>
    /// <returns>Returns <c>true</c> if <paramref name="left"/> is equal to <paramref name="right"/>.</returns>
    public static bool operator ==(Chunk? left, Chunk? right)
    {
        if (left is null && right is null)
            return true;

        if (left is null || right is null)
            return false;

        return EqualityComparer<Chunk>.Default.Equals(left, right);
    }

    /// <summary>
    /// Checks if <paramref name="left"/> is not equal to <paramref name="right"/> using <see cref="Equals(object)"/>.
    /// </summary>
    /// <param name="left">The <see cref="Chunk"/> to compare against.</param>
    /// <param name="right">The <see cref="Chunk"/> to compare to.</param>
    /// <returns>Returns <c>true</c> if <paramref name="left"/> is not equal to <paramref name="right"/>.</returns>
    public static bool operator !=(Chunk? left, Chunk? right) => !(left == right);

    protected static string GetChunkType(Chunk chunk)
    {
        if (Enum.IsDefined(typeof(ChunkIdentifier), (ChunkIdentifier)chunk.ID))
            return ((ChunkIdentifier)chunk.ID).ToString().Replace("_", " ");

        var chunkType = chunk.GetType().Name;
        if (chunkType.EndsWith("Chunk"))
            chunkType = chunkType.Substring(0, chunkType.Length - 5);
        return chunkType;
    }
}

public class UnknownChunk : Chunk
{
    private readonly Action _dataChanged;
    private ObservableByteArray _data;
    /// <summary>
    /// Property <c>Data</c> is the chunk's header data.
    /// </summary>
    public ObservableByteArray Data
    {
        get => _data;
        set
        {
            if (ReferenceEquals(_data, value))
                return;

            var oldSize = HeaderSize;
            if (value is ObservableByteArray oba)
                _data = new(oba.RawArray, _dataChanged);
            else
                _data = new(value?.ToArray() ?? [], _dataChanged);
            RecalculateSize(oldSize);
            _dataChanged();
        }
    }

    public override byte[] DataBytes => Data.RawArray;

    public override uint DataLength => (uint)Data.Length;

    /// <summary>
    /// This constructor initialises an unknown chunk with the given <paramref name="chunkId"/>. It sets <see cref="Data"/> to <paramref name="data"/>.
    /// </summary>
    /// <param name="chunkId">The chunk's identifier. See <seealso cref="ChunkIdentifier"/> for known identifiers.</param>
    /// <param name="data">The chunks's data. Be aware that this must be formatted in the system's default <see cref="Endianness"/>.</param>
    public UnknownChunk(uint chunkId, byte[] data) : base(chunkId)
    {
        ID = chunkId;

        _dataChanged = () => OnPropertyChanged(nameof(Data));
        _data = new((byte[])data.Clone(), _dataChanged);
    }

    protected override void WriteData(EndianAwareBinaryWriter bw) => bw.Write(DataBytes);

    protected override Chunk CloneSelf() => new UnknownChunk(ID, DataBytes);
}