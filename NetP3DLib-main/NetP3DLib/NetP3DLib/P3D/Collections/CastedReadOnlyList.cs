using System.Collections;
using System.Collections.Generic;

namespace NetP3DLib.P3D.Collections;
public readonly struct CastedReadOnlyList<TIn, TOut> : IReadOnlyList<TOut>
    where TIn : class
    where TOut : class, TIn
{
    private readonly List<TIn> _source;

    public CastedReadOnlyList(List<TIn> source) => _source = source;

    public TOut this[int index] => (TOut)_source[index];
    public int Count => _source.Count;

    public IEnumerator<TOut> GetEnumerator()
    {
        foreach (var item in _source)
            yield return (TOut)item;
    }

    IEnumerator IEnumerable.GetEnumerator() => GetEnumerator();
}