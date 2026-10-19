using NetP3DLib.IO;
using NetP3DLib.P3D.Attributes;
using NetP3DLib.P3D.Enums;
using System;
using System.Collections.Generic;

namespace NetP3DLib.P3D.Chunks;

[ChunkAttributes(ChunkID)]
public class VertexCompressionHintChunk : Chunk
{
    public const ChunkIdentifier ChunkID = ChunkIdentifier.Vertex_Compression_Hint;

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
    
    private uint _uV0Size;
    public uint UV0Size
    {
        get => _uV0Size;
        set
        {
            if (_uV0Size == value)
                return;
    
            _uV0Size = value;
            OnPropertyChanged(nameof(UV0Size));
        }
    }
    
    private uint _uV1Size;
    public uint UV1Size
    {
        get => _uV1Size;
        set
        {
            if (_uV1Size == value)
                return;
    
            _uV1Size = value;
            OnPropertyChanged(nameof(UV1Size));
        }
    }
    
    private uint _uV2Size;
    public uint UV2Size
    {
        get => _uV2Size;
        set
        {
            if (_uV2Size == value)
                return;
    
            _uV2Size = value;
            OnPropertyChanged(nameof(UV2Size));
        }
    }
    
    private uint _uV3Size;
    public uint UV3Size
    {
        get => _uV3Size;
        set
        {
            if (_uV3Size == value)
                return;
    
            _uV3Size = value;
            OnPropertyChanged(nameof(UV3Size));
        }
    }
    
    private uint _normalSize;
    public uint NormalSize
    {
        get => _normalSize;
        set
        {
            if (_normalSize == value)
                return;
    
            _normalSize = value;
            OnPropertyChanged(nameof(NormalSize));
        }
    }
    
    private uint _colourSize;
    public uint ColourSize
    {
        get => _colourSize;
        set
        {
            if (_colourSize == value)
                return;
    
            _colourSize = value;
            OnPropertyChanged(nameof(ColourSize));
        }
    }
    
    private uint _positionSize;
    public uint PositionSize
    {
        get => _positionSize;
        set
        {
            if (_positionSize == value)
                return;
    
            _positionSize = value;
            OnPropertyChanged(nameof(PositionSize));
        }
    }
    

    public override byte[] DataBytes
    {
        get
        {
            var data = new List<byte>((int)DataLength);

            data.AddRange(BitConverter.GetBytes(Version));
            data.AddRange(BitConverter.GetBytes(UV0Size));
            data.AddRange(BitConverter.GetBytes(UV1Size));
            data.AddRange(BitConverter.GetBytes(UV2Size));
            data.AddRange(BitConverter.GetBytes(UV3Size));
            data.AddRange(BitConverter.GetBytes(NormalSize));
            data.AddRange(BitConverter.GetBytes(ColourSize));
            data.AddRange(BitConverter.GetBytes(PositionSize));

            return [.. data];
        }
    }
    public override uint DataLength => sizeof(uint) + sizeof(uint) + sizeof(uint) + sizeof(uint) + sizeof(uint) + sizeof(uint) + sizeof(uint) + sizeof(uint);

    public VertexCompressionHintChunk(EndianAwareBinaryReader br) : this(br.ReadUInt32(), br.ReadUInt32(), br.ReadUInt32(), br.ReadUInt32(), br.ReadUInt32(), br.ReadUInt32(), br.ReadUInt32(), br.ReadUInt32())
    {
    }

    public VertexCompressionHintChunk(uint version, uint uv0Size, uint uv1Size, uint uv2Size, uint uv3Size, uint normalSize, uint colourSize, uint positionSize) : base(ChunkID)
    {
        _version = version;
        _uV0Size = uv0Size;
        _uV1Size = uv1Size;
        _uV2Size = uv2Size;
        _uV3Size = uv3Size;
        _normalSize = normalSize;
        _colourSize = colourSize;
        _positionSize = positionSize;
    }

    protected override void WriteData(EndianAwareBinaryWriter bw)
    {
        bw.Write(Version);
        bw.Write(UV0Size);
        bw.Write(UV1Size);
        bw.Write(UV2Size);
        bw.Write(UV3Size);
        bw.Write(NormalSize);
        bw.Write(ColourSize);
        bw.Write(PositionSize);
    }

    protected override Chunk CloneSelf() => new VertexCompressionHintChunk(Version, UV0Size, UV1Size, UV2Size, UV3Size, NormalSize, ColourSize, PositionSize);
}
