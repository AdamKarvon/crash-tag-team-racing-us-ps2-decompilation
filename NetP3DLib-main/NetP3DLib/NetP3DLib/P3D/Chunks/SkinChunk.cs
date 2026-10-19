using NetP3DLib.IO;
using NetP3DLib.P3D.Attributes;
using NetP3DLib.P3D.Enums;
using NetP3DLib.P3D.Exceptions;
using NetP3DLib.P3D.Extensions;
using NetP3DLib.P3D.Types;
using System;
using System.Collections.Generic;
using System.ComponentModel;

namespace NetP3DLib.P3D.Chunks;

[ChunkAttributes(ChunkID)]
public class SkinChunk : NamedChunk
{
    public const ChunkIdentifier ChunkID = ChunkIdentifier.Skin;

    private uint _version;
    [DefaultValue(4)]
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
    public uint NumOldPrimitiveGroups => GetChildCount(ChunkIdentifier.Old_Primitive_Group);
    public uint NumPrimitiveGroups => GetChildCount(ChunkIdentifier.Primitive_Group);

    public override byte[] DataBytes
    {
        get
        {
            var data = new List<byte>((int)DataLength);

            data.AddRange(BinaryExtensions.GetP3DStringBytes(Name));
            data.AddRange(BitConverter.GetBytes(Version));
            data.AddRange(BinaryExtensions.GetP3DStringBytes(SkeletonName));
            data.AddRange(BitConverter.GetBytes(NumOldPrimitiveGroups + NumPrimitiveGroups));

            return [.. data];
        }
    }
    public override uint DataLength => BinaryExtensions.GetP3DStringLength(Name) + sizeof(uint) + BinaryExtensions.GetP3DStringLength(SkeletonName) + sizeof(uint);

    [System.Diagnostics.CodeAnalysis.SuppressMessage("Style", "IDE0059:Unnecessary assignment of a value", Justification = "We want to read the value to progress the BinaryReader, but not set the value anywhere because it's calculated dynamically.")]
    public SkinChunk(EndianAwareBinaryReader br) : this(br.ReadP3DString(), br.ReadUInt32(), br.ReadP3DString())
    {
        var numPrimitiveGroups = br.ReadUInt32();
    }

    public SkinChunk(string name, uint version, string skeletonName) : base(ChunkID, name)
    {
        _version = version;
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
        bw.Write(Version);
        bw.WriteP3DString(SkeletonName);
        bw.Write(NumOldPrimitiveGroups + NumPrimitiveGroups);
    }

    protected override Chunk CloneSelf() => new SkinChunk(Name, Version, SkeletonName);
}
