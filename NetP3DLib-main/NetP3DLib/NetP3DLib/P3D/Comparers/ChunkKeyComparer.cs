using System;
using System.Collections.Generic;

namespace NetP3DLib.P3D.Comparers;
internal sealed class ChunkKeyComparer : IEqualityComparer<(Type, string)>
{
    public static readonly ChunkKeyComparer Instance = new();

    public bool Equals((Type, string) x, (Type, string) y) => x.Item1 == y.Item1 && string.Equals(x.Item2, y.Item2, StringComparison.Ordinal);

    public int GetHashCode((Type, string) obj)
    {
        unchecked
        {
            int hash = 17;
            hash = hash * 31 + (obj.Item1 != null ? obj.Item1.GetHashCode() : 0);
            hash = hash * 31 + (obj.Item2 != null ? obj.Item2.GetHashCode() : 0);
            return hash;
        }
    }
}