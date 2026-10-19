using NetP3DLib.IO;
using NetP3DLib.P3D.Attributes;
using NetP3DLib.P3D.Enums;
using NetP3DLib.P3D.Exceptions;
using NetP3DLib.P3D.Extensions;
using NetP3DLib.P3D.Types;
using System;
using System.Collections.Generic;

namespace NetP3DLib.P3D.Chunks;

[ChunkAttributes(ChunkID)]
public class TextureAnimationChunk : NamedChunk
{
    public const ChunkIdentifier ChunkID = ChunkIdentifier.Texture_Animation;

    private uint _version;
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
    
    private uint _numFrames;
    public uint NumFrames
    {
        get => _numFrames;
        set
        {
            if (_numFrames == value)
                return;
    
            _numFrames = value;
            OnPropertyChanged(nameof(NumFrames));
        }
    }
    
    private float _frameRate;
    public float FrameRate
    {
        get => _frameRate;
        set
        {
            if (_frameRate == value)
                return;
    
            _frameRate = value;
            OnPropertyChanged(nameof(FrameRate));
        }
    }
    
    private uint _cyclic;
    public bool Cyclic
    {
        get => _cyclic == 1;
        set
        {
            if (Cyclic == value)
                return;

            _cyclic = value ? 1u : 0u;
            OnPropertyChanged(nameof(Cyclic));
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
            data.AddRange(BitConverter.GetBytes(NumFrames));
            data.AddRange(BitConverter.GetBytes(FrameRate));
            data.AddRange(BitConverter.GetBytes(_cyclic));

            return [.. data];
        }
    }
    public override uint DataLength => BinaryExtensions.GetP3DStringLength(Name) + sizeof(uint) + BinaryExtensions.GetP3DStringLength(MaterialName) + sizeof(uint) + sizeof(float) + sizeof(uint);

    public TextureAnimationChunk(EndianAwareBinaryReader br) : this(br.ReadP3DString(), br.ReadUInt32(), br.ReadP3DString(), br.ReadUInt32(), br.ReadSingle(), br.ReadUInt32())
    {
    }

    public TextureAnimationChunk(string name, uint version, string materialName, uint numFrames, float frameRate, bool cyclic) : this(name, version, materialName, numFrames, frameRate, cyclic ? 1u : 0u)
    {
    }
    

    public TextureAnimationChunk(string name, uint version, string materialName, uint numFrames, float frameRate, uint cyclic) : base(ChunkID, name)
    {
        _version = version;
        _materialName = new(this, materialName, nameof(MaterialName));
        _numFrames = numFrames;
        _frameRate = frameRate;
        _cyclic = cyclic;
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
        bw.Write(NumFrames);
        bw.Write(FrameRate);
        bw.Write(_cyclic);
    }

    protected override Chunk CloneSelf() => new TextureAnimationChunk(Name, Version, MaterialName, NumFrames, FrameRate, Cyclic);
}
