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
public class ColourListChunk : Chunk
{
    public const ChunkIdentifier ChunkID = ChunkIdentifier.Colour_List;

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
    public SizeAwareList<Color> Colours { get; }

    public override byte[] DataBytes
    {
        get
        {
            var data = new List<byte>((int)DataLength);

            data.AddRange(BitConverter.GetBytes(NumColours));
            foreach (var colour in Colours)
                data.AddRange(BinaryExtensions.GetBytes(colour));

            return [.. data];
        }
    }
    public override uint DataLength => sizeof(uint) + sizeof(uint) * NumColours;

    public ColourListChunk(EndianAwareBinaryReader br) : this(br.ReadColorArray(out _))
    {
    }

    public ColourListChunk(IList<Color> colours) : base(ChunkID)
    {
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
        foreach (var colour in Colours)
            bw.Write(colour);
    }

    protected override Chunk CloneSelf() => new ColourListChunk(Colours);
}
