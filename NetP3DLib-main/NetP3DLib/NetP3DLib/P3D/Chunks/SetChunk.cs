using NetP3DLib.IO;
using NetP3DLib.P3D.Attributes;
using NetP3DLib.P3D.Enums;
using NetP3DLib.P3D.Exceptions;
using NetP3DLib.P3D.Extensions;
using System;
using System.Collections.Generic;
using System.ComponentModel;

namespace NetP3DLib.P3D.Chunks;

[ChunkAttributes(ChunkID)]
public class SetChunk : NamedChunk
{
    public const ChunkIdentifier ChunkID = ChunkIdentifier.Set;

    private uint _version;
    [DefaultValue(0)]
    public uint Version
    {
        get => _version;
        set
        {
            if (_version == value)
                return;
    
            _version = value;
            OnPropertyChanged(nameof(Version));
        }
    }
    
    public byte NumChildren => (byte)GetChildCount();

    public override byte[] DataBytes
    {
        get
        {
            var data = new List<byte>((int)DataLength);

            data.AddRange(BinaryExtensions.GetP3DStringBytes(Name));
            data.AddRange(BitConverter.GetBytes(Version));
            data.Add(NumChildren);

            return [.. data];
        }
    }
    public override uint DataLength => BinaryExtensions.GetP3DStringLength(Name) + sizeof(uint) + sizeof(byte);

    [System.Diagnostics.CodeAnalysis.SuppressMessage("Style", "IDE0059:Unnecessary assignment of a value", Justification = "We want to read the value to progress the BinaryReader, but not set the value anywhere because it's calculated dynamically.")]
    public SetChunk(EndianAwareBinaryReader br) : this(br.ReadP3DString(), br.ReadUInt32())
    {
        var numChildren = br.ReadByte();
    }

    public SetChunk(string name, uint version) : base(ChunkID, name)
    {
        _version = version;
    }

    public uint? GetSetID() => NumChildren == 0 ? null : Children[0].ID;

    public Type? GetSetType() => NumChildren == 0 ? null : Children[0].GetType();

    public override IEnumerable<InvalidP3DException> ValidateChunk()
    {
        foreach (var error in base.ValidateChunk())
            yield return error;

        var childCount = GetChildCount();
        if (childCount == 0)
            yield return new InvalidP3DException(this, "You must have at least one child chunk.");

        if (childCount > byte.MaxValue)
            yield return new InvalidP3DException(this, $"The max number of child chunks is {byte.MinValue}.");

        if (!typeof(NamedChunk).IsAssignableFrom(Children[0].GetType()))
            yield return new InvalidP3DException(this, $"The set type must be a {nameof(NamedChunk)}.");

        if (childCount > 1 && childCount != GetChildCount(Children[0].ID))
            yield return new InvalidP3DException(this, "All children must be the same type.");
    }

    protected override void WriteData(EndianAwareBinaryWriter bw)
    {
        bw.WriteP3DString(Name);
        bw.Write(Version);
        bw.Write(NumChildren);
    }

    protected override Chunk CloneSelf() => new SetChunk(Name, Version);

    public override string ToString() => $"\"{Name}\" ({(NumChildren > 0 ? $"{GetChunkType(Children[0])} (0x{Children[0].ID:X})" : "Null")}) ({GetChunkType(this)} (0x{ID:X}))";
}
