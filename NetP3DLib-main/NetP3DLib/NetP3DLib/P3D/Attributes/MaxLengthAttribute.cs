using System;

namespace NetP3DLib.P3D.Attributes;
[AttributeUsage(AttributeTargets.Property, AllowMultiple = false)]
public class MaxLengthAttribute : Attribute
{
    public int MaxLength { get; }

    public MaxLengthAttribute(int maxLength)
    {
        MaxLength = maxLength;
    }
}
