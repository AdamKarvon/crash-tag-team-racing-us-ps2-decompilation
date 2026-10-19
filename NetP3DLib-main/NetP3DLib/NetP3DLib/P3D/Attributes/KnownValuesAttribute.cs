using System;

namespace NetP3DLib.P3D.Attributes;
[AttributeUsage(AttributeTargets.Property, AllowMultiple = false)]
public class KnownValuesAttribute : Attribute
{
    public string[] Values { get; }

    public bool AllowArbitrary { get; }

    public KnownValuesAttribute(bool allowArbitrary = true, params string[] values)
    {
        Values = values ?? [];
        AllowArbitrary = allowArbitrary;
    }

    public bool IsValid(string value)
    {
        if (AllowArbitrary)
            return true;
        return Array.Exists(Values, v => v == value);
    }
}