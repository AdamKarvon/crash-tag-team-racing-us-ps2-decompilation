using NetP3DLib.IO;
using NetP3DLib.P3D.Attributes;
using NetP3DLib.P3D.Enums;
using NetP3DLib.P3D.Exceptions;
using NetP3DLib.P3D.Extensions;
using System;
using System.Collections.Generic;
using System.ComponentModel;

namespace NetP3DLib.P3D.Chunks;

[ChunkAttributes(ChunkID)]
public class AnimationGroupChunk : NamedChunk
{
    public const ChunkIdentifier ChunkID = ChunkIdentifier.Animation_Group;
    private const uint BaseSize = 40;
    private static readonly HashSet<ChunkIdentifier> ChannelChunkIDs = [
        ChunkIdentifier.Integer_Channel,
        ChunkIdentifier.Float_1_Channel,
        ChunkIdentifier.Float_2_Channel,
        ChunkIdentifier.Vector_1D_OF_Channel,
        ChunkIdentifier.Vector_2D_OF_Channel,
        ChunkIdentifier.Vector_3D_OF_Channel,
        ChunkIdentifier.Quaternion_Channel,
        ChunkIdentifier.Compressed_Quaternion_Channel,
        //ChunkIdentifier.String_Channel,
        ChunkIdentifier.Entity_Channel,
        ChunkIdentifier.Boolean_Channel,
        ChunkIdentifier.Colour_Channel,
        //ChunkIdentifier.Event_Channel,
    ];

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
    
    private uint _groupID;
    public uint GroupID
    {
        get => _groupID;
        set
        {
            if (_groupID == value)
                return;
    
            _groupID = value;
            OnPropertyChanged(nameof(GroupID));
        }
    }
    
    public uint NumChannels
    {
        get
        {
            var channelCount = 0u;

            foreach (var child in Children)
                if (ChannelChunkIDs.Contains((ChunkIdentifier)child.ID))
                    channelCount++;

            return channelCount;
        }
    }

    public override byte[] DataBytes
    {
        get
        {
            var data = new List<byte>((int)DataLength);

            data.AddRange(BitConverter.GetBytes(Version));
            data.AddRange(BinaryExtensions.GetP3DStringBytes(Name));
            data.AddRange(BitConverter.GetBytes(GroupID));
            data.AddRange(BitConverter.GetBytes(NumChannels));

            return [.. data];
        }
    }
    public override uint DataLength => sizeof(uint) + BinaryExtensions.GetP3DStringLength(Name) + sizeof(uint) + sizeof(uint);

    [System.Diagnostics.CodeAnalysis.SuppressMessage("Style", "IDE0059:Unnecessary assignment of a value", Justification = "We want to read the value to progress the BinaryReader, but not set the value anywhere because it's calculated dynamically.")]
    public AnimationGroupChunk(EndianAwareBinaryReader br) : this(br.ReadUInt32(), br.ReadP3DString(), br.ReadUInt32())
    {
        var numChannels = br.ReadUInt32();
    }

    public AnimationGroupChunk(uint version, string name, uint groupID) : base(ChunkID, name)
    {
        _version = version;
        _groupID = groupID;
    }

    public override IEnumerable<InvalidP3DException> ValidateChunk()
    {
        foreach (var error in base.ValidateChunk())
            yield return error;

        if (NumChannels != Children.Count)
            yield return new InvalidP3DException(this, $"Invalid children. Must only contain channels.");
    }

    protected override void WriteData(EndianAwareBinaryWriter bw)
    {
        bw.Write(Version);
        bw.WriteP3DString(Name);
        bw.Write(GroupID);
        bw.Write(NumChannels);
    }

    protected override Chunk CloneSelf() => new AnimationGroupChunk(Version, Name, GroupID);

    internal uint CalculateMemorySize(AnimationChunk.Platform platform, uint size)
    {
        size = ((size + 7u) & ~7u) + BaseSize;
        size = ((size + 3u) & ~3u) + NumChannels * 4;

        foreach (var intChannel in GetChunksOfType<IntegerChannelChunk>())
            size = intChannel.CalculateMemorySize(platform, size);

        foreach (var float1Channel in GetChunksOfType<Float1ChannelChunk>())
            size = float1Channel.CalculateMemorySize(platform, size);

        foreach (var float2Channel in GetChunksOfType<Float2ChannelChunk>())
            size = float2Channel.CalculateMemorySize(platform, size);

        foreach (var vector1DOFChannel in GetChunksOfType<Vector1DOFChannelChunk>())
            size = vector1DOFChannel.CalculateMemorySize(platform, size);

        foreach (var vector2DOFChannel in GetChunksOfType<Vector2DOFChannelChunk>())
            size = vector2DOFChannel.CalculateMemorySize(platform, size);

        foreach (var vector3DOFChannel in GetChunksOfType<Vector3DOFChannelChunk>())
            size = vector3DOFChannel.CalculateMemorySize(platform, size);

        foreach (var compressedQuaternionChannel in GetChunksOfType<CompressedQuaternionChannelChunk>())
            size = compressedQuaternionChannel.CalculateMemorySize(platform, size);

        foreach (var quaternionChannel in GetChunksOfType<QuaternionChannelChunk>())
            size = quaternionChannel.CalculateMemorySize(platform, size);

        foreach (var entityChannel in GetChunksOfType<EntityChannelChunk>())
            size = entityChannel.CalculateMemorySize(platform, size);

        foreach (var booleanChannel in GetChunksOfType<BooleanChannelChunk>())
            size = booleanChannel.CalculateMemorySize(platform, size);

        foreach (var colourChannel in GetChunksOfType<ColourChannelChunk>())
            size = colourChannel.CalculateMemorySize(platform, size);

        /*foreach (var eventChannel in GetChunksOfType<EventChannelChunk>())
            size = eventChannel.CalculateMemorySize(platform, size);*/

        return size;
    }
}
