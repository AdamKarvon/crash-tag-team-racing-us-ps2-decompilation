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
public class PrimitiveGroupChunk : Chunk
{
    public const ChunkIdentifier ChunkID = ChunkIdentifier.Primitive_Group;

    public enum PrimitiveTypes : uint
    {
        TriangleList,
        TriangleStrip,
        LineList,
        LineStrip,
        Points,
    }

    [Flags]
    public enum VertexTypes : uint
    {
        None = 0,
        UVs = 1,
        UVs2 = 2,
        UVs3 = 3,
        UVs4 = 4,
        UVs5 = 5,
        UVs6 = 6,
        UVs7 = 7,
        UVs8 = 8,
        Normals = 1 << 4,
        Colours = 1 << 5,
        Specular = 1 << 6,
        Matrices = 1 << 7,
        Weights = 1 << 8,
        Size = 1 << 9,
        W = 1 << 10,
        Binormal = 1 << 11,
        Tangent = 1 << 12,
        Position = 1 << 13,
        Colours2 = 1 << 14,
        ColourCount1 = 1 << 15,
        ColourCount2 = 2 << 15,
        ColourCount3 = 3 << 15,
        ColourCount4 = 4 << 15,
        ColourCount5 = 5 << 15,
        ColourCount6 = 6 << 15,
        ColourCount7 = 7 << 15,
        ColourMaskOffset = 15,
    }

    private static readonly Dictionary<uint, VertexTypes> VertexTypeMap = new() {
        { (uint)ChunkIdentifier.Packed_Normal_List, VertexTypes.Normals },
        { (uint)ChunkIdentifier.Normal_List, VertexTypes.Normals },
        { (uint)ChunkIdentifier.Colour_List, VertexTypes.Colours },
        { (uint)ChunkIdentifier.Multi_Colour_List, VertexTypes.Colours2 },
        { (uint)ChunkIdentifier.Matrix_List, VertexTypes.Matrices },
        { (uint)ChunkIdentifier.Matrix_Palette, VertexTypes.Matrices },
        { (uint)ChunkIdentifier.Weight_List, VertexTypes.Weights },
        { (uint)ChunkIdentifier.Position_List, VertexTypes.Position },
        { (uint)ChunkIdentifier.Binormal_List, VertexTypes.Binormal },
        { (uint)ChunkIdentifier.Tangent_List, VertexTypes.Tangent },
    };

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
    
    private readonly P3DString _shaderName;
    public string ShaderName
    {
        get => _shaderName?.Value ?? string.Empty;
        set => _shaderName.Value = value;
    }
    
    private PrimitiveTypes _primitiveType;
    public PrimitiveTypes PrimitiveType
    {
        get => _primitiveType;
        set
        {
            if (_primitiveType == value)
                return;
    
            _primitiveType = value;
            OnPropertyChanged(nameof(PrimitiveType));
        }
    }
    
    public VertexTypes VertexType
    {
        get
        {
            VertexTypes vertexType = VertexTypes.None;

            var uvN = 0;
            foreach (var chunk in Children)
            {
                if (chunk.ID == (uint)ChunkIdentifier.UV_List)
                {
                    uvN++;
                }
                else if (VertexTypeMap.TryGetValue(chunk.ID, out var subType))
                {
                    vertexType |= subType;
                }
            }

            if (uvN > 8)
                throw new InvalidP3DException(this, "Primitive Groups can only have a maximum of 8 UV Lists.");

            if (uvN > 0)
                vertexType |= (VertexTypes)uvN;

            return vertexType;
        }
    }
    
    private uint _numVertices;
    public uint NumVertices
    {
        get => _numVertices;
        set
        {
            if (_numVertices == value)
                return;
    
            _numVertices = value;
            OnPropertyChanged(nameof(NumVertices));
        }
    }
    
    private uint _numIndices;
    public uint NumIndices
    {
        get => _numIndices;
        set
        {
            if (_numIndices == value)
                return;
    
            _numIndices = value;
            OnPropertyChanged(nameof(NumIndices));
        }
    }
    
    private uint _numMatrices;
    public uint NumMatrices
    {
        get => _numMatrices;
        set
        {
            if (_numMatrices == value)
                return;
    
            _numMatrices = value;
            OnPropertyChanged(nameof(NumMatrices));
        }
    }
    
    private uint _memoryImaged;
    public uint MemoryImaged
    {
        get => _memoryImaged;
        set
        {
            if (_memoryImaged == value)
                return;
    
            _memoryImaged = value;
            OnPropertyChanged(nameof(MemoryImaged));
        }
    }
    
    private uint _optimized;
    public uint Optimized
    {
        get => _optimized;
        set
        {
            if (_optimized == value)
                return;
    
            _optimized = value;
            OnPropertyChanged(nameof(Optimized));
        }
    }
    
    private uint _vertexAnimated;
    public uint VertexAnimated
    {
        get => _vertexAnimated;
        set
        {
            if (_vertexAnimated == value)
                return;
    
            _vertexAnimated = value;
            OnPropertyChanged(nameof(VertexAnimated));
        }
    }
    
    private uint _vertexAnimationMask;
    public uint VertexAnimationMask
    {
        get => _vertexAnimationMask;
        set
        {
            if (_vertexAnimationMask == value)
                return;
    
            _vertexAnimationMask = value;
            OnPropertyChanged(nameof(VertexAnimationMask));
        }
    }
    

    public override byte[] DataBytes
    {
        get
        {
            var data = new List<byte>((int)DataLength);

            data.AddRange(BitConverter.GetBytes(Version));
            data.AddRange(BinaryExtensions.GetP3DStringBytes(ShaderName));
            data.AddRange(BitConverter.GetBytes((uint)PrimitiveType));
            data.AddRange(BitConverter.GetBytes((uint)VertexType));
            data.AddRange(BitConverter.GetBytes(NumVertices));
            data.AddRange(BitConverter.GetBytes(NumIndices));
            data.AddRange(BitConverter.GetBytes(NumMatrices));
            data.AddRange(BitConverter.GetBytes(MemoryImaged));
            data.AddRange(BitConverter.GetBytes(Optimized));
            data.AddRange(BitConverter.GetBytes(VertexAnimated));
            data.AddRange(BitConverter.GetBytes(VertexAnimationMask));

            return [.. data];
        }
    }
    public override uint DataLength => sizeof(uint) + BinaryExtensions.GetP3DStringLength(ShaderName) + sizeof(uint) + sizeof(uint) + sizeof(uint) + sizeof(uint) + sizeof(uint) + sizeof(uint) + sizeof(uint) + sizeof(uint) + sizeof(uint);

    public PrimitiveGroupChunk(EndianAwareBinaryReader br) : this(br.ReadUInt32(), br.ReadP3DString(), (PrimitiveTypes)br.ReadUInt32(), br.SkipAndRead(sizeof(uint), br.ReadUInt32), br.ReadUInt32(), br.ReadUInt32(), br.ReadUInt32(), br.ReadUInt32(), br.ReadUInt32(), br.ReadUInt32())
    {
    }

    public PrimitiveGroupChunk(uint version, string shaderName, PrimitiveTypes primitiveType, uint numVertices, uint numIndices, uint numMatrices, uint memoryImaged, uint optimized, uint vertexAnimated, uint vertexAnimationMask) : base(ChunkID)
    {
        _version = version;
        _shaderName = new(this, shaderName, nameof(ShaderName));
        _primitiveType = primitiveType;
        _numVertices = numVertices;
        _numIndices = numIndices;
        _numMatrices = numMatrices;
        _memoryImaged = memoryImaged;
        _optimized = optimized;
        _vertexAnimated = vertexAnimated;
        _vertexAnimationMask = vertexAnimationMask;
    }

    public override IEnumerable<InvalidP3DException> ValidateChunk()
    {
        foreach (var error in base.ValidateChunk())
            yield return error;

        if (!ShaderName.IsValidP3DString())
            yield return new InvalidP3DStringException(this, nameof(ShaderName), ShaderName);

        if ((ParentChunk != null || ParentFile != null) && FindNamedChunkInParentHierarchy<ShaderChunk>(ShaderName) == null)
            yield return new InvalidP3DException(this, $"Could not find the shader named \"{ShaderName}\" in the parent hierarchy.");
    }

    protected override void WriteData(EndianAwareBinaryWriter bw)
    {
        bw.Write(Version);
        bw.WriteP3DString(ShaderName);
        bw.Write((uint)PrimitiveType);
        bw.Write((uint)VertexType);
        bw.Write(NumVertices);
        bw.Write(NumIndices);
        bw.Write(NumMatrices);
        bw.Write(MemoryImaged);
        bw.Write(Optimized);
        bw.Write(VertexAnimated);
        bw.Write(VertexAnimationMask);
    }

    protected override Chunk CloneSelf() => new PrimitiveGroupChunk(Version, ShaderName, PrimitiveType, NumVertices, NumIndices, NumMatrices, MemoryImaged, Optimized, VertexAnimated, VertexAnimationMask);

    public override string ToString() => $"\"{ShaderName}\" ({GetChunkType(this)} (0x{ID:X}))";
}
