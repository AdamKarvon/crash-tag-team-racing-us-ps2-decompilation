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
public class LensFlareGroupChunk : NamedChunk
{
    public const ChunkIdentifier ChunkID = ChunkIdentifier.Lens_Flare_Group;

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
    private uint _zTest;
    public bool ZTest
    {
        get => _zTest == 1;
        set
        {
            if (ZTest == value)
                return;

            _zTest = value ? 1u : 0u;
            OnPropertyChanged(nameof(ZTest));
        }
    }
    private uint _zWrite;
    public bool ZWrite
    {
        get => _zWrite == 1;
        set
        {
            if (ZWrite == value)
                return;

            _zWrite = value ? 1u : 0u;
            OnPropertyChanged(nameof(ZWrite));
        }
    }
    private uint _fog;
    public bool Fog
    {
        get => _fog == 1;
        set
        {
            if (Fog == value)
                return;

            _fog = value ? 1u : 0u;
            OnPropertyChanged(nameof(Fog));
        }
    }
    
    private float _sourceRadius;
    public float SourceRadius
    {
        get => _sourceRadius;
        set
        {
            if (_sourceRadius == value)
                return;
    
            _sourceRadius = value;
            OnPropertyChanged(nameof(SourceRadius));
        }
    }
    
    private float _edgeRadius;
    public float EdgeRadius
    {
        get => _edgeRadius;
        set
        {
            if (_edgeRadius == value)
                return;
    
            _edgeRadius = value;
            OnPropertyChanged(nameof(EdgeRadius));
        }
    }
    
    public uint NumLensFlares => GetChildCount(ChunkIdentifier.Lens_Flare);

    public override byte[] DataBytes
    {
        get
        {
            var data = new List<byte>((int)DataLength);

            data.AddRange(BitConverter.GetBytes(Version));
            data.AddRange(BinaryExtensions.GetP3DStringBytes(Name));
            data.AddRange(BinaryExtensions.GetP3DStringBytes(ShaderName));
            data.AddRange(BitConverter.GetBytes(_zTest));
            data.AddRange(BitConverter.GetBytes(_zWrite));
            data.AddRange(BitConverter.GetBytes(_fog));
            data.AddRange(BitConverter.GetBytes(SourceRadius));
            data.AddRange(BitConverter.GetBytes(EdgeRadius));
            data.AddRange(BitConverter.GetBytes(NumLensFlares));

            return [.. data];
        }
    }
    public override uint DataLength => sizeof(uint) + BinaryExtensions.GetP3DStringLength(Name) + BinaryExtensions.GetP3DStringLength(ShaderName) + sizeof(uint) + sizeof(uint) + sizeof(uint) + sizeof(float) + sizeof(float) + sizeof(uint);

    [System.Diagnostics.CodeAnalysis.SuppressMessage("Style", "IDE0059:Unnecessary assignment of a value", Justification = "We want to read the value to progress the BinaryReader, but not set the value anywhere because it's calculated dynamically.")]
    public LensFlareGroupChunk(EndianAwareBinaryReader br) : this(br.ReadUInt32(), br.ReadP3DString(), br.ReadP3DString(), br.ReadUInt32(), br.ReadUInt32(), br.ReadUInt32(), br.ReadSingle(), br.ReadSingle())
    {
        var numLensFlares = br.ReadUInt32();
    }

    public LensFlareGroupChunk(uint version, string name, string shaderName, bool zTest, bool zWrite, bool fog, float sourceRadius, float edgeRadius) : this(version, name, shaderName, zTest ? 1u : 0u, zWrite ? 1u : 0u, fog ? 1u : 0u, sourceRadius, edgeRadius)
    {
    }

    public LensFlareGroupChunk(uint version, string name, string shaderName, uint zTest, uint zWrite, uint fog, float sourceRadius, float edgeRadius) : base(ChunkID, name)
    {
        _version = version;
        _shaderName = new(this, shaderName, nameof(ShaderName));
        _zTest = zTest;
        _zWrite = zWrite;
        _fog = fog;
        _sourceRadius = sourceRadius;
        _edgeRadius = edgeRadius;
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
        bw.WriteP3DString(Name);
        bw.WriteP3DString(ShaderName);
        bw.Write(_zTest);
        bw.Write(_zWrite);
        bw.Write(_fog);
        bw.Write(SourceRadius);
        bw.Write(EdgeRadius);
        bw.Write(NumLensFlares);
    }

    protected override Chunk CloneSelf() => new LensFlareGroupChunk(Version, Name, ShaderName, ZTest, ZWrite, Fog, SourceRadius, EdgeRadius);
}
