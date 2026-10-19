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
public class BillboardQuadGroupChunk : NamedChunk
{
    public const ChunkIdentifier ChunkID = ChunkIdentifier.Billboard_Quad_Group;

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
    
    private readonly P3DString _shader;
    public string Shader
    {
        get => _shader?.Value ?? string.Empty;
        set => _shader.Value = value;
    }
    
    private uint _cutOffEnabled;
    public uint CutOffEnabled
    {
        get => _cutOffEnabled;
        set
        {
            if (_cutOffEnabled == value)
                return;
    
            _cutOffEnabled = value;
            OnPropertyChanged(nameof(CutOffEnabled));
        }
    }
    
    private uint _zTest;
    public uint ZTest
    {
        get => _zTest;
        set
        {
            if (_zTest == value)
                return;
    
            _zTest = value;
            OnPropertyChanged(nameof(ZTest));
        }
    }
    
    private uint _zWrite;
    public uint ZWrite
    {
        get => _zWrite;
        set
        {
            if (_zWrite == value)
                return;
    
            _zWrite = value;
            OnPropertyChanged(nameof(ZWrite));
        }
    }
    
    private uint _occlusionCulling;
    public uint OcclusionCulling
    {
        get => _occlusionCulling;
        set
        {
            if (_occlusionCulling == value)
                return;
    
            _occlusionCulling = value;
            OnPropertyChanged(nameof(OcclusionCulling));
        }
    }
    
    public uint NumQuads => GetChildCount(ChunkIdentifier.Billboard_Quad);

    public override byte[] DataBytes
    {
        get
        {
            var data = new List<byte>((int)DataLength);

            data.AddRange(BitConverter.GetBytes(Version));
            data.AddRange(BinaryExtensions.GetP3DStringBytes(Name));
            data.AddRange(BinaryExtensions.GetP3DStringBytes(Shader));
            data.AddRange(BitConverter.GetBytes(CutOffEnabled));
            data.AddRange(BitConverter.GetBytes(ZTest));
            data.AddRange(BitConverter.GetBytes(ZWrite));
            data.AddRange(BitConverter.GetBytes(OcclusionCulling));
            data.AddRange(BitConverter.GetBytes(NumQuads));

            return [.. data];
        }
    }
    public override uint DataLength => sizeof(uint) + BinaryExtensions.GetP3DStringLength(Name) + BinaryExtensions.GetP3DStringLength(Shader) + sizeof(uint) + sizeof(uint) + sizeof(uint) + sizeof(uint) + sizeof(uint);

    [System.Diagnostics.CodeAnalysis.SuppressMessage("Style", "IDE0059:Unnecessary assignment of a value", Justification = "We want to read the value to progress the BinaryReader, but not set the value anywhere because it's calculated dynamically.")]
    public BillboardQuadGroupChunk(EndianAwareBinaryReader br) : this(br.ReadUInt32(), br.ReadP3DString(), br.ReadP3DString(), br.ReadUInt32(), br.ReadUInt32(), br.ReadUInt32(), br.ReadUInt32())
    {
        var numQuads = br.ReadUInt32();
    }

    public BillboardQuadGroupChunk(uint version, string name, string shader, uint cutOffEnabled, uint zTest, uint zWrite, uint occlusionCulling) : base(ChunkID, name)
    {
        _version = version;
        _shader = new(this, shader, nameof(Shader));
        _cutOffEnabled = cutOffEnabled;
        _zTest = zTest;
        _zWrite = zWrite;
        _occlusionCulling = occlusionCulling;
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
        bw.Write(Version);
        bw.WriteP3DString(Name);
        bw.WriteP3DString(Shader);
        bw.Write(CutOffEnabled);
        bw.Write(ZTest);
        bw.Write(ZWrite);
        bw.Write(OcclusionCulling);
        bw.Write(NumQuads);
    }

    protected override Chunk CloneSelf() => new BillboardQuadGroupChunk(Version, Name, Shader, CutOffEnabled, ZTest, ZWrite, OcclusionCulling);
}
