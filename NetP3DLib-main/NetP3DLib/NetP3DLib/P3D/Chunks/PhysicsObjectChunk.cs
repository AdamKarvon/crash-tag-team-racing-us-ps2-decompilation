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
public class PhysicsObjectChunk : NamedChunk
{
    public const ChunkIdentifier ChunkID = ChunkIdentifier.Physics_Object;

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
    
    private uint _numJoints;
    public uint NumJoints
    {
        get => _numJoints;
        set
        {
            if (_numJoints == value)
                return;
    
            _numJoints = value;
            OnPropertyChanged(nameof(NumJoints));
        }
    }
    
    private float _volume;
    public float Volume
    {
        get => _volume;
        set
        {
            if (_volume == value)
                return;
    
            _volume = value;
            OnPropertyChanged(nameof(Volume));
        }
    }
    
    private float _restingSensitivity;
    public float RestingSensitivity
    {
        get => _restingSensitivity;
        set
        {
            if (_restingSensitivity == value)
                return;
    
            _restingSensitivity = value;
            OnPropertyChanged(nameof(RestingSensitivity));
        }
    }
    

    public override byte[] DataBytes
    {
        get
        {
            var data = new List<byte>((int)DataLength);

            data.AddRange(BinaryExtensions.GetP3DStringBytes(Name));
            data.AddRange(BitConverter.GetBytes(Version));
            data.AddRange(BinaryExtensions.GetP3DStringBytes(MaterialName));
            data.AddRange(BitConverter.GetBytes(NumJoints));
            data.AddRange(BitConverter.GetBytes(Volume));
            data.AddRange(BitConverter.GetBytes(RestingSensitivity));

            return [.. data];
        }
    }
    public override uint DataLength => BinaryExtensions.GetP3DStringLength(Name) + sizeof(uint) + BinaryExtensions.GetP3DStringLength(MaterialName) + sizeof(uint) + sizeof(float) + sizeof(float);

    public PhysicsObjectChunk(EndianAwareBinaryReader br) : this(br.ReadP3DString(), br.ReadUInt32(), br.ReadP3DString(), br.ReadUInt32(), br.ReadSingle(), br.ReadSingle())
    {
    }

    public PhysicsObjectChunk(string name, uint version, string materialName, uint numJoints, float volume, float restingSensitivity) : base(ChunkID, name)
    {
        _version = version;
        _materialName = new(this, materialName, nameof(MaterialName));
        _numJoints = numJoints;
        _volume = volume;
        _restingSensitivity = restingSensitivity;
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
        bw.Write(NumJoints);
        bw.Write(Volume);
        bw.Write(RestingSensitivity);
    }

    protected override Chunk CloneSelf() => new PhysicsObjectChunk(Name, Version, MaterialName, NumJoints, Volume, RestingSensitivity);
}
