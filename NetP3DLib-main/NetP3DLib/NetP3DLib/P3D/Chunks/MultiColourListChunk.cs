using NetP3DLib.IO;
using NetP3DLib.P3D.Attributes;
using NetP3DLib.P3D.Collections;
using NetP3DLib.P3D.Enums;
using NetP3DLib.P3D.Exceptions;
using NetP3DLib.P3D.Extensions;
using System;
using System.Collections.Generic;
using System.Drawing;

namespace NetP3DLib.P3D.Chunks;

[ChunkAttributes(ChunkID)]
public class MultiColourListChunk : Chunk
{
    public const ChunkIdentifier ChunkID = ChunkIdentifier.Multi_Colour_List;

    public uint NumColours
    {
        get => (uint)(Colours?.Count ?? 0);
        set
        {
            if (value == NumColours)
                return;

            if (value < NumColours)
            {
                Colours.RemoveRange((int)value, (int)(NumColours - value));
            }
            else
            {
                int count = (int)(value - NumColours);
                var newColours = new Color[count];

                for (var i = 0; i < count; i++)
                    newColours[i] = default;

                Colours.AddRange(newColours);
            }
        }
    }
    
    private uint _channel;
    public uint Channel
    {
        get => _channel;
        set
        {
            if (_channel == value)
                return;
    
            _channel = value;
            OnPropertyChanged(nameof(Channel));
        }
    }
    
    public SizeAwareList<Color> Colours { get; }

    public override byte[] DataBytes
    {
        get
        {
            var data = new List<byte>((int)DataLength);

            data.AddRange(BitConverter.GetBytes(NumColours));
            data.AddRange(BitConverter.GetBytes(Channel));
            foreach (var colour in Colours)
                data.AddRange(BinaryExtensions.GetBytes(colour));

            return [.. data];
        }
    }
    public override uint DataLength => sizeof(uint) + sizeof(uint) + sizeof(uint) * NumColours;

    public MultiColourListChunk(EndianAwareBinaryReader br) : this(ReadChannel(br, out var num), br.ReadColorArray(num))
    {
    }

    private static uint ReadChannel(EndianAwareBinaryReader br, out int num)
    {
        num = br.ReadInt32();
        return br.ReadUInt32();
    }

    public MultiColourListChunk(uint channel, IList<Color> colours) : base(ChunkID)
    {
        _channel = channel;
        Colours = CreateSizeAwareList(colours, Colours_CollectionChanged);
    }
    
    private void Colours_CollectionChanged(object sender, System.Collections.Specialized.NotifyCollectionChangedEventArgs e) => OnPropertyChanged(nameof(Colours));

    public override IEnumerable<InvalidP3DException> ValidateChunk()
    {
        foreach (var error in base.ValidateChunk())
            yield return error;

        if (ParentChunk is OldPrimitiveGroupChunk oldPrimitiveGroup && oldPrimitiveGroup.NumVertices != NumColours)
            yield return new InvalidP3DException(this, $"Num Colours value {NumColours} does not match parent Num Vertices value {oldPrimitiveGroup.NumVertices}.");
    }

    protected override void WriteData(EndianAwareBinaryWriter bw)
    {
        bw.Write(NumColours);
        bw.Write(Channel);
        foreach (var colour in Colours)
            bw.Write(colour);
    }

    protected override Chunk CloneSelf() => new MultiColourListChunk(Channel, Colours);
}
