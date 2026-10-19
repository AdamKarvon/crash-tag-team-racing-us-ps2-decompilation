using NetP3DLib.IO;
using NetP3DLib.P3D.Attributes;
using NetP3DLib.P3D.Enums;
using NetP3DLib.P3D.Exceptions;
using NetP3DLib.P3D.Extensions;
using System;
using System.Collections.Generic;

namespace NetP3DLib.P3D.Chunks;

[ChunkAttributes(ChunkID)]
public class CompositeDrawableEffectChunk : NamedChunk
{
    public const ChunkIdentifier ChunkID = ChunkIdentifier.Composite_Drawable_Effect;

    private uint _isTranslucent;
    public bool IsTranslucent
    {
        get => _isTranslucent != 0;
        set
        {
            if (IsTranslucent == value)
                return;

            _isTranslucent = value ? 1u : 0u;
            OnPropertyChanged(nameof(IsTranslucent));
        }
    }
    
    private uint _skeletonJointId;
    public uint SkeletonJointId
    {
        get => _skeletonJointId;
        set
        {
            if (_skeletonJointId == value)
                return;
    
            _skeletonJointId = value;
            OnPropertyChanged(nameof(SkeletonJointId));
        }
    }
    

    public override byte[] DataBytes
    {
        get
        {
            var data = new List<byte>((int)DataLength);

            data.AddRange(BinaryExtensions.GetP3DStringBytes(Name));
            data.AddRange(BitConverter.GetBytes(_isTranslucent));
            data.AddRange(BitConverter.GetBytes(SkeletonJointId));

            return [.. data];
        }
    }
    public override uint DataLength => BinaryExtensions.GetP3DStringLength(Name) + sizeof(uint) + sizeof(uint);

    public CompositeDrawableEffectChunk(EndianAwareBinaryReader br) : this(br.ReadP3DString(), br.ReadUInt32(), br.ReadUInt32())
    {
    }

    public CompositeDrawableEffectChunk(string name, bool isTranslucent, uint skeletonJointId) : this(name, isTranslucent ? 1u : 0u, skeletonJointId)
    {
    }

    public CompositeDrawableEffectChunk(string name, uint isTranslucent, uint skeletonJointId) : base(ChunkID, name)
    {
        _isTranslucent = isTranslucent;
        _skeletonJointId = skeletonJointId;
    }

    public override IEnumerable<InvalidP3DException> ValidateChunk()
    {
        foreach (var error in base.ValidateChunk())
            yield return error;

        if (ParentChunk != null || ParentFile != null)
        {
            var compositeDrawableChunk = (ParentChunk as CompositeDrawableEffectListChunk)?.ParentChunk as CompositeDrawableChunk;

            var particleSystemChunk = FindNamedChunkInParentHierarchy<ParticleSystemChunk>(Name);
            if (particleSystemChunk == null)
                yield return new InvalidP3DException(this, $"Could not find particle system with name \"{Name}\" in the parent hierarchy.");
            
            if (compositeDrawableChunk != null)
            {
                var skeletonChunk = compositeDrawableChunk.FindNamedChunkInParentHierarchy<SkeletonChunk>(compositeDrawableChunk.SkeletonName);
                if (skeletonChunk != null && SkeletonJointId >= skeletonChunk.NumJoints)
                    yield return new InvalidP3DException(this, $"The {nameof(SkeletonJointId)} {SkeletonJointId} exceeds the number of joints in skeleton \"{compositeDrawableChunk.SkeletonName}\".");
            }
        }
    }

    protected override void WriteData(EndianAwareBinaryWriter bw)
    {
        bw.WriteP3DString(Name);
        bw.Write(_isTranslucent);
        bw.Write(SkeletonJointId);
    }

    protected override Chunk CloneSelf() => new CompositeDrawableEffectChunk(Name, IsTranslucent, SkeletonJointId);
}
