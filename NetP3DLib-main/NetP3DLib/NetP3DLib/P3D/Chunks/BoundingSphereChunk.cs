using NetP3DLib.IO;
using NetP3DLib.P3D.Attributes;
using NetP3DLib.P3D.Enums;
using NetP3DLib.P3D.Extensions;
using System;
using System.Collections.Generic;
using System.Numerics;

namespace NetP3DLib.P3D.Chunks;

[ChunkAttributes(ChunkID)]
public class BoundingSphereChunk : Chunk
{
    public const ChunkIdentifier ChunkID = ChunkIdentifier.Bounding_Sphere;

    private Vector3 _centre;
    public Vector3 Centre
    {
        get => _centre;
        set
        {
            if (_centre == value)
                return;
    
            _centre = value;
            OnPropertyChanged(nameof(Centre));
        }
    }
    
    private float _radius;
    public float Radius
    {
        get => _radius;
        set
        {
            if (_radius == value)
                return;
    
            _radius = value;
            OnPropertyChanged(nameof(Radius));
        }
    }
    

    public override byte[] DataBytes
    {
        get
        {
            var data = new List<byte>((int)DataLength);

            data.AddRange(BinaryExtensions.GetBytes(Centre));
            data.AddRange(BitConverter.GetBytes(Radius));

            return [.. data];
        }
    }
    public override uint DataLength => sizeof(float) * 3 + sizeof(float);

    public BoundingSphereChunk(EndianAwareBinaryReader br) : this(br.ReadVector3(), br.ReadSingle())
    {
    }

    public BoundingSphereChunk(Vector3 centre, float radius) : base(ChunkID)
    {
        _centre = centre;
        _radius = radius;
    }

    protected override void WriteData(EndianAwareBinaryWriter bw)
    {
        bw.Write(Centre);
        bw.Write(Radius);
    }

    protected override Chunk CloneSelf() => new BoundingSphereChunk(Centre, Radius);
}
