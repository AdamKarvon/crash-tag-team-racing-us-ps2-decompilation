using NetP3DLib.IO;
using NetP3DLib.P3D.Attributes;
using NetP3DLib.P3D.Enums;
using NetP3DLib.P3D.Exceptions;
using NetP3DLib.P3D.Extensions;
using System;
using System.Collections.Generic;

namespace NetP3DLib.P3D.Chunks;

[ChunkAttributes(ChunkID)]
public class CompositeDrawableSkinChunk : NamedChunk
{
    public const ChunkIdentifier ChunkID = ChunkIdentifier.Composite_Drawable_Skin;

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

    public override byte[] DataBytes
    {
        get
        {
            var data = new List<byte>((int)DataLength);

            data.AddRange(BinaryExtensions.GetP3DStringBytes(Name));
            data.AddRange(BitConverter.GetBytes(_isTranslucent));

            return [.. data];
        }
    }
    public override uint DataLength => BinaryExtensions.GetP3DStringLength(Name) + sizeof(uint);

    public CompositeDrawableSkinChunk(EndianAwareBinaryReader br) : this(br.ReadP3DString(), br.ReadUInt32())
    {
    }

    public CompositeDrawableSkinChunk(string name, bool isTranslucent) : this(name, isTranslucent ? 1u : 0u)
    {
    }

    public CompositeDrawableSkinChunk(string name, uint isTranslucent) : base(ChunkID, name)
    {
        _isTranslucent = isTranslucent;
    }

    public override IEnumerable<InvalidP3DException> ValidateChunk()
    {
        foreach (var error in base.ValidateChunk())
            yield return error;

        if (ParentChunk != null || ParentFile != null)
        {
            var compositeDrawableChunk = (ParentChunk as CompositeDrawableSkinListChunk)?.ParentChunk as CompositeDrawableChunk;

            var skinChunk = FindNamedChunkInParentHierarchy<SkinChunk>(Name);
            if (skinChunk == null)
            {
                yield return new InvalidP3DException(this, $"Could not find skin with name \"{Name}\" in the parent hierarchy.");
            }
            else if (compositeDrawableChunk != null && skinChunk.SkeletonName != compositeDrawableChunk.SkeletonName)
            {
                yield return new InvalidP3DException(this, $"The skin's skeleton \"{skinChunk.SkeletonName}\" does not match composite drawable's \"{compositeDrawableChunk.SkeletonName}\".");
            }
        }
    }

    protected override void WriteData(EndianAwareBinaryWriter bw)
    {
        bw.WriteP3DString(Name);
        bw.Write(_isTranslucent);
    }

    protected override Chunk CloneSelf() => new CompositeDrawableSkinChunk(Name, IsTranslucent);
}