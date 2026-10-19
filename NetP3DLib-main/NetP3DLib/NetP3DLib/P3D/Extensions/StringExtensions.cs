using System.Text;

namespace NetP3DLib.P3D.Extensions;
public static class StringExtensions
{
    public static bool IsValidP3DString(this string? str)
    {
        if (str == null)
            return false;
        if (Encoding.UTF8.GetByteCount(str) > 255)
            return false;

        return true;
    }

    public static bool IsValidP3DLongString(this string? str)
    {
        if (str == null)
            return false;
        if (Encoding.UTF8.GetByteCount(str) > int.MaxValue - 4)
            return false;

        return true;
    }

    public static bool IsValidFourCC(this string? str)
    {
        if (str == null)
            return false;
        var count = Encoding.UTF8.GetByteCount(str);
        //if (count < 1)
        //    return false;
        if (count > 4)
            return false;

        return true;
    }
}
