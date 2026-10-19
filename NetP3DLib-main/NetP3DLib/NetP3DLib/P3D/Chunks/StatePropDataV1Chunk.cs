using NetP3DLib.IO;
using NetP3DLib.P3D.Attributes;
using NetP3DLib.P3D.Enums;
using NetP3DLib.P3D.Exceptions;
using NetP3DLib.P3D.Extensions;
using NetP3DLib.P3D.Types;
using System;
using System.Collections.Generic;
using System.ComponentModel;

namespace NetP3DLib.P3D.Chunks;

[ChunkAttributes(ChunkID)]
public class StatePropDataV1Chunk : NamedChunk
{
    public const ChunkIdentifier ChunkID = ChunkIdentifier.State_Prop_Data_V1;

    private uint _version;
    [DefaultValue(1)]
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
    
    private readonly P3DString _objectFactoryName;
    public string ObjectFactoryName
    {
        get => _objectFactoryName?.Value ?? string.Empty;
        set => _objectFactoryName.Value = value;
    }
    public uint NumStates => GetChildCount(ChunkIdentifier.State_Prop_State_Data_V1);

    public override byte[] DataBytes
    {
        get
        {
            var data = new List<byte>((int)DataLength);

            data.AddRange(BitConverter.GetBytes(Version));
            data.AddRange(BinaryExtensions.GetP3DStringBytes(Name));
            data.AddRange(BinaryExtensions.GetP3DStringBytes(ObjectFactoryName));
            data.AddRange(BitConverter.GetBytes(NumStates));

            return [.. data];
        }
    }
    public override uint DataLength => sizeof(uint) + BinaryExtensions.GetP3DStringLength(Name) + BinaryExtensions.GetP3DStringLength(ObjectFactoryName) + sizeof(uint);

    [System.Diagnostics.CodeAnalysis.SuppressMessage("Style", "IDE0059:Unnecessary assignment of a value", Justification = "We want to read the value to progress the BinaryReader, but not set the value anywhere because it's calculated dynamically.")]
    public StatePropDataV1Chunk(EndianAwareBinaryReader br) : this(br.ReadUInt32(), br.ReadP3DString(), br.ReadP3DString())
    {
        var numStates = br.ReadUInt32();
    }

    public StatePropDataV1Chunk(uint version, string name, string objectFactoryName) : base(ChunkID, name)
    {
        _version = version;
        _objectFactoryName = new(this, objectFactoryName, nameof(ObjectFactoryName));
    }

    public override IEnumerable<InvalidP3DException> ValidateChunk()
    {
        foreach (var error in base.ValidateChunk())
            yield return error;

        if (!ObjectFactoryName.IsValidP3DString())
            yield return new InvalidP3DStringException(this, nameof(ObjectFactoryName), ObjectFactoryName);

        if (Children.Count == 0)
            yield return new InvalidP3DException(this, $"There must be at least one State Prop State Data V1 child chunk.");
        foreach (var child in Children)
            if (child.ID != (uint)ChunkIdentifier.State_Prop_State_Data_V1)
                yield return new InvalidP3DException(this, $"Child chunk {child} is invalid. Child chunks must be an instance of State Prop State Data V1.");
    }

    protected override void WriteData(EndianAwareBinaryWriter bw)
    {
        bw.Write(Version);
        bw.WriteP3DString(Name);
        bw.WriteP3DString(ObjectFactoryName);
        bw.Write(NumStates);
    }

    protected override Chunk CloneSelf() => new StatePropDataV1Chunk(Version, Name, ObjectFactoryName);
}
