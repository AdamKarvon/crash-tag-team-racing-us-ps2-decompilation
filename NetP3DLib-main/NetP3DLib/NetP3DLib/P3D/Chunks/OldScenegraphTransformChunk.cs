using NetP3DLib.IO;
using NetP3DLib.P3D.Attributes;
using NetP3DLib.P3D.Enums;
using NetP3DLib.P3D.Extensions;
using System;
using System.Collections.Generic;
using System.Numerics;

namespace NetP3DLib.P3D.Chunks;

[ChunkAttributes(ChunkID)]
public class OldScenegraphTransformChunk : NamedChunk
{
    public const ChunkIdentifier ChunkID = ChunkIdentifier.Old_Scenegraph_Transform;

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

            data.AddRange(BinaryExtensions.GetP3DStringBytes(Name));
            data.AddRange(BitConverter.GetBytes(NumChildren));
            data.AddRange(BinaryExtensions.GetBytes(Transform));

            return [.. data];
        }
    }
    public override uint DataLength => BinaryExtensions.GetP3DStringLength(Name) + sizeof(uint) + sizeof(float) * 16;

    public OldScenegraphTransformChunk(EndianAwareBinaryReader br) : this(br.ReadP3DString(), br.SkipAndRead(sizeof(uint), br.ReadMatrix4x4))
    {
    }

    public OldScenegraphTransformChunk(string name, Matrix4x4 transform) : base(ChunkID, name)
    {
        _transform = transform;
    }

    protected override void WriteData(EndianAwareBinaryWriter bw)
    {
        bw.WriteP3DString(Name);
        bw.Write(NumChildren);
        bw.Write(Transform);
    }

    protected override Chunk CloneSelf() => new OldScenegraphTransformChunk(Name, Transform);
}
