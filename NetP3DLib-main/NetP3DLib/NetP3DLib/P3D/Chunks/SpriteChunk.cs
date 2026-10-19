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
public class SpriteChunk : NamedChunk
{
    public const ChunkIdentifier ChunkID = ChunkIdentifier.Sprite;

    private uint _nativeX;
    public uint NativeX
    {
        get => _nativeX;
        set
        {
            if (_nativeX == value)
                return;
    
            _nativeX = value;
            OnPropertyChanged(nameof(NativeX));
        }
    }
    
    private uint _nativeY;
    public uint NativeY
    {
        get => _nativeY;
        set
        {
            if (_nativeY == value)
                return;
    
            _nativeY = value;
            OnPropertyChanged(nameof(NativeY));
        }
    }
    
    private readonly P3DString _shader;
    public string Shader
    {
        get => _shader?.Value ?? string.Empty;
        set => _shader.Value = value;
    }
    
    private uint _imageWidth;
    public uint ImageWidth
    {
        get => _imageWidth;
        set
        {
            if (_imageWidth == value)
                return;
    
            _imageWidth = value;
            OnPropertyChanged(nameof(ImageWidth));
        }
    }
    
    private uint _imageHeight;
    public uint ImageHeight
    {
        get => _imageHeight;
        set
        {
            if (_imageHeight == value)
                return;
    
            _imageHeight = value;
            OnPropertyChanged(nameof(ImageHeight));
        }
    }
    
    public uint ImageCount => GetChildCount(ChunkIdentifier.Image);
    private uint _blitBorder;
    public uint BlitBorder
    {
        get => _blitBorder;
        set
        {
            if (_blitBorder == value)
                return;
    
            _blitBorder = value;
            OnPropertyChanged(nameof(BlitBorder));
        }
    }
    

    public override byte[] DataBytes
    {
        get
        {
            var data = new List<byte>((int)DataLength);

            data.AddRange(BinaryExtensions.GetP3DStringBytes(Name));
            data.AddRange(BitConverter.GetBytes(NativeX));
            data.AddRange(BitConverter.GetBytes(NativeY));
            data.AddRange(BinaryExtensions.GetP3DStringBytes(Shader));
            data.AddRange(BitConverter.GetBytes(ImageWidth));
            data.AddRange(BitConverter.GetBytes(ImageHeight));
            data.AddRange(BitConverter.GetBytes(ImageCount));
            data.AddRange(BitConverter.GetBytes(BlitBorder));

            return [.. data];
        }
    }
    public override uint DataLength => BinaryExtensions.GetP3DStringLength(Name) + sizeof(uint) + sizeof(uint) + BinaryExtensions.GetP3DStringLength(Shader) + sizeof(uint) + sizeof(uint) + sizeof(uint) + sizeof(uint);

    public SpriteChunk(EndianAwareBinaryReader br) : this(br.ReadP3DString(), br.ReadUInt32(), br.ReadUInt32(), br.ReadP3DString(), br.ReadUInt32(), br.ReadUInt32(), br.SkipAndRead(sizeof(uint), br.ReadUInt32))
    {
    }

    public SpriteChunk(string name, uint nativeX, uint nativeY, string shader, uint imageWidth, uint imageHeight, uint blitBorder) : base(ChunkID, name)
    {
        _nativeX = nativeX;
        _nativeY = nativeY;
        _shader = new(this, shader, nameof(Shader));
        _imageWidth = imageWidth;
        _imageHeight = imageHeight;
        _blitBorder = blitBorder;
    }

    public override IEnumerable<InvalidP3DException> ValidateChunk()
    {
        foreach (var error in base.ValidateChunk())
            yield return error;

        if (!Shader.IsValidP3DString())
            yield return new InvalidP3DStringException(this, nameof(Shader), Shader);
    }

    protected override void WriteData(EndianAwareBinaryWriter bw)
    {
        bw.WriteP3DString(Name);
        bw.Write(NativeX);
        bw.Write(NativeY);
        bw.WriteP3DString(Shader);
        bw.Write(ImageWidth);
        bw.Write(ImageHeight);
        bw.Write(ImageCount);
        bw.Write(BlitBorder);
    }

    protected override Chunk CloneSelf() => new SpriteChunk(Name, NativeX, NativeY, Shader, ImageWidth, ImageHeight, BlitBorder);
}
