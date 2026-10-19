using NetP3DLib.P3D.Extensions;
using System;
using System.Collections.Generic;
using System.Drawing;
using System.IO;
using System.Numerics;

namespace NetP3DLib.P3D.Helpers;
public static class ListHelper
{
    private static int FindInsertIndex<T>(List<T> list, int indexInParent) where T : Chunk
    {
        int low = 0;
        int high = list.Count;

        while (low < high)
        {
            int mid = low + (high - low) / 2;
            if (list[mid].IndexInParent < indexInParent)
                low = mid + 1;
            else
                high = mid;
        }
        return low;
    }

    public static void InsertSorted<T>(List<T> list, T chunk) where T : Chunk
    {
        int index = FindInsertIndex(list, chunk.IndexInParent);
        list.Insert(index, chunk);
    }

    public static void InsertSorted<TKey, TValue>(Dictionary<TKey, List<TValue>> dict, TKey key, TValue chunk) where TValue : Chunk
    {
        if (!dict.TryGetValue(key, out var list))
        {
            list = [];
            dict[key] = list;
        }
        InsertSorted(list, chunk);
    }

    public static void CleanListInDictionary<TKey, TValue>(Dictionary<TKey, List<TValue>> dict, TKey key) where TValue : Chunk
    {
        if (!dict.TryGetValue(key, out var list))
            return;

        for (var i = list.Count - 1; i >= 0; i--)
            if (list[i].IndexInParent == -1)
                list.RemoveAt(i);

        if (list.Count == 0)
            dict.Remove(key);
    }
}
