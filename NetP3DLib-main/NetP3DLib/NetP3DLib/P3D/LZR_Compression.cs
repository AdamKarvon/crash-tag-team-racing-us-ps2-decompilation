using NetP3DLib.P3D.Chunks;
using System;
using System.Collections.Generic;
using System.IO;

namespace NetP3DLib.P3D;

public static class LZR_Compression
{

    /// <summary>
    /// Reads <paramref name="file"/>, and decompresses it if a compressed Pure3D File is detected.
    /// <para>Returns the normal file bytes if not a compressed Pure3D File.</para>
    /// </summary>
    /// <param name="file">The <see cref="BinaryReader"/> to read.</param>
    /// <returns>Returns <paramref name="file"/>'s bytes, decompressed if it was a compressed Pure3D File.</returns>
    public static byte[] DecompressFile(BinaryReader file)
    {
        uint identifier = file.ReadUInt32();
        if (identifier != P3DFile.COMPRESSED_SIGNATURE)
        {
            file.BaseStream.Seek(0, SeekOrigin.Begin);
            return file.ReadBytes((int)file.BaseStream.Length);
        }

        uint uncompressedLength = file.ReadUInt32();
        return Decompress(file, uncompressedLength);
    }

    private static byte[] Decompress(BinaryReader file, uint uncompressedLength)
    {
        byte[] output = new byte[uncompressedLength];
        uint decompressedLength = 0;

        while (decompressedLength < uncompressedLength)
        {
            uint compressedLength = file.ReadUInt32();
            uint blockSize = file.ReadUInt32();
            long blockStart = file.BaseStream.Position;

            DecompressBlock(file, output, decompressedLength, blockSize);
            decompressedLength += blockSize;

            file.BaseStream.Seek(blockStart + compressedLength, SeekOrigin.Begin);
        }
        return output;
    }

    private static void DecompressBlock(BinaryReader br, byte[] output, uint offset, uint size)
    {
        int written = 0;

        while (written < size)
        {
            byte code = br.ReadByte();

            if (code > 15)
            {
                int matchLength = code & 15;

                if (matchLength == 0)
                {
                    matchLength = 15;
                    byte tmp = br.ReadByte();
                    while (tmp == 0)
                    {
                        matchLength += 255;
                        tmp = br.ReadByte();
                    }
                    matchLength += tmp;
                }

                byte tmpByte = br.ReadByte();
                int matchOffset = (code >> 4) | tmpByte << 4;
                int matchPos = (int)(offset + written - matchOffset);

                int len = matchLength >> 2;
                matchLength -= len << 2;

                do
                {
                    output[(int)(offset + written++)] = output[matchPos++];
                    output[(int)(offset + written++)] = output[matchPos++];
                    output[(int)(offset + written++)] = output[matchPos++];
                    output[(int)(offset + written++)] = output[matchPos++];

                    len--;
                } while (len != 0);

                while (matchLength != 0)
                {
                    output[(int)(offset + written++)] = output[matchPos++];

                    matchLength--;
                }
            }
            else
            {
                int runLength = code;

                if (runLength == 0)
                {
                    byte tmp = br.ReadByte();
                    while (tmp == 0)
                    {
                        runLength += 255;
                        tmp = br.ReadByte();
                    }
                    runLength += tmp;

                    br.Read(output, (int)(offset + written), 15);
                    written += 15;
                }

                br.Read(output, (int)(offset + written), runLength);
                written += runLength;
            }
        }
    }

    // TODO: Stop using `List<byte>` for compression
    private const int MINIMUM_MATCH_LENGTH = 4;
    private const int LZR_BLOCK_SIZE = 4096;
    private const int TREE_ROOT = LZR_BLOCK_SIZE;
    private const int UNUSED = LZR_BLOCK_SIZE + 0xffff;

    private struct TreeNode
    {
        public uint Parent;
        public uint Smaller;
        public uint Larger;
    }

    private static readonly TreeNode[] tree = new TreeNode[LZR_BLOCK_SIZE + 1];

    private static void CompressBlock(List<byte> input, uint inputSize, List<byte> output, ref uint outputSize, bool fastest)
    {
        uint inputCount = 0;
        uint outputCount = 0;
        uint literalStart = 0;
        uint literalCount = 0;

        InitTree();

        uint offset = 0;
        uint count = 0;

        while (inputCount < inputSize)
        {
            if ((count > MINIMUM_MATCH_LENGTH) && ((offset & 15) != 0))
            {
                if (literalCount > 0)
                {
                    outputCount += WriteCount(output, literalCount, 0);
                    output.AddRange(input.GetRange((int)literalStart, (int)literalCount));
                    outputCount += literalCount;
                }

                outputCount += WriteCount(output, count, (byte)((offset & 0xf) << 4));
                output.Add((byte)((offset & 0xff0) >> 4));
                outputCount++;

                if (fastest)
                {
                    inputCount += count;
                }
                else
                {
                    uint total = count;
                    bool failed = false;
                    for (int c = 0; c < total; c++)
                    {
                        inputCount++;
                        failed = AddString(input, inputCount, inputSize, ref offset, ref count);
                    }
                }
                literalCount = 0;
                literalStart = inputCount;
            }
            else
            {
                literalCount++;
                inputCount++;
                if (!fastest)
                {
                    if (AddString(input, inputCount, inputSize, ref offset, ref count))
                    {
                        FindLongestMatch(input, inputCount, inputSize, ref offset, ref count);
                    }
                }
            }
            if (fastest)
            {
                AddString(input, inputCount, inputSize, ref offset, ref count);
            }
        }

        if (literalCount > 0)
        {
            outputCount += WriteCount(output, literalCount, 0);
            output.AddRange(input.GetRange((int)literalStart, (int)literalCount));
            outputCount += literalCount;
        }

        outputSize = outputCount;
    }

    /// <summary>
    /// Converts a <see cref="List{T}"/> of bytes to a compressed <see cref="byte"/> array.
    /// <para>Note: This will ONLY write <c>Little Endian</c> files.</para>
    /// </summary>
    /// <param name="input">The list of bytes to compress.</param>
    /// <param name="fast">If <c>true</c>, a slightly faster algorithm will be used.<para>Defaults to <c>false</c>.</para></param>
    /// <returns>A <see cref="byte"/> array containing the compressed bytes of <paramref name="input"/>.</returns>
    /// <exception cref="InvalidDataException">Thrown if <paramref name="input"/> is not a Little Endian Pure3D file.</exception>
    public static byte[] CompressFile(List<byte> input, bool fast = false)
    {
        var identifierBytes = new byte[4]
        {
            input[0],
            input[1],
            input[2],
            input[3],
        };
        uint identifier = BitConverter.ToUInt32(identifierBytes, 0);
        if (identifier != P3DFile.SIGNATURE)
            throw new InvalidDataException("The specified file is not a Little Endian P3D file.");

        uint fileSize = (uint)input.Count;

        List<byte> output = [];
        output.AddRange(BitConverter.GetBytes(P3DFile.COMPRESSED_SIGNATURE));
        output.AddRange(BitConverter.GetBytes(fileSize));

        uint remaining = fileSize;

        uint start = 0;
        while (remaining > 0)
        {
            uint size = Math.Min(LZR_BLOCK_SIZE, remaining);
            List<byte> comp = new((int)size);
            uint compSize = 0;

            CompressBlock(input.GetRange((int)start, (int)remaining), size, comp, ref compSize, fast);

            output.AddRange(BitConverter.GetBytes(compSize));
            output.AddRange(BitConverter.GetBytes(size));
            output.AddRange(comp);

            start += size;
            remaining -= size;
        }

        return [.. output];
    }

    /// <summary>
    /// Converts a <see cref="P3DFile"/> to a compressed <see cref="byte"/> array.
    /// <para>Note: This will ONLY write <c>Little Endian</c> files.</para>
    /// </summary>
    /// <param name="file">The file to compress.</param>
    /// <param name="includeHistory">If <c>true</c>, a history chunk will be added to the start to indicate how and when it was compressed.<para>Defaults to <c>true</c>.</para></param>
    /// <param name="fast">If <c>true</c>, a slightly faster algorithm will be used.<para>Defaults to <c>false</c>.</para></param>
    /// <returns>A <see cref="byte"/> array containing the compressed bytes of <paramref name="file"/>.</returns>
    public static byte[] CompressFile(P3DFile file, bool includeHistory = true, bool fast = false, bool validate = true)
    {
        if (includeHistory)
            file.Chunks.Insert(0, new HistoryChunk(["Compressed with NetP3DLib", $"Run at {DateTime.Now:R}"]));

        using var stream = new MemoryStream();
        file.Write(stream, validate);
        List<byte> input = [.. stream.ToArray()];

        return CompressFile(input, fast);
    }

    private static void InitTree()
    {
        tree[TREE_ROOT].Larger = 0;
        tree[0].Parent = TREE_ROOT;
        tree[0].Larger = UNUSED;
        tree[0].Smaller = UNUSED;
    }

    private static bool AddString(List<byte> input, uint inputCount, uint inputSize, ref uint offset, ref uint count)
    {
        uint testNode = tree[TREE_ROOT].Larger;
        bool failure = false;
        count = 0;

        while (true)
        {
            uint i;
            int delta = 0;

            for (i = 0; i < inputSize - inputCount; i++)
            {
                delta = input[(int)(inputCount + i)] - input[(int)(testNode + i)];
                if (delta != 0)
                {
                    break;
                }
            }

            if (i > count)
            {
                if (((inputCount - testNode) & 15) == 0)
                {
                    failure = true;
                }
                else
                {
                    count = i;
                    offset = inputCount - testNode;
                }
            }

            ref uint child = ref (delta >= 0 ? ref tree[testNode].Larger : ref tree[testNode].Smaller);
            if (child == UNUSED)
            {
                child = inputCount;
                tree[inputCount].Parent = testNode;
                tree[inputCount].Larger = UNUSED;
                tree[inputCount].Smaller = UNUSED;
                return failure;
            }

            testNode = child;
        }
    }

    private static void FindLongestMatch(List<byte> input, uint inputCount, uint inputSize, ref uint offset, ref uint count)
    {
        offset = 0;
        count = 0;

        for (uint o = 0; o < inputCount; o++)
        {
            if (((o - inputCount) & 0xf) == 0)
            {
                continue;
            }

            uint c;
            for (c = 0; (c < inputSize - o) && (c + inputCount < inputSize); c++)
            {
                if (input[(int)(o + c)] == input[(int)(inputCount + c)])
                {
                    if (c + 1 > count)
                    {
                        count = c + 1;
                        offset = o;
                    }
                }
                else
                {
                    break;
                }
            }
        }

        offset = inputCount - offset;
    }

    private static uint WriteCount(List<byte> output, uint count, byte highBits)
    {
        uint outCount = 0;
        if (count > 15)
        {
            output.Add(highBits);
            outCount++;
            count -= 15;
            while (count > 255)
            {
                output.Add(0);
                outCount++;
                count -= 255;
            }
            output.Add((byte)count);
            outCount++;
        }
        else
        {
            output.Add((byte)(highBits | count));
            outCount++;
        }
        return outCount;
    }
}
