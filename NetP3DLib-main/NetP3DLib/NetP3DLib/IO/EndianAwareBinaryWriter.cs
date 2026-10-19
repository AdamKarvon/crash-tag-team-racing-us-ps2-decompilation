using NetP3DLib.P3D.Extensions;
using System;
using System.IO;
using System.Runtime.CompilerServices;
using System.Text;

namespace NetP3DLib.IO;

public class EndianAwareBinaryWriter : BinaryWriter
{
    public Endianness Endianness { get; } = BitConverter.IsLittleEndian ? Endianness.Little : Endianness.Big;
    private readonly bool _swap;

    public EndianAwareBinaryWriter(Stream output, Encoding encoding, bool leaveOpen, Endianness endianness) : base(output, encoding, leaveOpen)
    {
        Endianness = endianness;
        _swap = endianness != (BitConverter.IsLittleEndian ? Endianness.Little : Endianness.Big);
    }

    public EndianAwareBinaryWriter(Stream output) : this(output, Encoding.UTF8, leaveOpen: false, BinaryExtensions.DefaultEndian)
    { }

    public EndianAwareBinaryWriter(Stream output, bool leaveOpen) : this(output, Encoding.UTF8, leaveOpen, BinaryExtensions.DefaultEndian)
    { }

    public EndianAwareBinaryWriter(Stream output, Encoding encoding) : this(output, encoding, leaveOpen: false, BinaryExtensions.DefaultEndian)
    { }

    public EndianAwareBinaryWriter(Stream output, Encoding encoding, bool leaveOpen) : this(output, encoding, leaveOpen, BinaryExtensions.DefaultEndian)
    { }

    public EndianAwareBinaryWriter(Stream output, Endianness endianness) : this(output, Encoding.UTF8, leaveOpen: false, endianness)
    { }

    public EndianAwareBinaryWriter(Stream output, bool leaveOpen, Endianness endianness) : this(output, Encoding.UTF8, leaveOpen, endianness)
    { }

    public EndianAwareBinaryWriter(Stream output, Encoding encoding, Endianness endianness) : this(output, encoding, leaveOpen: false, endianness)
    { }

    [MethodImpl(MethodImplOptions.AggressiveInlining)]
    private static ushort Reverse(ushort value)
    {
        return (ushort)((value >> 8) | (value << 8));
    }

    [MethodImpl(MethodImplOptions.AggressiveInlining)]
    private static uint Reverse(uint value)
    {
        return (value >> 24) |
               ((value >> 8) & 0x0000FF00) |
               ((value << 8) & 0x00FF0000) |
               (value << 24);
    }

    [MethodImpl(MethodImplOptions.AggressiveInlining)]
    private static ulong Reverse(ulong value)
    {
        return (value >> 56) |
               ((value >> 40) & 0x000000000000FF00) |
               ((value >> 24) & 0x0000000000FF0000) |
               ((value >> 8) & 0x00000000FF000000) |
               ((value << 8) & 0x000000FF00000000) |
               ((value << 24) & 0x0000FF0000000000) |
               ((value << 40) & 0x00FF000000000000) |
               (value << 56);
    }
    public override void Write(short value)
    {
        if (_swap)
            value = (short)Reverse((ushort)value);
        base.Write(value);
    }

    public override void Write(ushort value)
    {
        if (_swap)
            value = Reverse(value);
        base.Write(value);
    }

    public override void Write(int value)
    {
        if (_swap)
            value = (int)Reverse((uint)value);
        base.Write(value);
    }

    public override void Write(uint value)
    {
        if (_swap)
            value = Reverse(value);
        base.Write(value);
    }

    public override void Write(long value)
    {
        if (_swap)
            value = (long)Reverse((ulong)value);
        base.Write(value);
    }

    public override void Write(ulong value)
    {
        if (_swap)
            value = Reverse(value);
        base.Write(value);
    }

    public override void Write(float value)
    {
        if (_swap)
        {
            unsafe
            {
                uint temp = *(uint*)&value;
                base.Write(Reverse(temp));
            }
        }
        else
        {
            base.Write(value);
        }
    }

    public override void Write(double value)
    {
        if (_swap)
        {
            unsafe
            {
                ulong temp = *(ulong*)&value;
                base.Write(Reverse(temp));
            }
        }
        else
        {
            base.Write(value);
        }
    }
}
