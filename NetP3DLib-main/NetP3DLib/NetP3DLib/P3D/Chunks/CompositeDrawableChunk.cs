using NetP3DLib.IO;
using NetP3DLib.P3D.Attributes;
using NetP3DLib.P3D.Enums;
using NetP3DLib.P3D.Exceptions;
using NetP3DLib.P3D.Extensions;
using NetP3DLib.P3D.Types;
using System.Collections.Generic;

namespace NetP3DLib.P3D.Chunks;

[ChunkAttributes(ChunkID)]
public class CompositeDrawableChunk : NamedChunk
{
    public const ChunkIdentifier ChunkID = ChunkIdentifier.Composite_Drawable;

    private readonly P3DString _skeletonName;
    public string SkeletonName
    {
        get => _skeletonName?.Value ?? string.Empty;
        set => _skeletonName.Value = value;
    }

    public override byte[] DataBytes
    {
        get
        {
            var data = new List<byte>((int)DataLength);

            data.AddRange(BinaryExtensions.GetP3DStringBytes(Name));
            data.AddRange(BinaryExtensions.GetP3DStringBytes(SkeletonName));

            return [.. data];
        }
    }
    public override uint DataLength => BinaryExtensions.GetP3DStringLength(Name) + BinaryExtensions.GetP3DStringLength(SkeletonName);

    public CompositeDrawableChunk(EndianAwareBinaryReader br) : this(br.ReadP3DString(), br.ReadP3DString())
    {
    }

    public CompositeDrawableChunk(string name, string skeletonName) : base(ChunkID, name)
    {
        _skeletonName = new(this, skeletonName, nameof(SkeletonName));
    }

    public override IEnumerable<InvalidP3DException> ValidateChunk()
    {
        foreach (var error in base.ValidateChunk())
            yield return error;

        if (!SkeletonName.IsValidP3DString())
            yield return new InvalidP3DStringException(this, nameof(SkeletonName), SkeletonName);

        if ((ParentChunk != null || ParentFile != null) && FindNamedChunkInParentHierarchy<SkeletonChunk>(SkeletonName) == null && FindNamedChunkInParentHierarchy<Skeleton2Chunk>(SkeletonName) == null)
            yield return new InvalidP3DException(this, $"Could not find skeleton with name \"{SkeletonName}\" in the parent hierarchy.");
    }

    protected override void WriteData(EndianAwareBinaryWriter bw)
    {
        bw.WriteP3DString(Name);
        bw.WriteP3DString(SkeletonName);
    }

    protected override Chunk CloneSelf() => new CompositeDrawableChunk(Name, SkeletonName);
}