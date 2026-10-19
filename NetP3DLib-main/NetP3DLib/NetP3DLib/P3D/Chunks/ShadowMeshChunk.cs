using NetP3DLib.IO;
using NetP3DLib.P3D.Attributes;
using NetP3DLib.P3D.Enums;
using NetP3DLib.P3D.Extensions;
using System;
using System.Collections.Generic;

namespace NetP3DLib.P3D.Chunks;

[ChunkAttributes(ChunkID)]
public class ShadowMeshChunk : NamedChunk
{
    public const ChunkIdentifier ChunkID = ChunkIdentifier.Shadow_Mesh;

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
            data.AddRange(BitConverter.GetBytes(NumVertices));
            data.AddRange(BitConverter.GetBytes(NumTriangles));

            return [.. data];
        }
    }
    public override uint DataLength => BinaryExtensions.GetP3DStringLength(Name) + sizeof(uint) + sizeof(uint) + sizeof(uint);

    public ShadowMeshChunk(EndianAwareBinaryReader br) : this(br.ReadP3DString(), br.ReadUInt32(), br.ReadUInt32(), br.ReadUInt32())
    {
    }

    public ShadowMeshChunk(string name, uint version, uint numVertices, uint numTriangles) : base(ChunkID, name)
    {
        _version = version;
        _numVertices = numVertices;
        _numTriangles = numTriangles;
    }

    protected override void WriteData(EndianAwareBinaryWriter bw)
    {
        bw.WriteP3DString(Name);
        bw.Write(Version);
        bw.Write(NumVertices);
        bw.Write(NumTriangles);
    }

    protected override Chunk CloneSelf() => new ShadowMeshChunk(Name, Version, NumVertices, NumTriangles);
}
