using NetP3DLib.IO;
using NetP3DLib.P3D.Attributes;

#if DEBUG
using NetP3DLib.P3D.Enums;
#endif
using System;
using System.Collections.Generic;
#if DEBUG
using System.Diagnostics;
#endif
using System.IO;
using System.Linq.Expressions;
using System.Reflection;

namespace NetP3DLib.P3D;

public static partial class ChunkLoader
{
    public static readonly Dictionary<uint, (Type, Func<EndianAwareBinaryReader, uint, Chunk>)> ChunkTypes;
    public static readonly Dictionary<Type, uint> ChunkTypeMap;
#if DEBUG
    public static HashSet<uint> UnknownChunks = [];
#endif

    static ChunkLoader()
    {
        ChunkTypes = [];
        ChunkTypeMap = [];

        LoadInternalChunks();
#if DEBUG
        Console.WriteLine($"Known chunk identifiers: {Enum.GetValues(typeof(ChunkIdentifier)).Length}. Loaded chunk classes: {ChunkTypes.Count}.");
        foreach (var chunkIdentifier in Enum.GetValues(typeof(ChunkIdentifier)))
            if (!ChunkTypes.ContainsKey((uint)chunkIdentifier))
                Console.WriteLine($"\tMissing class for identifier: {chunkIdentifier} (0x{(uint)chunkIdentifier:X})");
#endif
    }

    /// <summary>
    /// Loads chunk definitions from the given <paramref name="nameSpace"/>.
    /// <para>If <paramref name="throwOnDuplicate"/> is <c>true</c>, <see cref="InvalidOperationException"/> will be thrown if there is already a chunk with a matching identifier loaded.</para>
    /// <para>if <paramref name="throwOnInvalid"/> is <c>true</c>, <see cref="InvalidOperationException"/> will be thrown if a chunk class in the namespace is invalid.</para>
    /// </summary>
    /// <param name="nameSpace">The namespace to load chunk definitions from.</param>
    /// <param name="throwOnDuplicate">If <c>true</c> duplicate chunks will error. If <c>false</c> duplicate chunks will overwrite.</param>
    /// <param name="throwOnInvalid">If <c>true</c> invalid chunks will error. If <c>false</c> invalid chunks will be ignored.</param>
    /// <exception cref="ArgumentException">Thrown if the <paramref name="nameSpace"/> is null or whitespace.</exception>
    /// <exception cref="InvalidOperationException">Thrown is <paramref name="throwOnDuplicate"/> or <paramref name="throwOnInvalid"/> are <c>true</c> and an invalid chunk is encountered.</exception>
    public static void LoadChunkTypes(string nameSpace, bool throwOnDuplicate = true, bool throwOnInvalid = true, Assembly? assembly = null)
    {
        if (string.IsNullOrWhiteSpace(nameSpace))
            throw new ArgumentException($"{nameof(nameSpace)} cannot be null.", nameof(nameSpace));

        assembly ??= Assembly.GetExecutingAssembly();
        var chunkTypeList = GetTypesInNamespace(assembly, nameSpace);
        Type baseType = typeof(Chunk);

        foreach (Type chunkType in chunkTypeList)
        {
            if (chunkType.IsNested)
                continue;

            if (!chunkType.IsSubclassOf(baseType))
            {
                if (throwOnInvalid)
                    throw new InvalidOperationException($"Type {chunkType.FullName} is not a subclass of {baseType.FullName}.");

#if DEBUG
                Console.WriteLine($"Type {chunkType.FullName} is not a subclass of {baseType.Namespace}. Skipping.");
#endif
                continue;
            }

            ChunkAttributes chunkAttriutes = chunkType.GetCustomAttribute<ChunkAttributes>(false);
            if (chunkAttriutes == null)
            {
                if (throwOnInvalid)
                    throw new InvalidOperationException($"Type {chunkType.FullName} does not have a `ChunkAttributes` attribute.");

#if DEBUG
                Console.WriteLine($"Type {chunkType.FullName} does not have a `ChunkAttributes` attribute. Skipping.");
#endif
                continue;
            }

            if (throwOnDuplicate && ChunkTypes.ContainsKey(chunkAttriutes.Identifier))
                throw new InvalidOperationException($"Chunk type with identifier 0x{chunkAttriutes.Identifier:X} already exists.");


            var ctor1 = chunkType.GetConstructor([typeof(EndianAwareBinaryReader)]);
            var ctor2 = chunkType.GetConstructor([typeof(EndianAwareBinaryReader), typeof(uint)]);
            if (ctor1 == null && ctor2 == null)
            {
                if (throwOnInvalid)
                    throw new InvalidOperationException($"Type {chunkType.FullName} must have a constructor with (EndianAwareBinaryReader) or (EndianAwareBinaryReader,uint).");

#if DEBUG
                Console.WriteLine($"[ChunkLoader] Skipped: {chunkType.FullName} lacks expected constructor.");
#endif
                continue;
            }

            var brParam = Expression.Parameter(typeof(EndianAwareBinaryReader), "br");
            var headerParam = Expression.Parameter(typeof(uint), "headerSize");

            Expression newExpr;
            if (ctor2 != null)
            {
                newExpr = Expression.New(ctor2, brParam, headerParam);
            }
            else
            {
                newExpr = Expression.New(ctor1, brParam);
            }

            var lambda = Expression.Lambda<Func<EndianAwareBinaryReader, uint, Chunk>>(newExpr, brParam, headerParam);
            ChunkTypes[chunkAttriutes.Identifier] = (chunkType, lambda.Compile());
            ChunkTypeMap[chunkType] = chunkAttriutes.Identifier;
        }
    }

    /// <summary>
    /// Loads a chunk from a <see cref="BinaryReader"/>. Assumes the stream position is at the start of the chunk.
    /// </summary>
    /// <param name="br">The <see cref="BinaryReader"/> to read from. <see cref="EndianAwareBinaryReader"/> is preferred.</param>
    /// <returns>A known chunk class if one exists in <see cref="ChunkTypes"/>, or <see cref="UnknownChunk"/> otherwise.</returns>
    public static Chunk LoadChunk(EndianAwareBinaryReader br, out uint bytesRead)
    {
        uint chunkId = br.ReadUInt32();
        uint headerSize = br.ReadUInt32() - P3DFile.HEADER_SIZE;
        uint chunkSize = br.ReadUInt32();

        Chunk c;

        uint actualHeaderSize;
        if (ChunkTypes.TryGetValue(chunkId, out var entry))
        {
            var (chunkType, chunkConstructor) = entry;

            var startPos = br.BaseStream.Position;
            var expectedEndPos = startPos + headerSize;

            c = chunkConstructor(br, headerSize);

            var actualEndPos = br.BaseStream.Position;

            if (actualEndPos > expectedEndPos)
                throw new InvalidDataException($"Chunk {c} read {actualEndPos - expectedEndPos} bytes past its header boundary.");

            actualHeaderSize = (uint)(actualEndPos - startPos);
        }
        else
        {
            c = new UnknownChunk(chunkId, br.ReadBytes((int)headerSize));
            actualHeaderSize = headerSize;
#if DEBUG
            UnknownChunks.Add(chunkId);
#endif
        }

#if DEBUG
        if (headerSize != actualHeaderSize)
        {
            if (chunkId != (uint)ChunkIdentifier.Physics_Object && chunkId != (uint)ChunkIdentifier.Animation_Header)
                Debugger.Break();
        }
#endif

        bytesRead = actualHeaderSize + P3DFile.HEADER_SIZE;
        if (bytesRead < chunkSize)
        {
            c.Children = new(c, (int)((chunkSize - bytesRead) / 12));
            while (bytesRead < chunkSize)
            {
                Chunk subChunk = LoadChunk(br, out var childBytesRead);

                if (childBytesRead == 0)
                    throw new InvalidDataException("Child chunk consumed 0 bytes.");

                bytesRead += childBytesRead;
                c.Children.Add(subChunk);
            }
        }

        return c;
    }

    private static IReadOnlyList<Type> GetTypesInNamespace(Assembly assembly, string nameSpace)
    {
        var types = new List<Type>();
        try
        {
            foreach (var type in assembly.GetTypes())
                if (type?.Namespace == nameSpace)
                    types.Add(type);
        }
        catch (ReflectionTypeLoadException ex)
        {
            var loaderExceptions = ex.LoaderExceptions;
            foreach (var loaderException in loaderExceptions)
                Console.WriteLine(loaderException);

            foreach (var type in ex.Types)
                if (type?.Namespace == nameSpace)
                    types.Add(type);
        }
        return types;
    }
}
