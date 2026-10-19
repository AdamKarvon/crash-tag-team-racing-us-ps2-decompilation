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
public class ShadowSkinChunk : NamedChunk
{
    public const ChunkIdentifier ChunkID = ChunkIdentifier.Shadow_Skin;

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
    
    private readonly P3DString _skeletonName;
    public string SkeletonName
    {
        get => _skeletonName?.Value ?? string.Empty;
        set => _skeletonName.Value = value;
    }
    // TODO: Calculate from children
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
    
    private uint _numTriangles;
    public uint NumTriangles
    {
        get => _numTriangles;
        set
        {
            if (_numTriangles == value)
                return;
    
            _numTriangles = value;
            OnPropertyChanged(nameof(NumTriangles));
        }
    }
    

    public override byte[] DataBytes
    {
        get
        {
            var data = new List<byte>((int)DataLength);

            data.AddRange(BinaryExtensions.GetP3DStringBytes(Name));
            data.AddRange(BitConverter.GetBytes(Version));
            data.AddRange(BinaryExtensions.GetP3DStringBytes(SkeletonName));
            data.AddRange(BitConverter.GetBytes(NumVertices));
            data.AddRange(BitConverter.GetBytes(NumTriangles));

            return [.. data];
        }
    }
    public override uint DataLength => BinaryExtensions.GetP3DStringLength(Name) + sizeof(uint) + BinaryExtensions.GetP3DStringLength(SkeletonName) + sizeof(uint) + sizeof(uint);

    public ShadowSkinChunk(EndianAwareBinaryReader br) : this(br.ReadP3DString(), br.ReadUInt32(), br.ReadP3DString(), br.ReadUInt32(), br.ReadUInt32())
    {
    }

    public ShadowSkinChunk(string name, uint version, string skeletonName, uint numVertices, uint numTriangles) : base(ChunkID, name)
    {
        _version = version;
        _skeletonName = new(this, skeletonName, nameof(SkeletonName));
        _numVertices = numVertices;
        _numTriangles = numTriangles;
    }

    public override IEnumerable<InvalidP3DException> ValidateChunk()
    {
        foreach (var error in base.ValidateChunk())
            yield return error;

        if (!SkeletonName.IsValidP3DString())
            yield return new InvalidP3DStringException(this, nameof(SkeletonName), SkeletonName);
    }

    protected override void WriteData(EndianAwareBinaryWriter bw)
    {
        bw.WriteP3DString(Name);
        bw.Write(Version);
        bw.WriteP3DString(SkeletonName);
        bw.Write(NumVertices);
        bw.Write(NumTriangles);
    }

    protected override Chunk CloneSelf() => new ShadowSkinChunk(Name, Version, SkeletonName, NumVertices, NumTriangles);
}
