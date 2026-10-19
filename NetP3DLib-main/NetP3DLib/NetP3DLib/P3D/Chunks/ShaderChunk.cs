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
public class ShaderChunk : NamedChunk
{
    public const ChunkIdentifier ChunkID = ChunkIdentifier.Shader;

    [Flags]
    public enum VertexMasks : int
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
        BiNormal = 1 << 11,
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
    }

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
    
    private readonly P3DString _pddiShaderName;
    public string PddiShaderName
    {
        get => _pddiShaderName?.Value ?? string.Empty;
        set => _pddiShaderName.Value = value;
    }
    private uint _hasTranslucency;
    public bool HasTranslucency
    {
        get => _hasTranslucency != 0;
        set
        {
            if (HasTranslucency == value)
                return;

            _hasTranslucency = value ? 1u : 0u;
            OnPropertyChanged(nameof(HasTranslucency));
        }
    }
    
    private VertexMasks _vertexNeeds;
    public VertexMasks VertexNeeds
    {
        get => _vertexNeeds;
        set
        {
            if (_vertexNeeds == value)
                return;
    
            _vertexNeeds = value;
            OnPropertyChanged(nameof(VertexNeeds));
        }
    }
    
    private VertexMasks _vertexMask;
    public VertexMasks VertexMask
    {
        get => _vertexMask;
        set
        {
            if (_vertexMask == value)
                return;
    
            _vertexMask = value;
            OnPropertyChanged(nameof(VertexMask));
        }
    }
    
    public uint NumParams => (uint)Children.Count;

    public override byte[] DataBytes
    {
        get
        {
            var data = new List<byte>((int)DataLength);

            data.AddRange(BinaryExtensions.GetP3DStringBytes(Name));
            data.AddRange(BitConverter.GetBytes(Version));
            data.AddRange(BinaryExtensions.GetP3DStringBytes(PddiShaderName));
            data.AddRange(BitConverter.GetBytes(_hasTranslucency));
            data.AddRange(BitConverter.GetBytes((int)VertexNeeds));
            data.AddRange(BitConverter.GetBytes((int)~VertexMask));
            data.AddRange(BitConverter.GetBytes(NumParams));

            return [.. data];
        }
    }
    public override uint DataLength => BinaryExtensions.GetP3DStringLength(Name) + sizeof(uint) + BinaryExtensions.GetP3DStringLength(PddiShaderName) + sizeof(uint) + sizeof(int) + sizeof(int) + sizeof(uint);

    [System.Diagnostics.CodeAnalysis.SuppressMessage("Style", "IDE0059:Unnecessary assignment of a value", Justification = "We want to read the value to progress the BinaryReader, but not set the value anywhere because it's calculated dynamically.")]
    public ShaderChunk(EndianAwareBinaryReader br) : this(br.ReadP3DString(), br.ReadUInt32(), br.ReadP3DString(), br.ReadUInt32(), (VertexMasks)br.ReadInt32(), (VertexMasks)~br.ReadInt32())
    {
        var numParams = br.ReadUInt32();
    }

    public ShaderChunk(string name, uint version, string pddiShaderName, bool hasTranslucency, VertexMasks vertexNeeds, VertexMasks vertexMask) : this(name, version, pddiShaderName, hasTranslucency ? 1u : 0u, vertexNeeds, vertexMask)
    {
    }

    public ShaderChunk(string name, uint version, string pddiShaderName, uint hasTranslucency, VertexMasks vertexNeeds, VertexMasks vertexMask) : base(ChunkID, name)
    {
        _version = version;
        _pddiShaderName = new(this, pddiShaderName, nameof(PddiShaderName));
        _hasTranslucency = hasTranslucency;
        _vertexNeeds = vertexNeeds;
        _vertexMask = vertexMask;
    }

    public override IEnumerable<InvalidP3DException> ValidateChunk()
    {
        foreach (var error in base.ValidateChunk())
            yield return error;

        if (!PddiShaderName.IsValidP3DString())
            yield return new InvalidP3DStringException(this, nameof(PddiShaderName), PddiShaderName);
    }

    protected override void WriteData(EndianAwareBinaryWriter bw)
    {
        bw.WriteP3DString(Name);
        bw.Write(Version);
        bw.WriteP3DString(PddiShaderName);
        bw.Write(_hasTranslucency);
        bw.Write((int)VertexNeeds);
        bw.Write((int)~VertexMask);
        bw.Write(NumParams);
    }

    protected override Chunk CloneSelf() => new ShaderChunk(Name, Version, PddiShaderName, HasTranslucency, VertexNeeds, VertexMask);
}
