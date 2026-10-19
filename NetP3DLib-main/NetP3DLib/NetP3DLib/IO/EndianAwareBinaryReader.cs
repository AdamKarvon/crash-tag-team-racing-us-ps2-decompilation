using NetP3DLib.P3D.Extensions;
using System;
using System.IO;
using System.Runtime.CompilerServices;
using System.Text;

namespace NetP3DLib.IO;

public sealed class EndianAwareBinaryReader : BinaryReader
{
    public Endianness Endianness { get; } = BitConverter.IsLittleEndian ? Endianness.Little : Endianness.Big;
    private readonly bool _swap;

    public EndianAwareBinaryReader(Stream input, Encoding encoding, bool leaveOpen, Endianness endianness) : base(input, encoding, leaveOpen)
    {
        Endianness = endianness;
        _swap = endianness != (BitConverter.IsLittleEndian ? Endianness.Little : Endianness.Big);
    }

    public EndianAwareBinaryReader(Stream input) : this(input, Encoding.UTF8, leaveOpen: false, BinaryExtensions.DefaultEndian)
    { }

    public EndianAwareBinaryReader(Stream input, bool leaveOpen) : this(input, Encoding.UTF8, leaveOpen, BinaryExtensions.DefaultEndian)
    { }

    public EndianAwareBinaryReader(Stream input, Encoding encoding) : this(input, encoding, leaveOpen: false, BinaryExtensions.DefaultEndian)
    { }

    public EndianAwareBinaryReader(Stream input, Encoding encoding, bool leaveOpen) : this(input, encoding, leaveOpen, BinaryExtensions.DefaultEndian)
    { }

    public EndianAwareBinaryReader(Stream input, Endianness endianness) : this(input, Encoding.UTF8, leaveOpen: false, endianness)
    { }

    public EndianAwareBinaryReader(Stream input, bool leaveOpen, Endianness endianness) : this(input, Encoding.UTF8, leaveOpen, endianness)
    { }

    public EndianAwareBinaryReader(Stream input, Encoding encoding, Endianness endianness) : this(input, encoding, leaveOpen: false, endianness)
    { }

    [MethodImpl(MethodImplOptions.AggressiveInlining)]
    public new short ReadInt16()
    {
        short val = base.ReadInt16();
        return _swap ? (short)((val >> 8) | (val << 8)) : val;
    }

    [MethodImpl(MethodImplOptions.AggressiveInlining)]
    public new ushort ReadUInt16()
    {
        ushort val = base.ReadUInt16();
        return _swap ? (ushort)((val >> 8) | (val << 8)) : val;
    }

    [MethodImpl(MethodImplOptions.AggressiveInlining)]
    public new int ReadInt32()
    {
        int val = base.ReadInt32();
        return _swap ? (int)(((uint)val >> 24) | (((uint)val >> 8) & 0x0000FF00) | (((uint)val << 8) & 0x00FF0000) | ((uint)val << 24)) : val;
    }

    [MethodImpl(MethodImplOptions.AggressiveInlining)]
    public new uint ReadUInt32()
    {
        uint val = base.ReadUInt32();
        return _swap ? (val >> 24) | ((val >> 8) & 0x0000FF00) | ((val << 8) & 0x00FF0000) | (val << 24) : val;
    }

    [MethodImpl(MethodImplOptions.AggressiveInlining)]
    public new long ReadInt64()
    {
        long val = base.ReadInt64();
        if (!_swap)
            return val;

        ulong uval = (ulong)val;
        uval = (uval >> 56) |
               ((uval >> 40) & 0x000000000000FF00UL) |
               ((uval >> 24) & 0x0000000000FF0000UL) |
               ((uval >> 8) & 0x00000000FF000000UL) |
               ((uval << 8) & 0x000000FF00000000UL) |
               ((uval << 24) & 0x0000FF0000000000UL) |
               ((uval << 40) & 0x00FF000000000000UL) |
               (uval << 56);
        return (long)uval;
    }

    [MethodImpl(MethodImplOptions.AggressiveInlining)]
    public new ulong ReadUInt64()
    {
        ulong val = base.ReadUInt64();
        if (!_swap)
            return val;

        return (val >> 56) |
               ((val >> 40) & 0x000000000000FF00UL) |
               ((val >> 24) & 0x0000000000FF0000UL) |
               ((val >> 8) & 0x00000000FF000000UL) |
               ((val << 8) & 0x000000FF00000000UL) |
               ((val << 24) & 0x0000FF0000000000UL) |
               ((val << 40) & 0x00FF000000000000UL) |
               (val << 56);
    }

    [MethodImpl(MethodImplOptions.AggressiveInlining)]
    public new float ReadSingle()
    {
        if (!_swap)
            return base.ReadSingle();

        uint val = base.ReadUInt32();
        val = (val >> 24) | ((val >> 8) & 0x0000FF00) | ((val << 8) & 0x00FF0000) | (val << 24);

        unsafe
        {
            return *(float*)&val;
        }
    }

    [MethodImpl(MethodImplOptions.AggressiveInlining)]
    public new double ReadDouble()
    {
        if (!_swap)
            return base.ReadDouble();

        ulong val = base.ReadUInt64();
        val = (val >> 56) |
              ((val >> 40) & 0x000000000000FF00UL) |
              ((val >> 24) & 0x0000000000FF0000UL) |
              ((val >> 8) & 0x00000000FF000000UL) |
              ((val << 8) & 0x000000FF00000000UL) |
              ((val << 24) & 0x0000FF0000000000UL) |
              ((val << 40) & 0x00FF000000000000UL) |
              (val << 56);

        unsafe
        {
            return *(double*)&val;
        }
    }
}
