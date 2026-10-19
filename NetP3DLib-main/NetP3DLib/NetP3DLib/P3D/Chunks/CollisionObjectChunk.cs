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
public class CollisionObjectChunk : NamedChunk
{
    public const ChunkIdentifier ChunkID = ChunkIdentifier.Collision_Object;

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
    
    private readonly P3DString _materialName;
    public string MaterialName
    {
        get => _materialName?.Value ?? string.Empty;
        set => _materialName.Value = value;
    }
    
    private uint _numSubObjects;
    public uint NumSubObjects
    {
        get => _numSubObjects;
        set
        {
            if (_numSubObjects == value)
                return;
    
            _numSubObjects = value;
            OnPropertyChanged(nameof(NumSubObjects));
        }
    }
    
    public uint NumOwners => GetChildCount(ChunkIdentifier.Collision_Volume_Owner);

    public override byte[] DataBytes
    {
        get
        {
            var data = new List<byte>((int)DataLength);

            data.AddRange(BinaryExtensions.GetP3DStringBytes(Name));
            data.AddRange(BitConverter.GetBytes(Version));
            data.AddRange(BinaryExtensions.GetP3DStringBytes(MaterialName));
            data.AddRange(BitConverter.GetBytes(NumSubObjects));
            data.AddRange(BitConverter.GetBytes(NumOwners));

            return [.. data];
        }
    }
    public override uint DataLength => BinaryExtensions.GetP3DStringLength(Name) + sizeof(uint) + BinaryExtensions.GetP3DStringLength(MaterialName) + sizeof(uint) + sizeof(uint);

    [System.Diagnostics.CodeAnalysis.SuppressMessage("Style", "IDE0059:Unnecessary assignment of a value", Justification = "We want to read the value to progress the BinaryReader, but not set the value anywhere because it's calculated dynamically.")]
    public CollisionObjectChunk(EndianAwareBinaryReader br) : this(br.ReadP3DString(), br.ReadUInt32(), br.ReadP3DString(), br.ReadUInt32())
    {
        var numOwners = br.ReadUInt32();
    }

    public CollisionObjectChunk(string name, uint version, string materialName, uint numSubObjects) : base(ChunkID, name)
    {
        _version = version;
        _materialName = new(this, materialName, nameof(MaterialName));
        _numSubObjects = numSubObjects;
    }

    public override IEnumerable<InvalidP3DException> ValidateChunk()
    {
        foreach (var error in base.ValidateChunk())
            yield return error;

        if (!MaterialName.IsValidP3DString())
            yield return new InvalidP3DStringException(this, nameof(MaterialName), MaterialName);
    }

    protected override void WriteData(EndianAwareBinaryWriter bw)
    {
        bw.WriteP3DString(Name);
        bw.Write(Version);
        bw.WriteP3DString(MaterialName);
        bw.Write(NumSubObjects);
        bw.Write(NumOwners);
    }

    protected override Chunk CloneSelf() => new CollisionObjectChunk(Name, Version, MaterialName, NumSubObjects);
}
