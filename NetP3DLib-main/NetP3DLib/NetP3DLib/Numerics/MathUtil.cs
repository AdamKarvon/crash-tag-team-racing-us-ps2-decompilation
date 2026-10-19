using System;

namespace NetP3DLib.Numerics;

/// <summary>
/// Mostly yoinked from <see href="https://github.com/sharpdx/SharpDX/blob/master/Source/SharpDX.Mathematics/MathUtil.cs" langword=" (SharpDX.MathUtil)"/>.
/// </summary>
public static class MathUtil
{
    /// <summary>
    /// The value for which all absolute numbers smaller than are considered equal to zero.
    /// </summary>
    public const float ZeroTolerance = 1e-6f; // Value a 8x higher than 1.19209290E-07F

    /// <summary>
    /// Checks if a and b are almost equals, taking into account the magnitude of floating point numbers (unlike <see cref="WithinEpsilon"/> method). See Remarks.
    /// See remarks.
    /// </summary>
    /// <param name="a">The left value to compare.</param>
    /// <param name="b">The right value to compare.</param>
    /// <returns><c>true</c> if a almost equal to b, <c>false</c> otherwise</returns>
    /// <remarks>
    /// The code is using the technique described by Bruce Dawson in 
    /// <a href="http://randomascii.wordpress.com/2012/02/25/comparing-floating-point-numbers-2012-edition/">Comparing Floating point numbers 2012 edition</a>. 
    /// </remarks>
    public static bool NearEqual(float a, float b)
    {
        // Check if the numbers are really close -- needed
        // when comparing numbers near zero.
        if (IsZero(a - b))
            return true;

        // Original from Bruce Dawson: http://randomascii.wordpress.com/2012/02/25/comparing-floating-point-numbers-2012-edition/
        int aInt = FloatToInt32Bits(a);
        int bInt = FloatToInt32Bits(b);

        // Different signs means they do not match.
        if ((aInt < 0) != (bInt < 0))
            return false;

        // Find the difference in ULPs.
        int ulp = Math.Abs(aInt - bInt);

        // Choose of maxUlp = 4
        // according to http://code.google.com/p/googletest/source/browse/trunk/include/gtest/internal/gtest-internal.h
        const int maxUlp = 4;
        return (ulp <= maxUlp);
    }

    /// <summary>
    /// Determines whether the specified value is close to zero (0.0f).
    /// </summary>
    /// <param name="a">The floating value.</param>
    /// <returns><c>true</c> if the specified value is close to zero (0.0f); otherwise, <c>false</c>.</returns>
    public static bool IsZero(float a)
    {
        return Math.Abs(a) < ZeroTolerance;
    }

    /// <summary>
    /// Determines whether the specified value is close to one (1.0f).
    /// </summary>
    /// <param name="a">The floating value.</param>
    /// <returns><c>true</c> if the specified value is close to one (1.0f); otherwise, <c>false</c>.</returns>
    public static bool IsOne(float a)
    {
        return IsZero(a - 1.0f);
    }

    /// <summary>
    /// Checks if a - b are almost equals within a float epsilon.
    /// </summary>
    /// <param name="a">The left value to compare.</param>
    /// <param name="b">The right value to compare.</param>
    /// <param name="epsilon">Epsilon value</param>
    /// <returns><c>true</c> if a almost equal to b within a float epsilon, <c>false</c> otherwise</returns>
    public static bool WithinEpsilon(float a, float b, float epsilon)
    {
        float num = a - b;
        return ((-epsilon <= num) && (num <= epsilon));
    }

    /// <summary>
    /// Converts a float to its bitwise representation as an integer.
    /// </summary>
    /// <param name="value">The float value to convert.</param>
    /// <returns>The bitwise representation of the float as an integer.</returns>
    private static unsafe int FloatToInt32Bits(float value) => *(int*)&value;
}
