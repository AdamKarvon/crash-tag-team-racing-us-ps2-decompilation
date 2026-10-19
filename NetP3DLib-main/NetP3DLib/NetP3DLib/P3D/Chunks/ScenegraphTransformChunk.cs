using NetP3DLib.IO;
using NetP3DLib.P3D.Attributes;
using NetP3DLib.P3D.Enums;
using NetP3DLib.P3D.Extensions;
using System;
using System.Collections.Generic;
using System.Numerics;

namespace NetP3DLib.P3D.Chunks;

[ChunkAttributes(ChunkID)]
public class ScenegraphTransformChunk : NamedChunk
{
    public const ChunkIdentifier ChunkID = ChunkIdentifier.Scenegraph_Transform;

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
    
    public uint NumChildren => (uint)Children.Count;
    private Matrix4x4 _transform;
    public Matrix4x4 Transform
    {
        get => _transform;
        set
        {
            if (_transform == value)
                return;
    
            _transform = value;
            OnPropertyChanged(nameof(Transform));
        }
    }
    

    public override byte[] DataBytes
    {
        get
        {
            var data = new List<byte>((int)DataLength);

            data.AddRange(BitConverter.GetBytes(Version));
            data.AddRange(BinaryExtensions.GetP3DStringBytes(Name));
            data.AddRange(BitConverter.GetBytes(NumChildren));
            data.AddRange(BinaryExtensions.GetBytes(Transform));

            return [.. data];
        }
    }
    public override uint DataLength => sizeof(uint) + BinaryExtensions.GetP3DStringLength(Name) + sizeof(uint) + sizeof(float) * 16;

    [System.Diagnostics.CodeAnalysis.SuppressMessage("Style", "IDE0059:Unnecessary assignment of a value", Justification = "We want to read the value to progress the BinaryReader, but not set the value anywhere because it's calculated dynamically.")]
    public ScenegraphTransformChunk(EndianAwareBinaryReader br) : this(br.ReadUInt32(), br.ReadP3DString(), br.SkipAndRead(sizeof(uint), br.ReadMatrix4x4))
    {
    }

    public ScenegraphTransformChunk(uint version, string name, Matrix4x4 transform) : base(ChunkID, name)
    {
        _version = version;
        _transform = transform;
    }

    protected override void WriteData(EndianAwareBinaryWriter bw)
    {
        bw.Write(Version);
        bw.WriteP3DString(Name);
        bw.Write(NumChildren);
        bw.Write(Transform);
    }

    protected override Chunk CloneSelf() => new ScenegraphTransformChunk(Version, Name, Transform);
}
