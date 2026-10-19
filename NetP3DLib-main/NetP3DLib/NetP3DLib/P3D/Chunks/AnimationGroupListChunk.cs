using NetP3DLib.IO;
using NetP3DLib.P3D.Attributes;
using NetP3DLib.P3D.Enums;
using NetP3DLib.P3D.Exceptions;
using System;
using System.Collections.Generic;
using System.ComponentModel;

namespace NetP3DLib.P3D.Chunks;

[ChunkAttributes(ChunkID)]
public class AnimationGroupListChunk : Chunk
{
    public const ChunkIdentifier ChunkID = ChunkIdentifier.Animation_Group_List;

    private uint _version;
    [DefaultValue(0)]
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
    
    public uint NumGroups => GetChildCount(ChunkIdentifier.Animation_Group);

    public override byte[] DataBytes
    {
        get
        {
            var data = new List<byte>((int)DataLength);

            data.AddRange(BitConverter.GetBytes(Version));
            data.AddRange(BitConverter.GetBytes(NumGroups));

            return [.. data];
        }
    }
    public override uint DataLength => sizeof(uint) + sizeof(uint);

    [System.Diagnostics.CodeAnalysis.SuppressMessage("Style", "IDE0059:Unnecessary assignment of a value", Justification = "We want to read the value to progress the BinaryReader, but not set the value anywhere because it's calculated dynamically.")]
    public AnimationGroupListChunk(EndianAwareBinaryReader br) : this(br.ReadUInt32())
    {
        var numGroups = br.ReadUInt32();
    }

    public AnimationGroupListChunk(uint version) : base(ChunkID)
    {
        _version = version;
    }

    public override IEnumerable<InvalidP3DException> ValidateChunk()
    {
        foreach (var error in base.ValidateChunk())
            yield return error;

        if (NumGroups != Children.Count)
            yield return new InvalidP3DException(this, $"Invalid children. Must be only {nameof(AnimationGroupChunk)}.");
    }

    protected override void WriteData(EndianAwareBinaryWriter bw)
    {
        bw.Write(Version);
        bw.Write(NumGroups);
    }

    protected override Chunk CloneSelf() => new AnimationGroupListChunk(Version);
}
