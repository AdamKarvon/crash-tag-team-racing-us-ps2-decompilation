namespace NetP3DLib.P3D.Extensions;
public static class IntegerExtensions
{
    public static bool IsPowerOfTwo(this sbyte value) => value != 0 && (value & (value - 1)) == 0;
    public static bool IsPowerOfTwo(this byte value) => value != 0 && (value & (value - 1)) == 0;
    public static bool IsPowerOfTwo(this short value) => value != 0 && (value & (value - 1)) == 0;
    public static bool IsPowerOfTwo(this ushort value) => value != 0 && (value & (value - 1)) == 0;
    public static bool IsPowerOfTwo(this int value) => value != 0 && (value & (value - 1)) == 0;
    public static bool IsPowerOfTwo(this uint value) => value != 0 && (value & (value - 1)) == 0;
    public static bool IsPowerOfTwo(this long value) => value != 0 && (value & (value - 1)) == 0;
    public static bool IsPowerOfTwo(this ulong value) => value != 0 && (value & (value - 1)) == 0;
}
