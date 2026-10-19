using NetP3DLib.IO;
using NetP3DLib.P3D.Attributes;
using NetP3DLib.P3D.Enums;
using NetP3DLib.P3D.Exceptions;
using NetP3DLib.P3D.Extensions;
using NetP3DLib.P3D.Types;
using System;
using System.Collections.Generic;

namespace NetP3DLib.P3D.Chunks;

[ChunkAttributes(ChunkID)]
public class CompositeDrawable2Chunk : NamedChunk
{
    public const ChunkIdentifier ChunkID = ChunkIdentifier.Composite_Drawable_2;

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
    
    private readonly P3DString _skeletonName;
    public string SkeletonName
    {
        get => _skeletonName?.Value ?? string.Empty;
        set => _skeletonName.Value = value;
    }
    public uint NumPrimitives => GetChildCount(ChunkIdentifier.Composite_Drawable_Primitive);

    public override byte[] DataBytes
    {
        get
        {
            var data = new List<byte>((int)DataLength);

            data.AddRange(BitConverter.GetBytes(Version));
            data.AddRange(BinaryExtensions.GetP3DStringBytes(Name));
            data.AddRange(BinaryExtensions.GetP3DStringBytes(SkeletonName));
            data.AddRange(BitConverter.GetBytes(NumPrimitives));

            return [.. data];
        }
    }
    public override uint DataLength => sizeof(uint) + BinaryExtensions.GetP3DStringLength(Name) + BinaryExtensions.GetP3DStringLength(SkeletonName) + sizeof(uint);

    [System.Diagnostics.CodeAnalysis.SuppressMessage("Style", "IDE0059:Unnecessary assignment of a value", Justification = "We want to read the value to progress the BinaryReader, but not set the value anywhere because it's calculated dynamically.")]
    public CompositeDrawable2Chunk(EndianAwareBinaryReader br) : this(br.ReadUInt32(), br.ReadP3DString(), br.ReadP3DString())
    {
        var numPrimitives = br.ReadUInt32();
    }

    public CompositeDrawable2Chunk(uint version, string name, string skeletonName) : base(ChunkID, name)
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
    }

    protected override void WriteData(EndianAwareBinaryWriter bw)
    {
        bw.Write(Version);
        bw.WriteP3DString(Name);
        bw.WriteP3DString(SkeletonName);
        bw.Write(NumPrimitives);
    }

    protected override Chunk CloneSelf() => new CompositeDrawable2Chunk(Version, Name, SkeletonName);
}
