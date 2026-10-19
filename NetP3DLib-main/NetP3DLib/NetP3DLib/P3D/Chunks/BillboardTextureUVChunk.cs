using NetP3DLib.IO;
using NetP3DLib.P3D.Attributes;
using NetP3DLib.P3D.Enums;
using NetP3DLib.P3D.Extensions;
using System;
using System.Collections.Generic;
using System.Numerics;

namespace NetP3DLib.P3D.Chunks;

[ChunkAttributes(ChunkID)]
public class BillboardTextureUVChunk : Chunk
{
    public const ChunkIdentifier ChunkID = ChunkIdentifier.Billboard_Texture_UV;

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
    
    private uint _randomU;
    public uint RandomU
    {
        get => _randomU;
        set
        {
            if (_randomU == value)
                return;
    
            _randomU = value;
            OnPropertyChanged(nameof(RandomU));
        }
    }
    
    private uint _randomV;
    public uint RandomV
    {
        get => _randomV;
        set
        {
            if (_randomV == value)
                return;
    
            _randomV = value;
            OnPropertyChanged(nameof(RandomV));
        }
    }
    
    private Vector2 _uV0;
    public Vector2 UV0
    {
        get => _uV0;
        set
        {
            if (_uV0 == value)
                return;
    
            _uV0 = value;
            OnPropertyChanged(nameof(UV0));
        }
    }
    
    private Vector2 _uV1;
    public Vector2 UV1
    {
        get => _uV1;
        set
        {
            if (_uV1 == value)
                return;
    
            _uV1 = value;
            OnPropertyChanged(nameof(UV1));
        }
    }
    
    private Vector2 _uV2;
    public Vector2 UV2
    {
        get => _uV2;
        set
        {
            if (_uV2 == value)
                return;
    
            _uV2 = value;
            OnPropertyChanged(nameof(UV2));
        }
    }
    
    private Vector2 _uV3;
    public Vector2 UV3
    {
        get => _uV3;
        set
        {
            if (_uV3 == value)
                return;
    
            _uV3 = value;
            OnPropertyChanged(nameof(UV3));
        }
    }
    
    private Vector2 _uVOffset;
    public Vector2 UVOffset
    {
        get => _uVOffset;
        set
        {
            if (_uVOffset == value)
                return;
    
            _uVOffset = value;
            OnPropertyChanged(nameof(UVOffset));
        }
    }
    

    public override byte[] DataBytes
    {
        get
        {
            var data = new List<byte>((int)DataLength);

            data.AddRange(BitConverter.GetBytes(Version));
            data.AddRange(BitConverter.GetBytes(RandomU));

            return [.. data];
        }
    }
    public override uint DataLength => sizeof(uint) + sizeof(uint) + sizeof(uint) + sizeof(float) * 2 + sizeof(float) * 2 + sizeof(float) * 2 + sizeof(float) * 2 + sizeof(float) * 2;

    public BillboardTextureUVChunk(EndianAwareBinaryReader br) : this(br.ReadUInt32(), br.ReadUInt32(), br.ReadUInt32(), br.ReadVector2(), br.ReadVector2(), br.ReadVector2(), br.ReadVector2(), br.ReadVector2())
    {
    }

    public BillboardTextureUVChunk(uint version, uint randomU, uint randomV, Vector2 uv0, Vector2 uv1, Vector2 uv2, Vector2 uv3, Vector2 uvOffset) : base(ChunkID)
    {
        _version = version;
        _randomU = randomU;
        _randomV = randomV;
        _uV0 = uv0;
        _uV1 = uv1;
        _uV2 = uv2;
        _uV3 = uv3;
        _uVOffset = uvOffset;
    }

    protected override void WriteData(EndianAwareBinaryWriter bw)
    {
        bw.Write(Version);
        bw.Write(RandomU);
        bw.Write(RandomV);
        bw.Write(UV0);
        bw.Write(UV1);
        bw.Write(UV2);
        bw.Write(UV3);
        bw.Write(UVOffset);
    }

    protected override Chunk CloneSelf() => new BillboardTextureUVChunk(Version, RandomU, RandomV, UV0, UV1, UV2, UV3, UVOffset);
}
