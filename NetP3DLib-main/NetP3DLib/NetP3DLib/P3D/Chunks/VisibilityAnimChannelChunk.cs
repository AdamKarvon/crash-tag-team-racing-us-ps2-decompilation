using NetP3DLib.IO;
using NetP3DLib.P3D.Attributes;
using NetP3DLib.P3D.Collections;
using NetP3DLib.P3D.Enums;
using NetP3DLib.P3D.Extensions;
using System;
using System.Collections.Generic;

namespace NetP3DLib.P3D.Chunks;

[ChunkAttributes(ChunkID)]
public class VisibilityAnimChannelChunk : NamedChunk
{
    public const ChunkIdentifier ChunkID = ChunkIdentifier.Visibility_Anim_Channel;

    private ushort _startState;
    public ushort StartState
    {
        get => _startState;
        set
        {
            if (_startState == value)
                return;
    
            _startState = value;
            OnPropertyChanged(nameof(StartState));
        }
    }
    
    public uint NumFrames
    {
        get => (uint)(Frames?.Count ?? 0);
        set
        {
            if (value == NumFrames)
                return;

            if (value < NumFrames)
            {
                Frames.RemoveRange((int)value, (int)(NumFrames - value));
            }
            else
            {
                int count = (int)(value - NumFrames);
                var newFrames = new uint[count];

                for (var i = 0; i < count; i++)
                    newFrames[i] = default;

                Frames.AddRange(newFrames);
            }
        }
    }
    public SizeAwareList<uint> Frames { get; }

    public override byte[] DataBytes
    {
        get
        {
            var data = new List<byte>((int)DataLength);

            data.AddRange(BinaryExtensions.GetP3DStringBytes(Name));
            data.AddRange(BitConverter.GetBytes(StartState));
            data.AddRange(BitConverter.GetBytes(NumFrames));
            foreach (var value in Frames)
                data.AddRange(BitConverter.GetBytes(value));

            return [.. data];
        }
    }
    public override uint DataLength => BinaryExtensions.GetP3DStringLength(Name) + sizeof(ushort) + sizeof(uint) + sizeof(uint) * NumFrames;

    public VisibilityAnimChannelChunk(EndianAwareBinaryReader br) : this(br.ReadP3DString(), br.ReadUInt16(), br.ReadUInt32Array(out _))
    {
    }

    public VisibilityAnimChannelChunk(string name, ushort startState, IList<uint> frames) : base(ChunkID, name)
    {
        _startState = startState;
        Frames = CreateSizeAwareList(frames, Frames_CollectionChanged);
    }
    
    private void Frames_CollectionChanged(object sender, System.Collections.Specialized.NotifyCollectionChangedEventArgs e) => OnPropertyChanged(nameof(Frames));

    protected override void WriteData(EndianAwareBinaryWriter bw)
    {
        bw.WriteP3DString(Name);
        bw.Write(StartState);
        bw.Write(NumFrames);
        foreach (var value in Frames)
            bw.Write(value);
    }

    protected override Chunk CloneSelf() => new VisibilityAnimChannelChunk(Name, StartState, Frames);
}
