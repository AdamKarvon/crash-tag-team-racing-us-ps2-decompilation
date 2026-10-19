using NetP3DLib.IO;
using NetP3DLib.P3D.Attributes;
using NetP3DLib.P3D.Enums;
using NetP3DLib.P3D.Extensions;
using System;
using System.Collections.Generic;
using System.Numerics;

namespace NetP3DLib.P3D.Chunks;

[ChunkAttributes(ChunkID)]
public class SkeletonJoint2Chunk : NamedChunk
{
    public const ChunkIdentifier ChunkID = ChunkIdentifier.Skeleton_Joint_2;

    private uint _parent;
    public uint Parent
    {
        get => _parent;
        set
        {
            if (_parent == value)
                return;
    
            _parent = value;
            OnPropertyChanged(nameof(Parent));
        }
    }
    
    private Matrix4x4 _restPose;
    public Matrix4x4 RestPose
    {
        get => _restPose;
        set
        {
            if (_restPose == value)
                return;
    
            _restPose = value;
            OnPropertyChanged(nameof(RestPose));
        }
    }
    

    public override byte[] DataBytes
    {
        get
        {
            var data = new List<byte>((int)DataLength);

            data.AddRange(BinaryExtensions.GetP3DStringBytes(Name));
            data.AddRange(BitConverter.GetBytes(Parent));
            data.AddRange(BinaryExtensions.GetBytes(RestPose));

            return [.. data];
        }
    }
    public override uint DataLength => BinaryExtensions.GetP3DStringLength(Name) + sizeof(uint) + sizeof(float) * 16;

    public SkeletonJoint2Chunk(EndianAwareBinaryReader br) : this(br.ReadP3DString(), br.ReadUInt32(), br.ReadMatrix4x4())
    {
    }

    public SkeletonJoint2Chunk(string name, uint parent, Matrix4x4 restPose) : base(ChunkID, name)
    {
        _parent = parent;
        _restPose = restPose;
    }

    protected override void WriteData(EndianAwareBinaryWriter bw)
    {
        bw.WriteP3DString(Name);
        bw.Write(Parent);
        bw.Write(RestPose);
    }

    protected override Chunk CloneSelf() => new SkeletonJoint2Chunk(Name, Parent, RestPose);
}
