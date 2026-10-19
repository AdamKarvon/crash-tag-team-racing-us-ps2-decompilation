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
public class OldSpriteEmitterChunk : NamedChunk
{
    public const ChunkIdentifier ChunkID = ChunkIdentifier.Old_Sprite_Emitter;

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
    
    private readonly P3DString _shaderName;
    public string ShaderName
    {
        get => _shaderName?.Value ?? string.Empty;
        set => _shaderName.Value = value;
    }
    private readonly FourCC _angleMode;
    [MaxLength(4)]
    public string AngleMode
    {
        get => _angleMode?.Value ?? string.Empty;
        set => _angleMode.Value = value;
    }
    
    private float _angle;
    public float Angle
    {
        get => _angle;
        set
        {
            if (_angle == value)
                return;
    
            _angle = value;
            OnPropertyChanged(nameof(Angle));
        }
    }
    
    private readonly FourCC _textureAnimMode;
    [MaxLength(4)]
    public string TextureAnimMode
    {
        get => _textureAnimMode?.Value ?? string.Empty;
        set => _textureAnimMode.Value = value;
    }
    
    private uint _numTextureFrames;
    public uint NumTextureFrames
    {
        get => _numTextureFrames;
        set
        {
            if (_numTextureFrames == value)
                return;
    
            _numTextureFrames = value;
            OnPropertyChanged(nameof(NumTextureFrames));
        }
    }
    
    private uint _textureFrameRate;
    public uint TextureFrameRate
    {
        get => _textureFrameRate;
        set
        {
            if (_textureFrameRate == value)
                return;
    
            _textureFrameRate = value;
            OnPropertyChanged(nameof(TextureFrameRate));
        }
    }
    

    public override byte[] DataBytes
    {
        get
        {
            var data = new List<byte>((int)DataLength);

            data.AddRange(BitConverter.GetBytes(Version));
            data.AddRange(BinaryExtensions.GetP3DStringBytes(Name));
            data.AddRange(BinaryExtensions.GetP3DStringBytes(ShaderName));
            data.AddRange(BinaryExtensions.GetFourCCBytes(AngleMode));
            data.AddRange(BitConverter.GetBytes(Angle));
            data.AddRange(BinaryExtensions.GetFourCCBytes(TextureAnimMode));
            data.AddRange(BitConverter.GetBytes(NumTextureFrames));
            data.AddRange(BitConverter.GetBytes(TextureFrameRate));

            return [.. data];
        }
    }
    public override uint DataLength => sizeof(uint) + BinaryExtensions.GetP3DStringLength(Name) + BinaryExtensions.GetP3DStringLength(ShaderName) + 4 + sizeof(float) + 4 + sizeof(uint) + sizeof(uint);

    public OldSpriteEmitterChunk(EndianAwareBinaryReader br) : this(br.ReadUInt32(), br.ReadP3DString(), br.ReadP3DString(), br.ReadFourCC(), br.ReadSingle(), br.ReadFourCC(), br.ReadUInt32(), br.ReadUInt32())
    {
    }

    public OldSpriteEmitterChunk(uint version, string name, string shaderName, string angleMode, float angle, string textureAnimMode, uint numTextureFrames, uint textureFrameRate) : base(ChunkID, name)
    {
        _version = version;
        _shaderName = new(this, shaderName, nameof(ShaderName));
        _angleMode = new(this, angleMode, nameof(AngleMode));
        _angle = angle;
        _textureAnimMode = new(this, textureAnimMode, nameof(TextureAnimMode));
        _numTextureFrames = numTextureFrames;
        _textureFrameRate = textureFrameRate;
    }

    public override IEnumerable<InvalidP3DException> ValidateChunk()
    {
        foreach (var error in base.ValidateChunk())
            yield return error;

        if (!ShaderName.IsValidP3DString())
            yield return new InvalidP3DStringException(this, nameof(ShaderName), ShaderName);

        if ((ParentChunk != null || ParentFile != null) && FindNamedChunkInParentHierarchy<ShaderChunk>(ShaderName) == null)
            yield return new InvalidP3DException(this, $"Could not find the shader named \"{ShaderName}\" in the parent hierarchy.");

        if (!AngleMode.IsValidFourCC())
            yield return new InvalidP3DFourCCException(this, nameof(AngleMode), AngleMode);

        if (!TextureAnimMode.IsValidFourCC())
            yield return new InvalidP3DFourCCException(this, nameof(TextureAnimMode), TextureAnimMode);
    }

    protected override void WriteData(EndianAwareBinaryWriter bw)
    {
        bw.Write(Version);
        bw.WriteP3DString(Name);
        bw.WriteP3DString(ShaderName);
        bw.WriteFourCC(AngleMode);
        bw.Write(Angle);
        bw.WriteFourCC(TextureAnimMode);
        bw.Write(NumTextureFrames);
        bw.Write(TextureFrameRate);
    }

    protected override Chunk CloneSelf() => new OldSpriteEmitterChunk(Version, Name, ShaderName, AngleMode, Angle, TextureAnimMode, NumTextureFrames, TextureFrameRate);
}
