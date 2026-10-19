using NetP3DLib.Comparer;
using NetP3DLib.IO;
using NetP3DLib.P3D.Chunks;
using NetP3DLib.P3D.Collections;
using NetP3DLib.P3D.Comparers;
using NetP3DLib.P3D.Enums;
using NetP3DLib.P3D.Extensions;
using NetP3DLib.P3D.Helpers;
using System;
using System.Collections.Concurrent;
using System.Collections.Generic;
using System.Collections.ObjectModel;
using System.IO;

namespace NetP3DLib.P3D;

public class P3DFile
{
    public const uint SIGNATURE = 0xFF443350;
    public const uint SIGNATURE_SWAP = 0x503344FF;
    public const uint COMPRESSED_SIGNATURE = 0x5A443350;
    public const uint COMPRESSED_SIGNATURE_SWAP = 0x5033445A;
    public const uint HEADER_SIZE = sizeof(uint) + sizeof(uint) + sizeof(uint); // ID + HeaderLength + ChunkLength

    /// <summary>
    /// Which padding mode to use for P3D strings.
    /// </summary>
    public static StringPaddingMode StringPaddingMode { get; set; } = StringPaddingMode.LegacyExcludeLength;

    public ChunkFileCollection Chunks { get; private set; }
    public ReadOnlyCollection<Chunk> AllChunks
    {
        get
        {
            var result = new List<Chunk>(Chunks.Count * 4);

            var stack = new Stack<Chunk>(Chunks.Count);
            for (int i = Chunks.Count - 1; i >= 0; i--)
                stack.Push(Chunks[i]);

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

    public uint Size => HEADER_SIZE + Chunks.TotalSize;
    public byte[] Bytes
    {
        get
        {
            List<byte> bytes = [];
            bytes.AddRange(BitConverter.GetBytes(SIGNATURE));
            bytes.AddRange(BitConverter.GetBytes(HEADER_SIZE));
            bytes.AddRange(BitConverter.GetBytes(Size));
            foreach (var chunk in Chunks)
                bytes.AddRange(chunk.Bytes);
            return [.. bytes];
        }
    }

    /// <summary>
    /// When using <see cref="SortChunks"/>, this is the chunk sort order.
    /// <para>Defaults to the order <c>Simpsons: Hit &amp; Run</c> expects.</para>
    /// </summary>
    public static readonly List<uint> ChunkSortPriority = [
        (uint)ChunkIdentifier.History,
        (uint)ChunkIdentifier.Export_Info,
        (uint)ChunkIdentifier.Tree,
        (uint)ChunkIdentifier.Image,
        (uint)ChunkIdentifier.Texture,
        (uint)ChunkIdentifier.Set,
        (uint)ChunkIdentifier.Shader,
        (uint)ChunkIdentifier.Texture_Font,
        (uint)ChunkIdentifier.Image_Font,
        (uint)ChunkIdentifier.Sprite,
        (uint)ChunkIdentifier.Old_Vertex_Anim_Key_Frame,
        (uint)ChunkIdentifier.Animation,
        (uint)ChunkIdentifier.Light,
        (uint)ChunkIdentifier.Light_Group,
        (uint)ChunkIdentifier.Photon_Map,
        (uint)ChunkIdentifier.Camera,
        (uint)ChunkIdentifier.Skeleton,
        (uint)ChunkIdentifier.Lens_Flare_Group,
        (uint)ChunkIdentifier.Particle_System_Factory,
        (uint)ChunkIdentifier.Particle_System,
        (uint)ChunkIdentifier.Old_Billboard_Quad,
        (uint)ChunkIdentifier.Old_Billboard_Quad_Group,
        (uint)ChunkIdentifier.Mesh,
        (uint)ChunkIdentifier.Physics_Object,
        (uint)ChunkIdentifier.Collision_Object,
        (uint)ChunkIdentifier.Anim_Obj_Wrapper,
        (uint)ChunkIdentifier.Breakable_Object,
        (uint)ChunkIdentifier.Inst_Particle_System,
        (uint)ChunkIdentifier.Anim_Dyna_Phys_Wrapper,
        (uint)ChunkIdentifier.Collision_Effect,
        (uint)ChunkIdentifier.Instance_List,
        (uint)ChunkIdentifier.Anim_Dyna_Phys,
        (uint)ChunkIdentifier.Anim_Coll,
        (uint)ChunkIdentifier.Skin,
        (uint)ChunkIdentifier.Expression_Group,
        (uint)ChunkIdentifier.Expression_Mixer,
        (uint)ChunkIdentifier.Wall,
        (uint)ChunkIdentifier.Fenceline,
        (uint)ChunkIdentifier.Fence,
        (uint)ChunkIdentifier.Intersection,
        (uint)ChunkIdentifier.Road_Data_Segment,
        (uint)ChunkIdentifier.Road_Segment,
        (uint)ChunkIdentifier.Road,
        (uint)ChunkIdentifier.Path,
        (uint)ChunkIdentifier.Anim,
        (uint)ChunkIdentifier.Locator,
        (uint)ChunkIdentifier.Spline,
        (uint)ChunkIdentifier.Composite_Drawable,
        (uint)ChunkIdentifier.Animated_Object_Factory,
        (uint)ChunkIdentifier.Animated_Object,
        (uint)ChunkIdentifier.Scenegraph,
        (uint)ChunkIdentifier.Frame_Controller,
        (uint)ChunkIdentifier.Old_Frame_Controller,
        (uint)ChunkIdentifier.Multi_Controller,
        (uint)ChunkIdentifier.Old_Locator,
        (uint)ChunkIdentifier.Game_Attr,
        (uint)ChunkIdentifier.Smart_Prop,
        (uint)ChunkIdentifier.State_Prop_Data_V1,
    ];

    public P3DFile()
    {
        Chunks = new(this);
    }

    public P3DFile(string filePath) : this()
    {
        if (!File.Exists(filePath))
            throw new FileNotFoundException($"Could not find the specified file.", filePath);

        using var fs = new FileStream(filePath, FileMode.Open, FileAccess.Read, FileShare.Read);

        LoadFromStream(fs, false);
    }

    public P3DFile(Stream stream) : this()
    {
        if (!stream.CanRead)
            throw new InvalidDataException("Could not read from stream.");

        LoadFromStream(stream, false);
    }

    private void LoadFromStream(Stream stream, bool leaveOpen)
    {
        if (stream.Length < HEADER_SIZE)
            throw new InvalidDataException($"Specified file too short. Must be at least {HEADER_SIZE} bytes.");
        if (stream.Length > int.MaxValue)
            throw new InvalidDataException($"Specified file too long. Must be fewer than {int.MaxValue} bytes.");

        byte[] signatureBuffer = new byte[4];
        if (stream.Read(signatureBuffer, 0, 4) != 4)
            throw new InvalidDataException("Failed to read file signature.");
        uint signature = BitConverter.ToUInt32(signatureBuffer, 0);

        var endianness = (signature == SIGNATURE_SWAP || signature == COMPRESSED_SIGNATURE_SWAP) ? BinaryExtensions.SwappedEndian : BinaryExtensions.DefaultEndian;

        EndianAwareBinaryReader? br = null;
        MemoryStream? ms = null;

        try
        {
            switch (signature)
            {
                case SIGNATURE:
                case SIGNATURE_SWAP:
                    br = new(stream, true, endianness);
                    break;
                case COMPRESSED_SIGNATURE:
                case COMPRESSED_SIGNATURE_SWAP:
                    stream.Position = 0;
                    using (EndianAwareBinaryReader br2 = new(stream, true, endianness))
                    {
                        byte[] decryptedBytes = LZR_Compression.DecompressFile(br2);
                        ms = new(decryptedBytes, true);
                        br = new(ms, true, endianness);

                        if (br.ReadUInt32() != SIGNATURE)
                            throw new InvalidDataException($"Decompressed file signature 0x{signature:X} is invalid.");
                    }
                    break;
                default:
                    throw new InvalidDataException($"File signature 0x{signature:X} is invalid.");
            }

            uint headerSize = br.ReadUInt32();
            if (headerSize != HEADER_SIZE)
                throw new InvalidDataException($"P3D File header size incorrect. Expected {HEADER_SIZE}, read {headerSize}.");

            uint fileSize = br.ReadUInt32();
            if (fileSize != br.BaseStream.Length)
                throw new InvalidDataException($"P3D File full size incorrect. Expected {br.BaseStream.Length}, read {fileSize}.");

            uint bytesRead = headerSize;
            if (bytesRead < fileSize)
            {
                Chunks = new(this, (int)((fileSize - bytesRead) / 12));
                while (bytesRead < fileSize)
                {
                    Chunk c = ChunkLoader.LoadChunk(br, out var childBytesRead);
                    if (childBytesRead == 0)
                        throw new InvalidDataException("Child chunk consumed 0 bytes.");
                    bytesRead += childBytesRead;
                    Chunks.Add(c);
                }
            }
        }
        finally
        {
            br?.Dispose();
            if (!leaveOpen)
                stream.Dispose();
            ms?.Dispose();
        }
    }

    private static readonly ConcurrentDictionary<uint, string> _chunkNameCache = new();
    private static string GetCachedChunkName(uint id)
    {
        if (_chunkNameCache.TryGetValue(id, out var name))
            return name;

        name = ((ChunkIdentifier)id).ToString().Replace("_", " ");
        _chunkNameCache[id] = name;
        return name;
    }
    private readonly struct ChunkGroup
    {
        public readonly uint Id;
        public readonly List<Chunk> Chunks;
        public ChunkGroup(uint id, List<Chunk> chunks)
        {
            Id = id;
            Chunks = chunks;
        }
    }
    /// <summary>
    /// Sort <see cref="Chunks"/> in the order specified in <see cref="ChunkSortPriority"/>.
    /// <para>Chunk IDs without a sort priority will remain in their original order at the end of the file.</para>
    /// </summary>
    /// <param name="includeSectionHeaders">If <c>true</c>, will add a History chunk before each chunk ID.</param>
    /// <param name="alphabetical">If <c>true</c>, named chunks will be further sorted into alphabetical order.</param>
    /// <param name="includeHistory">If <c>true</c>, a history chunk will be added to the start to indicate how and when it was sorted.<para>Defaults to <c>true</c>.</para></param>
    /// <param name="caseInsensitive">If <c>true</c>, and <paramref name="alphabetical"/> is <c>true</c>, named chunk sorting will be case insensitive</param>
    [System.Diagnostics.CodeAnalysis.SuppressMessage("Style", "IDE0220:Add explicit cast", Justification = "As the list is all chunks of the same type, if the first is a LocatorChunk, the rest will be.")]
    public void SortChunks(bool includeSectionHeaders = false, bool alphabetical = false, bool includeHistory = true, bool caseInsensitive = false)
    {
        List<Chunk> newChunks = new(Chunks.Count + (includeSectionHeaders ? _chunksByType.Count + 20 : 0));

        if (includeHistory)
            Chunks.Add(new HistoryChunk(["Sorted with NetP3DLib", $"Run at {DateTime.Now:R}"]));

        var comparer = new NaturalComparer(caseInsensitive);

        Dictionary<uint, int> priorityMap = new(ChunkSortPriority.Count);
        for (var i = 0; i < ChunkSortPriority.Count; i++)
            priorityMap[ChunkSortPriority[i]] = i;

        List<ChunkGroup> groups = new(_chunksByType.Count);
        foreach (var entry in _chunksByType)
        {
            var type = entry.Key;
            var list = entry.Value;

            if (list.Count == 0)
                continue;

            if (ChunkLoader.ChunkTypeMap.TryGetValue(type, out var id))
            {
                groups.Add(new(id, list));
            }
            else
            {
                Dictionary<uint, List<Chunk>> subGroups = [];
                foreach (var chunk in list)
                {
                    if (!subGroups.TryGetValue(chunk.ID, out var subList))
                    {
                        subList = [];
                        subGroups[chunk.ID] = subList;
                    }
                    subList.Add(chunk);
                }

                foreach (var subGroup in subGroups)
                    groups.Add(new(subGroup.Key, subGroup.Value));
            }
        }

        groups.Sort((a, b) =>
        {
            var aInList = priorityMap.TryGetValue(a.Id, out int indexA);
            var bInList = priorityMap.TryGetValue(b.Id, out int indexB);

            if (aInList && bInList)
                return indexA.CompareTo(indexB);
            if (aInList)
                return -1;
            if (bInList)
                return 1;

            return a.Id.CompareTo(b.Id);
        });

        foreach (var group in groups)
        {
            var chunks = group.Chunks;
            var id = group.Id;

            if (chunks.Count == 0)
                continue;

            if (chunks[0] is HistoryChunk)
            {
                for (int i = chunks.Count - 1; i >= 0; i--)
                {
                    var historyChunk = (HistoryChunk)chunks[i];
                    if (historyChunk.NumHistory != 1)
                        continue;

                    var text = historyChunk.History[0];
                    var hexStart = text.LastIndexOf(" (0x", StringComparison.Ordinal);
                    if (hexStart < 0 || !text.EndsWith(")", StringComparison.Ordinal))
                        continue;

                    var typeName = text.Substring(0, hexStart);
                    var hexPart = text.Substring(hexStart + 4, text.Length - hexStart - 5);

                    if (uint.TryParse(hexPart, System.Globalization.NumberStyles.HexNumber, null, out uint typeID))
                    {
                        var expectedName = GetCachedChunkName(typeID);
                        if (((ChunkIdentifier)typeID == ChunkIdentifier.Locator && typeName.IndexOf("Locator", StringComparison.Ordinal) >= 0) || expectedName == typeName)
                            chunks.RemoveAt(i);
                    }
                }

                if (chunks.Count == 0)
                    continue;
            }
            else if (chunks[0] is LocatorChunk)
            {
                if (!includeSectionHeaders && !alphabetical)
                {
                    newChunks.AddRange(chunks);
                    continue;
                }

                Dictionary<uint, List<LocatorChunk>> locatorChunksByType = [];
                foreach (LocatorChunk locatorChunk in chunks)
                {
                    if (!locatorChunksByType.TryGetValue((uint)locatorChunk.LocatorType, out var locatorList))
                    {
                        locatorList = [];
                        locatorChunksByType[(uint)locatorChunk.LocatorType] = locatorList;
                    }
                    locatorList.Add(locatorChunk);
                }

                List<uint> locatorTypes = [.. locatorChunksByType.Keys];
                locatorTypes.Sort();

                foreach (uint locatorType in locatorTypes)
                {
                    var typeChunks = locatorChunksByType[locatorType];

                    if (includeSectionHeaders)
                        newChunks.Add(new HistoryChunk([$"{typeChunks[0].LocatorType} Locator (Type {locatorType}) (0x{id:X})"]));

                    if (alphabetical)
                        typeChunks.Sort((x, y) => comparer.Compare(x.Name, y.Name));

                    newChunks.AddRange(typeChunks);
                }

                continue;
            }

            if (includeSectionHeaders)
                newChunks.Add(new HistoryChunk([$"{GetCachedChunkName(id)} (0x{id:X})"]));

            if (alphabetical && chunks[0] is NamedChunk)
                chunks.Sort((x, y) => comparer.Compare(((NamedChunk)x).Name, ((NamedChunk)y).Name));

            newChunks.AddRange(chunks);
        }

        Chunks.Clear();
        Chunks.AddRange(newChunks);
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

    private readonly Dictionary<uint, uint> _chunkCounts = [];
    public uint GetChunkCount() => (uint)Chunks.Count;

    public uint GetChunkCount(ChunkIdentifier chunkIdentifier) => GetChunkCount((uint)chunkIdentifier);

    public uint GetChunkCount(uint chunkID) => _chunkCounts.TryGetValue(chunkID, out var count) ? count : 0;

    private void ProcessAddedChunk(Chunk chunk)
    {
        if (_chunkCounts.ContainsKey(chunk.ID))
            _chunkCounts[chunk.ID]++;
        else
            _chunkCounts[chunk.ID] = 1;

        var chunkType = chunk.GetType();
        ListHelper.InsertSorted(_chunksByType, chunkType, chunk);

        if (chunk is NamedChunk namedChunk)
            ListHelper.InsertSorted(_namedChunks, (chunkType, namedChunk.Name), namedChunk);
        else if (chunk is ParamChunk paramChunk)
            ListHelper.InsertSorted(_paramChunks, (chunkType, paramChunk.Param), paramChunk);
    }

    private void ProcessRemovedChunk(Chunk chunk)
    {
        if (_chunkCounts[chunk.ID] == 1)
            _chunkCounts.Remove(chunk.ID);
        else
            _chunkCounts[chunk.ID]--;

        var chunkType = chunk.GetType();
        ListHelper.CleanListInDictionary(_chunksByType, chunkType);

        if (chunk is NamedChunk namedChunk)
            ListHelper.CleanListInDictionary(_namedChunks, (chunkType, namedChunk.Name));
        else if (chunk is ParamChunk paramChunk)
            ListHelper.CleanListInDictionary(_paramChunks, (chunkType, paramChunk.Param));
    }

    public event Action<Chunk>? ChunkAdded;
    internal void OnChunkAdded(Chunk chunk)
    {
        ProcessAddedChunk(chunk);

        ChunkAdded?.Invoke(chunk);
    }

    public event Action<Chunk, int>? ChunkRemoved;
    internal void OnChunkRemoved(Chunk chunk, int oldIndex)
    {
        ProcessRemovedChunk(chunk);

        ChunkRemoved?.Invoke(chunk, oldIndex);
    }

    public event Action<IReadOnlyList<Chunk>>? ChunksAdded;
    internal void OnChunksAdded(IReadOnlyList<Chunk> chunks)
    {
        foreach (var chunk in chunks)
            ProcessAddedChunk(chunk);

        ChunksAdded?.Invoke(chunks);
    }

    public event Action<IReadOnlyList<(Chunk chunk, int oldIndex)>>? ChunksRemoved;
    internal void OnChunksRemoved(IReadOnlyList<(Chunk chunk, int oldIndex)> chunks)
    {
        foreach (var (chunk, _) in chunks)
            ProcessRemovedChunk(chunk);

        ChunksRemoved?.Invoke(chunks);
    }

    public event Action<IReadOnlyList<(Chunk chunk, int oldIndex)>>? ChunksCleared;
    internal void OnChunksCleared(IReadOnlyList<(Chunk chunk, int oldIndex)> children)
    {
        _chunkCounts.Clear();
        _chunksByType.Clear();
        _namedChunks.Clear();
        _paramChunks.Clear();
        ChunksCleared?.Invoke(children);
    }

    public P3DFile Clone()
    {
        var clone = new P3DFile();
        foreach (var child in Chunks)
            clone.Chunks.Add(child.Clone());
        return clone;
    }

    public void Write(string filePath) => Write(filePath, BinaryExtensions.DefaultEndian, true);

    public void Write(string filePath, bool validate) => Write(filePath, BinaryExtensions.DefaultEndian, validate);

    public void Write(string filePath, Endianness endianness) => Write(filePath, endianness, true);

    public void Write(string filePath, Endianness endianness, bool validate)
    {
        using FileStream fs = File.Open(filePath, FileMode.Create, FileAccess.ReadWrite, FileShare.None);
        Write(fs, endianness, validate);
    }

    public void Write(Stream stream) => Write(stream, BinaryExtensions.DefaultEndian, true);

    public void Write(Stream stream, bool validate) => Write(stream, BinaryExtensions.DefaultEndian, validate);

    public void Write(Stream stream, Endianness endianness) => Write(stream, endianness, true);

    public void Write(Stream stream, Endianness endianness, bool validate)
    {
        if (!stream.CanWrite)
            throw new ArgumentException("Cannot write to stream.", nameof(stream));

        if (validate)
            foreach (var c in Chunks)
                foreach (var error in c.ValidateChunks())
                    throw error;

        EndianAwareBinaryWriter bw = new(stream, endianness);

        bw.Write(SIGNATURE);
        bw.Write(HEADER_SIZE);
        bw.Write(Size);
        foreach (Chunk chunk in Chunks)
            chunk.Write(bw);
        bw.Flush();
    }

    public void Compress(string filePath, bool includeHistory = true, bool fast = false, bool validate = true)
    {
        using FileStream fs = File.Open(filePath, FileMode.Create, FileAccess.ReadWrite, FileShare.None);
        Compress(fs, includeHistory, fast, validate);
    }

    public void Compress(Stream stream, bool includeHistory = true, bool fast = false, bool validate = true)
    {
        if (!stream.CanWrite)
            throw new ArgumentException("Cannot write to stream.", nameof(stream));

        var compressedBytes = LZR_Compression.CompressFile(this, includeHistory, fast, validate);
        stream.Write(compressedBytes, 0, compressedBytes.Length);
        stream.Flush();
    }

    public override string ToString()
    {
        int totalChunks = Chunks.Count;
        int totalSubChunks = AllChunks.Count - totalChunks;

        return $"P3DFile: {totalChunks:N0} root chunk{(totalChunks != 1 ? "s" : "")}; {totalSubChunks:N0} subchunk{(totalSubChunks != 1 ? "s" : "")}; {Size:N0} bytes";
    }
}
