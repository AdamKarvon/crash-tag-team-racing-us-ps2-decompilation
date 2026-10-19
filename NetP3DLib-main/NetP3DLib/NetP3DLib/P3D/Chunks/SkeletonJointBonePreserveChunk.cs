using NetP3DLib.IO;
using NetP3DLib.P3D.Attributes;
using NetP3DLib.P3D.Enums;
using System;
using System.Collections.Generic;

namespace NetP3DLib.P3D.Chunks;

[ChunkAttributes(ChunkID)]
public class SkeletonJointBonePreserveChunk : Chunk
{
    public const ChunkIdentifier ChunkID = ChunkIdentifier.Skeleton_Joint_Bone_Preserve;

    private uint _preserveBoneLengths;
    public bool PreserveBoneLengths
    {
        get => _preserveBoneLengths != 0;
        set
        {
            if (PreserveBoneLengths == value)
                return;

            _preserveBoneLengths = value ? 1u : 0u;
            OnPropertyChanged(nameof(PreserveBoneLengths));
        }
    }

    public override byte[] DataBytes
    {
        get
        {
            var data = new List<byte>((int)DataLength);

            data.AddRange(BitConverter.GetBytes(_preserveBoneLengths));

            return [.. data];
        }
    }
    public override uint DataLength => sizeof(uint);

    public SkeletonJointBonePreserveChunk(EndianAwareBinaryReader br) : this(br.ReadUInt32())
    {
    }

    public SkeletonJointBonePreserveChunk(bool preserveBoneLengths) : this(preserveBoneLengths ? 1u : 0u)
    {
    }

    public SkeletonJointBonePreserveChunk(uint preserveBoneLengths) : base(ChunkID)
    {
        _preserveBoneLengths = preserveBoneLengths;
    }

    protected override void WriteData(EndianAwareBinaryWriter bw)
    {
        bw.Write(_preserveBoneLengths);
    }

    protected override Chunk CloneSelf() => new SkeletonJointBonePreserveChunk(PreserveBoneLengths);
}