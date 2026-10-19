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
public class OldBillboardQuadGroupChunk : NamedChunk
{
    public const ChunkIdentifier ChunkID = ChunkIdentifier.Old_Billboard_Quad_Group;

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
    
    private readonly P3DString _shader;
    public string Shader
    {
        get => _shader?.Value ?? string.Empty;
        set => _shader.Value = value;
    }
    private uint _zTest;
    public bool ZTest
    {
        get => _zTest != 0;
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
        get => _zWrite != 0;
        set
        {
            if (ZWrite == value)
                return;

            _zWrite = value ? 1u : 0u;
            OnPropertyChanged(nameof(ZWrite));
        }
    }
    
    private uint _occlusion;
    public uint Occlusion
    {
        get => _occlusion;
        set
        {
            if (_occlusion == value)
                return;
    
            _occlusion = value;
            OnPropertyChanged(nameof(Occlusion));
        }
    }
    
    public uint NumQuads => GetChildCount(ChunkIdentifier.Old_Billboard_Quad);

    public override byte[] DataBytes
    {
        get
        {
            var data = new List<byte>((int)DataLength);

            data.AddRange(BitConverter.GetBytes(Version));
            data.AddRange(BinaryExtensions.GetP3DStringBytes(Name));
            data.AddRange(BinaryExtensions.GetP3DStringBytes(Shader));
            data.AddRange(BitConverter.GetBytes(_zTest));
            data.AddRange(BitConverter.GetBytes(_zWrite));
            data.AddRange(BitConverter.GetBytes(Occlusion));
            data.AddRange(BitConverter.GetBytes(NumQuads));

            return [.. data];
        }
    }
    public override uint DataLength => sizeof(uint) + BinaryExtensions.GetP3DStringLength(Name) + BinaryExtensions.GetP3DStringLength(Shader) + sizeof(uint) + sizeof(uint) + sizeof(uint) + sizeof(uint);

    [System.Diagnostics.CodeAnalysis.SuppressMessage("Style", "IDE0059:Unnecessary assignment of a value", Justification = "We want to read the value to progress the BinaryReader, but not set the value anywhere because it's calculated dynamically.")]
    public OldBillboardQuadGroupChunk(EndianAwareBinaryReader br) : this(br.ReadUInt32(), br.ReadP3DString(), br.ReadP3DString(), br.ReadUInt32(), br.ReadUInt32(), br.ReadUInt32())
    {
        var numQuads = br.ReadUInt32();
    }

    public OldBillboardQuadGroupChunk(uint version, string name, string shader, bool zTest, bool zWrite, uint occlusion) : this(version, name, shader, zTest ? 1u : 0u, zWrite ? 1u : 0u, occlusion)
    {
    }

    public OldBillboardQuadGroupChunk(uint version, string name, string shader, uint zTest, uint zWrite, uint occlusion) : base(ChunkID, name)
    {
        _version = version;
        _shader = new(this, shader, nameof(Shader));
        _zTest = zTest;
        _zWrite = zWrite;
        _occlusion = occlusion;
    }

    public override IEnumerable<InvalidP3DException> ValidateChunk()
    {
        foreach (var error in base.ValidateChunk())
            yield return error;

        if (!Shader.IsValidP3DString())
            yield return new InvalidP3DStringException(this, nameof(Shader), Shader);

        if ((ParentChunk != null || ParentFile != null) && FindNamedChunkInParentHierarchy<ShaderChunk>(Shader) == null)
            yield return new InvalidP3DException(this, $"Could not find shader with name \"{Shader}\" in the parent hierarchy.");
    }

    protected override void WriteData(EndianAwareBinaryWriter bw)
    {
        bw.Write(Version);
        bw.WriteP3DString(Name);
        bw.WriteP3DString(Shader);
        bw.Write(_zTest);
        bw.Write(_zWrite);
        bw.Write(Occlusion);
        bw.Write(NumQuads);
    }

    protected override Chunk CloneSelf() => new OldBillboardQuadGroupChunk(Version, Name, Shader, ZTest, ZWrite, Occlusion);
}
