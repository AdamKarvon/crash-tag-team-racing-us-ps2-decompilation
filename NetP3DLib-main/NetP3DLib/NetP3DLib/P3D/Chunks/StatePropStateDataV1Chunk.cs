using NetP3DLib.IO;
using NetP3DLib.P3D.Attributes;
using NetP3DLib.P3D.Enums;
using NetP3DLib.P3D.Exceptions;
using NetP3DLib.P3D.Extensions;
using System;
using System.Collections.Generic;
using System.Linq;

namespace NetP3DLib.P3D.Chunks;

[ChunkAttributes(ChunkID)]
public class StatePropStateDataV1Chunk : NamedChunk
{
    public const ChunkIdentifier ChunkID = ChunkIdentifier.State_Prop_State_Data_V1;

    private uint _autoTransition;
    public bool AutoTransition
    {
        get => _autoTransition != 0;
        set
        {
            if (AutoTransition == value)
                return;

            _autoTransition = value ? 1u : 0u;
            OnPropertyChanged(nameof(AutoTransition));
        }
    }
    
    private uint _outState;
    public uint OutState
    {
        get => _outState;
        set
        {
            if (_outState == value)
                return;
    
            _outState = value;
            OnPropertyChanged(nameof(OutState));
        }
    }
    
    public uint NumDrawables => GetChildCount(ChunkIdentifier.State_Prop_Visibilities_Data);
    public uint NumFrameControllers => GetChildCount(ChunkIdentifier.State_Prop_Frame_Controller_Data);
    public uint NumEvents => GetChildCount(ChunkIdentifier.State_Prop_Event_Data);
    public uint NumCallbacks => GetChildCount(ChunkIdentifier.State_Prop_Callback_Data);
    private float _outFrame;
    public float OutFrame
    {
        get => _outFrame;
        set
        {
            if (_outFrame == value)
                return;
    
            _outFrame = value;
            OnPropertyChanged(nameof(OutFrame));
        }
    }
    

    public override byte[] DataBytes
    {
        get
        {
            var data = new List<byte>((int)DataLength);

            data.AddRange(BinaryExtensions.GetP3DStringBytes(Name));
            data.AddRange(BitConverter.GetBytes(_autoTransition));
            data.AddRange(BitConverter.GetBytes(OutState));
            data.AddRange(BitConverter.GetBytes(NumDrawables));
            data.AddRange(BitConverter.GetBytes(NumFrameControllers));
            data.AddRange(BitConverter.GetBytes(NumEvents));
            data.AddRange(BitConverter.GetBytes(NumCallbacks));
            data.AddRange(BitConverter.GetBytes(OutFrame));

            return [.. data];
        }
    }
    public override uint DataLength => BinaryExtensions.GetP3DStringLength(Name) + sizeof(uint) + sizeof(uint) + sizeof(uint) + sizeof(uint) + sizeof(uint) + sizeof(uint) + sizeof(float);

    [System.Diagnostics.CodeAnalysis.SuppressMessage("Style", "IDE0059:Unnecessary assignment of a value", Justification = "We want to read the value to progress the BinaryReader, but not set the value anywhere because it's calculated dynamically.")]
    public StatePropStateDataV1Chunk(EndianAwareBinaryReader br) : this(br.ReadP3DString(), br.ReadUInt32(), br.ReadUInt32(), br.SkipAndRead(sizeof(uint) + sizeof(uint) + sizeof(uint) + sizeof(uint), br.ReadSingle))
    {
    }

    public StatePropStateDataV1Chunk(string name, bool autoTransition, uint outState, float outFrame) : this(name, autoTransition ? 1u : 0u, outState, outFrame)
    {
    }

    public StatePropStateDataV1Chunk(string name, uint autoTransition, uint outState, float outFrame) : base(ChunkID, name)
    {
        _autoTransition = autoTransition;
        _outState = outState;
        _outFrame = outFrame;
    }

    public override IEnumerable<InvalidP3DException> ValidateChunk()
    {
        foreach (var error in base.ValidateChunk())
            yield return error;

        if (Children.Count == 0)
            yield return new InvalidP3DException(this, $"There must be at least one child chunk.");

        var currentIndex = 0;
        foreach (var child in Children)
        {
            var expectedIndex = ChunkSortPriority.IndexOf(child.ID);

            if (expectedIndex == -1)
                yield return new InvalidP3DException(this, $"Invalid child chunk: {child}.");

            if (expectedIndex < currentIndex)
                yield return new InvalidP3DException(this, $"Child chunk {child} is out of order. Expected order: {string.Join("; ", ChunkSortPriority.Select(x => $"{(ChunkIdentifier)x} (0x{x:X})"))}.");

            currentIndex = expectedIndex;
        }
    }

    protected override void WriteData(EndianAwareBinaryWriter bw)
    {
        bw.WriteP3DString(Name);
        bw.Write(_autoTransition);
        bw.Write(OutState);
        bw.Write(NumDrawables);
        bw.Write(NumFrameControllers);
        bw.Write(NumEvents);
        bw.Write(NumCallbacks);
        bw.Write(OutFrame);
    }

    protected override Chunk CloneSelf() => new StatePropStateDataV1Chunk(Name, AutoTransition, OutState, OutFrame);

    private static readonly List<uint> ChunkSortPriority = [
        (uint)ChunkIdentifier.State_Prop_Visibilities_Data,
        (uint)ChunkIdentifier.State_Prop_Frame_Controller_Data,
        (uint)ChunkIdentifier.State_Prop_Event_Data,
        (uint)ChunkIdentifier.State_Prop_Callback_Data,
    ];
    /// <summary>
    /// Children must be in the order: <c>Visibilities</c>; <c>Frame Controllers</c>; <c>Events</c>; <c>Callbacks</c>.
    /// <para>This function sorts child chunks into the correct order.</para>
    /// </summary>
    public void SortChildren()
    {
        List<Chunk> newChildren = new(Children.Count);

        foreach (uint id in ChunkSortPriority)
            foreach (var child in Children)
                if (child.ID == id)
                    newChildren.Add(child);

        foreach (var child in Children)
            if (!ChunkSortPriority.Contains(child.ID))
                newChildren.Add(child);

        Children.Clear();
        Children.AddRange(newChildren);
    }
}
