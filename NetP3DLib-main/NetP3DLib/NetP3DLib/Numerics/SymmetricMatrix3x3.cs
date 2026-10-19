using System.Numerics;

namespace NetP3DLib.Numerics;

/// <summary>
/// Credits: <see href="https://lucasstuff.com" langword=" (Lucas Cardellini)"/>.
/// </summary>
public struct SymmetricMatrix3x3
{
    public const int SizeInBytes = sizeof(float) * 6;
    public static readonly SymmetricMatrix3x3 Zero = new();
    public static readonly SymmetricMatrix3x3 Identity = new() { XX = 1.0f, YY = 1.0f, ZZ = 1.0f };

    public float XX;
    public float XY;
    public float XZ;

    public float YY;
    public float YZ;

    public float ZZ;

    public SymmetricMatrix3x3(float Value)
    {
        this.XX = this.XY = this.XZ =
        this.YY = this.YZ =
        this.ZZ = Value;
    }

    public SymmetricMatrix3x3(float XX, float XY, float XZ, float YY, float YZ, float ZZ)
    {
        this.XX = XX; this.XY = XY; this.XZ = XZ;
        this.YY = YY; this.YZ = YZ;
        this.ZZ = ZZ;
    }

    public SymmetricMatrix3x3(float[] Values)
    {
        this.XX = Values[0];
        this.XY = Values[1];
        this.XZ = Values[2];

        this.YY = Values[3];
        this.YZ = Values[4];

        this.ZZ = Values[5];
    }

    public bool IsIdentity => Equals(Identity);

    /// <summary>
    /// Credits: <see href="https://github.com/EnAppelsin" langword=" (EnAppelsin)"/>.
    /// </summary>
    public static SymmetricMatrix3x3 Outer(Vector3 r)
    {
        SymmetricMatrix3x3 i = Zero;
        i.XX = r.X * r.X;
        i.XY = r.X * r.Y;
        i.XZ = r.X * r.Z;
        i.YY = r.Y * r.Y;
        i.YZ = r.Y * r.Z;
        i.ZZ = r.Z * r.Z;
        return i;
    }

    /// <summary>
    /// Credits: <see href="https://github.com/EnAppelsin" langword=" (EnAppelsin)"/>.
    /// </summary>
    public static SymmetricMatrix3x3 Translate(SymmetricMatrix3x3 inertia, Vector3 centre)
    {
        SymmetricMatrix3x3 e3 = Identity;
        return inertia + (e3 * centre.LengthSquared() - Outer(centre));
    }

    public static void Add(ref SymmetricMatrix3x3 Left, ref SymmetricMatrix3x3 Right, out SymmetricMatrix3x3 Result)
    {
        Result.XX = Left.XX + Right.XX;
        Result.XY = Left.XY + Right.XY;
        Result.XZ = Left.XZ + Right.XZ;
        Result.YY = Left.YY + Right.YY;
        Result.YZ = Left.YZ + Right.YZ;
        Result.ZZ = Left.ZZ + Right.ZZ;
    }

    public static SymmetricMatrix3x3 Add(SymmetricMatrix3x3 Left, SymmetricMatrix3x3 Right)
    {
        Add(ref Left, ref Right, out var Result);
        return Result;
    }

    public static void Subtract(ref SymmetricMatrix3x3 Left, ref SymmetricMatrix3x3 Right, out SymmetricMatrix3x3 Result)
    {
        Result.XX = Left.XX - Right.XX;
        Result.XY = Left.XY - Right.XY;
        Result.XZ = Left.XZ - Right.XZ;
        Result.YY = Left.YY - Right.YY;
        Result.YZ = Left.YZ - Right.YZ;
        Result.ZZ = Left.ZZ - Right.ZZ;
    }

    public static SymmetricMatrix3x3 Subtract(SymmetricMatrix3x3 Left, SymmetricMatrix3x3 Right)
    {
        Subtract(ref Left, ref Right, out var Result);
        return Result;
    }

    public static void Multiply(ref SymmetricMatrix3x3 Left, float Right, out SymmetricMatrix3x3 Result)
    {
        Result.XX = Left.XX * Right;
        Result.XY = Left.XY * Right;
        Result.XZ = Left.XZ * Right;
        Result.YY = Left.YY * Right;
        Result.YZ = Left.YZ * Right;
        Result.ZZ = Left.ZZ * Right;
    }

    public static SymmetricMatrix3x3 Multiply(SymmetricMatrix3x3 Left, float Right)
    {
        Multiply(ref Left, Right, out var Result);
        return Result;
    }

    public static void Multiply(ref SymmetricMatrix3x3 Left, ref SymmetricMatrix3x3 Right, out SymmetricMatrix3x3 Result)
    {
        var Temp = new SymmetricMatrix3x3
        {
            XX = (Left.XX * Right.XX) + (Left.XY * Right.XY) + (Left.XZ * Right.XZ),
            XY = (Left.XX * Right.XY) + (Left.XY * Right.YY) + (Left.XZ * Right.YZ),
            XZ = (Left.XX * Right.XZ) + (Left.XY * Right.YZ) + (Left.XZ * Right.ZZ),
            YY = (Left.XY * Right.XY) + (Left.YY * Right.YY) + (Left.YZ * Right.YZ),
            YZ = (Left.XY * Right.XZ) + (Left.YY * Right.YZ) + (Left.YZ * Right.ZZ),
            ZZ = (Left.XZ * Right.XZ) + (Left.YZ * Right.YZ) + (Left.ZZ * Right.ZZ)
        };
        Result = Temp;
    }

    public static SymmetricMatrix3x3 Multiply(SymmetricMatrix3x3 Left, SymmetricMatrix3x3 Right)
    {
        Multiply(ref Left, ref Right, out var Result);
        return Result;
    }

    /*public static void Multiply(ref SymmetricMatrix3x3 Left, ref SharpDX.Matrix3x3 Right, out SymmetricMatrix3x3 Result)
    {
        var Temp = new SymmetricMatrix3x3();
        {
            var Temp2 = (Left.XX * Right.M11) + (Left.XY * Right.M21) + (Left.XZ * Right.M31);
            var Temp3 = (Left.XY * Right.M11) + (Left.YY * Right.M21) + (Left.YZ * Right.M31);
            var Temp4 = (Left.XZ * Right.M11) + (Left.YZ * Right.M21) + (Left.ZZ * Right.M31);
            Temp.XX = (Temp2 * Right.M11) + (Temp3 * Right.M21) + (Temp4 * Right.M31);
            Temp.XY = (Temp2 * Right.M12) + (Temp3 * Right.M22) + (Temp4 * Right.M32);
            Temp.XZ = (Temp2 * Right.M13) + (Temp3 * Right.M23) + (Temp4 * Right.M33);
        }
        {
            var Temp2 = (Left.XX * Right.M12) + (Left.XY * Right.M22) + (Left.XZ * Right.M32);
            var Temp3 = (Left.XY * Right.M12) + (Left.YY * Right.M22) + (Left.YZ * Right.M32);
            var Temp4 = (Left.XZ * Right.M12) + (Left.YZ * Right.M22) + (Left.ZZ * Right.M32);
            Temp.YY = (Temp2 * Right.M12) + (Temp3 * Right.M22) + (Temp4 * Right.M32);
            Temp.YZ = (Temp2 * Right.M13) + (Temp3 * Right.M23) + (Temp4 * Right.M33);
        }
        {
            var Temp2 = (Left.XX * Right.M13) + (Left.XY * Right.M23) + (Left.XZ * Right.M33);
            var Temp3 = (Left.XY * Right.M13) + (Left.YY * Right.M23) + (Left.YZ * Right.M33);
            var Temp4 = (Left.XZ * Right.M13) + (Left.YZ * Right.M23) + (Left.ZZ * Right.M33);
            Temp.ZZ = (Temp2 * Right.M13) + (Temp3 * Right.M23) + (Temp4 * Right.M33);
        }
        Result = Temp;
    }*/

    public static void Divide(ref SymmetricMatrix3x3 Left, float Right, out SymmetricMatrix3x3 Result)
    {
        Result.XX = Left.XX / Right;
        Result.XY = Left.XY / Right;
        Result.XZ = Left.XZ / Right;
        Result.YY = Left.YY / Right;
        Result.YZ = Left.YZ / Right;
        Result.ZZ = Left.ZZ / Right;
    }

    public static SymmetricMatrix3x3 Divide(SymmetricMatrix3x3 Left, float Right)
    {
        Divide(ref Left, Right, out var Result);
        return Result;
    }

    public static SymmetricMatrix3x3 operator +(SymmetricMatrix3x3 Left, SymmetricMatrix3x3 Right)
    {
        Add(ref Left, ref Right, out var Result);
        return Result;
    }

    public static SymmetricMatrix3x3 operator -(SymmetricMatrix3x3 Left, SymmetricMatrix3x3 Right)
    {
        Subtract(ref Left, ref Right, out var Result);
        return Result;
    }

    public static SymmetricMatrix3x3 operator *(float Left, SymmetricMatrix3x3 Right)
    {
        Multiply(ref Right, Left, out var Result);
        return Result;
    }

    public static SymmetricMatrix3x3 operator *(SymmetricMatrix3x3 Left, float Right)
    {
        Multiply(ref Left, Right, out var Result);
        return Result;
    }

    public static SymmetricMatrix3x3 operator *(SymmetricMatrix3x3 Left, SymmetricMatrix3x3 Right)
    {
        Multiply(ref Left, ref Right, out var Result);
        return Result;
    }

    /*public static SymmetricMatrix3x3 operator *(SharpDX.Matrix3x3 Left, SymmetricMatrix3x3 Right)
    {
        Multiply(ref Right, ref Left, out var Result);
        return Result;
    }

    public static SymmetricMatrix3x3 operator *(SymmetricMatrix3x3 Left, SharpDX.Matrix3x3 Right)
    {
        Multiply(ref Left, ref Right, out var Result);
        return Result;
    }*/

    public static SymmetricMatrix3x3 operator /(SymmetricMatrix3x3 Left, float Right)
    {
        Divide(ref Left, Right, out var Result);
        return Result;
    }

    public static bool operator ==(SymmetricMatrix3x3 Left, SymmetricMatrix3x3 Right)
    {
        return Left.Equals(ref Right);
    }

    public static bool operator !=(SymmetricMatrix3x3 Left, SymmetricMatrix3x3 Right)
    {
        return !Left.Equals(ref Right);
    }

    /*public static implicit operator SharpDX.Matrix3x3(SymmetricMatrix3x3 Value)
    {
        return new SharpDX.Matrix3x3(
            Value.XX, Value.XY, Value.XZ,
            Value.XY, Value.YY, Value.YZ,
            Value.XZ, Value.YZ, Value.ZZ
            );
    }*/

    /*public static explicit operator SymmetricMatrix3x3(SharpDX.Matrix3x3 Value)
    {
        System.Diagnostics.Trace.Assert(SharpDX.MathUtil.NearEqual(Value.M21, Value.M12));
        System.Diagnostics.Trace.Assert(SharpDX.MathUtil.NearEqual(Value.M31, Value.M13));
        System.Diagnostics.Trace.Assert(SharpDX.MathUtil.NearEqual(Value.M32, Value.M23));
        return new SymmetricMatrix3x3(
            Value.M11, Value.M12, Value.M13,
            Value.M22, Value.M23,
            Value.M33
            );
    }*/

    public override readonly string ToString() => string.Format(System.Globalization.CultureInfo.CurrentCulture, "[XX:{0} XY:{1} XZ:{2}] [YY:{3} YZ:{4}] [ZZ:{5}]", this.XX, this.XY, this.XZ, this.YY, this.YZ, this.ZZ);

    public string ToString(string Format)
    {
        if (Format == null)
            return ToString();

        return string.Format(Format, System.Globalization.CultureInfo.CurrentCulture, "[XX:{0} XY:{1} XZ:{2}] [YY:{3} YZ:{4}] [ZZ:{5}]",
            this.XX.ToString(Format, System.Globalization.CultureInfo.CurrentCulture), this.XY.ToString(Format, System.Globalization.CultureInfo.CurrentCulture), this.XZ.ToString(Format, System.Globalization.CultureInfo.CurrentCulture),
            this.YY.ToString(Format, System.Globalization.CultureInfo.CurrentCulture), this.XZ.ToString(Format, System.Globalization.CultureInfo.CurrentCulture),
            this.ZZ.ToString(Format, System.Globalization.CultureInfo.CurrentCulture));
    }

    public string ToString(System.IFormatProvider FormatProvider)
    {
        return string.Format(FormatProvider, "[XX:{0} XY:{1} XZ:{2}] [YY:{3} YZ:{4}] [ZZ:{5}]",
            this.XX.ToString(FormatProvider), this.XY.ToString(FormatProvider), this.XZ.ToString(FormatProvider),
            this.YY.ToString(FormatProvider), this.YZ.ToString(FormatProvider),
            this.ZZ.ToString(FormatProvider));
    }

    public string ToString(string Format, System.IFormatProvider FormatProvider)
    {
        if (Format == null)
            return ToString(FormatProvider);

        return string.Format(Format, FormatProvider, "[XX:{0} XY:{1} XZ:{2}] [YY:{3} YZ:{4}] [ZZ:{5}]",
            this.XX.ToString(Format, FormatProvider), this.XY.ToString(Format, FormatProvider), this.XZ.ToString(Format, FormatProvider),
            this.YY.ToString(Format, FormatProvider), this.YZ.ToString(Format, FormatProvider),
            this.ZZ.ToString(Format, FormatProvider));
    }

    public override int GetHashCode()
    {
        unchecked
        {
            var HashCode = this.XX.GetHashCode();
            HashCode = (HashCode * 397) ^ this.XY.GetHashCode();
            HashCode = (HashCode * 397) ^ this.XZ.GetHashCode();
            HashCode = (HashCode * 397) ^ this.YY.GetHashCode();
            HashCode = (HashCode * 397) ^ this.YZ.GetHashCode();
            HashCode = (HashCode * 397) ^ this.ZZ.GetHashCode();
            return HashCode;
        }
    }

    public bool Equals(ref SymmetricMatrix3x3 Other)
    {
        return (MathUtil.NearEqual(Other.XX, this.XX) &&
            MathUtil.NearEqual(Other.XY, this.XY) &&
            MathUtil.NearEqual(Other.XZ, this.XZ) &&
            MathUtil.NearEqual(Other.YY, this.YY) &&
            MathUtil.NearEqual(Other.YZ, this.YZ) &&
            MathUtil.NearEqual(Other.ZZ, this.ZZ));
    }

    public bool Equals(SymmetricMatrix3x3 Other)
    {
        return Equals(ref Other);
    }

    public static bool Equals(ref SymmetricMatrix3x3 A, ref SymmetricMatrix3x3 B)
    {
        return
            MathUtil.NearEqual(A.XX, B.XX) &&
            MathUtil.NearEqual(A.XY, B.XY) &&
            MathUtil.NearEqual(A.XZ, B.XZ) &&

            MathUtil.NearEqual(A.YY, B.YY) &&
            MathUtil.NearEqual(A.YZ, B.YZ) &&

            MathUtil.NearEqual(A.ZZ, B.ZZ)
            ;
    }

    public override bool Equals(object Value)
    {
        if (Value is not SymmetricMatrix3x3)
            return false;

        var StrongValue = (SymmetricMatrix3x3)Value;
        return Equals(ref StrongValue);
    }
}