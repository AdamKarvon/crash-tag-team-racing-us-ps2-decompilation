namespace NetP3DLib.P3D.Types;

internal class P3DLongString
{
    private readonly Chunk _chunk;
    private readonly string _propertyName;

    private string _value;
    public string Value
    {
        get => _value;
        set
        {
            if (_value == value)
                return;

            var oldSize = _chunk.HeaderSize;
            _value = value;
            _chunk.RecalculateSize(oldSize);
            _chunk.OnPropertyChanged(_propertyName);
        }
    }


    internal P3DLongString(Chunk chunk, string value, string propertyName)
    {
        _chunk = chunk;
        _value = value;
        _propertyName = propertyName;
    }
}
