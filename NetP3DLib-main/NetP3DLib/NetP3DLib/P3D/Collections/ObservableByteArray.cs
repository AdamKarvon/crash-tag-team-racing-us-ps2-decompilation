using System;
using System.Collections;
using System.Collections.Generic;

namespace NetP3DLib.P3D.Collections;

public class ObservableByteArray : IEnumerable<byte>
{
    private readonly byte[] _data;
    private readonly Action? _onChanged;

    public byte[] RawArray => _data;

    public ObservableByteArray(int size, Action? onChanged = null)
    {
        _data = new byte[size];
        _onChanged = onChanged;
    }

    public ObservableByteArray(byte[] data, Action? onChanged = null)
    {
        _data = data ?? [];
        _onChanged = onChanged;
    }

    public static implicit operator ObservableByteArray(byte[] data) => new(data);

    public int Length => _data.Length;

    public byte this[int index]
    {
        get => _data[index];
        set
        {
            if (_data[index] != value)
            {
                _data[index] = value;
                _onChanged?.Invoke();
            }
        }
    }

    public byte[] ToArray() => (byte[])_data.Clone();

    public IEnumerator<byte> GetEnumerator()
    {
        for (int i = 0; i < _data.Length; i++)
            yield return _data[i];
    }

    IEnumerator IEnumerable.GetEnumerator() => GetEnumerator();
}