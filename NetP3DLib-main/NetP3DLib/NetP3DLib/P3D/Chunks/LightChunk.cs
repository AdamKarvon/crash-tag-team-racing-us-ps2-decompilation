using NetP3DLib.IO;
using NetP3DLib.P3D.Attributes;
using NetP3DLib.P3D.Enums;
using NetP3DLib.P3D.Extensions;
using System;
using System.Collections.Generic;
using System.ComponentModel;
using System.Drawing;

namespace NetP3DLib.P3D.Chunks;

[ChunkAttributes(ChunkID)]
public class LightChunk : NamedChunk
{
    public const ChunkIdentifier ChunkID = ChunkIdentifier.Light;

    public enum Types : uint
    {
        Ambient,
        Point,
        Directional,
        Spotlight,
    }

    private uint _version;
    [DefaultValue(257)]
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
    
    private Types _type;
    public Types Type
    {
        get => _type;
        set
        {
            if (_type == value)
                return;
    
            _type = value;
            OnPropertyChanged(nameof(Type));
        }
    }
    
    private Color _colour;
    public Color Colour
    {
        get => _colour;
        set
        {
            if (_colour == value)
                return;
    
            _colour = value;
            OnPropertyChanged(nameof(Colour));
        }
    }
    
    private float _constant;
    public float Constant
    {
        get => _constant;
        set
        {
            if (_constant == value)
                return;
    
            _constant = value;
            OnPropertyChanged(nameof(Constant));
        }
    }
    
    private float _linear;
    public float Linear
    {
        get => _linear;
        set
        {
            if (_linear == value)
                return;
    
            _linear = value;
            OnPropertyChanged(nameof(Linear));
        }
    }
    
    private float _squared;
    public float Squared
    {
        get => _squared;
        set
        {
            if (_squared == value)
                return;
    
            _squared = value;
            OnPropertyChanged(nameof(Squared));
        }
    }
    
    private uint _enabled;
    public bool Enabled
    {
        get => _enabled != 0;
        set
        {
            if (Enabled == value)
                return;

            _enabled = value ? 1u : 0u;
            OnPropertyChanged(nameof(Enabled));
        }
    }

    public override byte[] DataBytes
    {
        get
        {
            var data = new List<byte>((int)DataLength);

            data.AddRange(BinaryExtensions.GetP3DStringBytes(Name));
            data.AddRange(BitConverter.GetBytes(Version));
            data.AddRange(BitConverter.GetBytes((uint)Type));
            data.AddRange(BinaryExtensions.GetBytes(Colour));
            data.AddRange(BitConverter.GetBytes(Constant));
            data.AddRange(BitConverter.GetBytes(Linear));
            data.AddRange(BitConverter.GetBytes(Squared));
            data.AddRange(BitConverter.GetBytes(_enabled));

            return [.. data];
        }
    }
    public override uint DataLength => BinaryExtensions.GetP3DStringLength(Name) + sizeof(uint) + sizeof(uint) + sizeof(uint) + sizeof(float) + sizeof(float) + sizeof(float) + sizeof(uint);

    public LightChunk(EndianAwareBinaryReader br) : this(br.ReadP3DString(), br.ReadUInt32(), (Types)br.ReadUInt32(), br.ReadColor(), br.ReadSingle(), br.ReadSingle(), br.ReadSingle(), br.ReadUInt32())
    {
    }

    public LightChunk(string name, uint version, Types type, Color colour, float constant, float linear, float squared, bool enabled) : this(name, version, type, colour, constant, linear, squared, enabled ? 1u : 0u)
    {
    }

    public LightChunk(string name, uint version, Types type, Color colour, float constant, float linear, float squared, uint enabled) : base(ChunkID, name)
    {
        _version = version;
        _type = type;
        _colour = colour;
        _constant = constant;
        _linear = linear;
        _squared = squared;
        _enabled = enabled;
    }

    protected override void WriteData(EndianAwareBinaryWriter bw)
    {
        bw.WriteP3DString(Name);
        bw.Write(Version);
        bw.Write((uint)Type);
        bw.Write(Colour);
        bw.Write(Constant);
        bw.Write(Linear);
        bw.Write(Squared);
        bw.Write(_enabled);
    }

    protected override Chunk CloneSelf() => new LightChunk(Name, Version, Type, Colour, Constant, Linear, Squared, Enabled);
}
