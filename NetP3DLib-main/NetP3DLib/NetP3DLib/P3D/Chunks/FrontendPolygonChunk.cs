using NetP3DLib.IO;
using NetP3DLib.P3D.Attributes;
using NetP3DLib.P3D.Collections;
using NetP3DLib.P3D.Enums;
using NetP3DLib.P3D.Exceptions;
using NetP3DLib.P3D.Extensions;
using System;
using System.Collections.Generic;
using System.ComponentModel;
using System.Drawing;
using System.Numerics;

namespace NetP3DLib.P3D.Chunks;

[ChunkAttributes(ChunkID)]
public class FrontendPolygonChunk : NamedChunk
{
    public const ChunkIdentifier ChunkID = ChunkIdentifier.Frontend_Polygon;

    private uint _version;
    [DefaultValue(1)]
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
    
    private uint _translucency;
    public uint Translucency
    {
        get => _translucency;
        set
        {
            if (_translucency == value)
                return;
    
            _translucency = value;
            OnPropertyChanged(nameof(Translucency));
        }
    }
    
    public uint NumPoints
    {
        get => (uint)(Points?.Count ?? 0);
        set
        {
            if (value == NumPoints)
                return;

            if (value < NumPoints)
            {
                Points.RemoveRange((int)value, (int)(NumPoints - value));
            }
            else
            {
                int count = (int)(value - NumPoints);
                var newPoints = new Vector3[count];

                for (var i = 0; i < count; i++)
                    newPoints[i] = default;

                Points.AddRange(newPoints);
            }
            NumColours = value;
        }
    }
    public SizeAwareList<Vector3> Points { get; }
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
            NumPoints = value;
        }
    }
    public SizeAwareList<Color> Colours { get; }

    public override byte[] DataBytes
    {
        get
        {
            var data = new List<byte>((int)DataLength);

            data.AddRange(BinaryExtensions.GetP3DStringBytes(Name));
            data.AddRange(BitConverter.GetBytes(Version));
            data.AddRange(BitConverter.GetBytes(Translucency));
            data.AddRange(BitConverter.GetBytes(NumPoints));
            foreach (var point in Points)
                data.AddRange(BinaryExtensions.GetBytes(point));
            foreach (var colour in Colours)
                data.AddRange(BinaryExtensions.GetBytes(colour));

            return [.. data];
        }
    }
    public override uint DataLength => BinaryExtensions.GetP3DStringLength(Name) + sizeof(uint) + sizeof(uint) + sizeof(uint) + sizeof(uint) * 3 * NumPoints + sizeof(uint) * NumColours;

    public FrontendPolygonChunk(EndianAwareBinaryReader br) : this(br.ReadP3DString(), br.ReadUInt32(), br.ReadUInt32(), br.ReadVector3Array(out var num), br.ReadColorArray(num))
    {
    }

    public FrontendPolygonChunk(string name, uint version, uint translucency, IList<Vector3> points, IList<Color> colours) : base(ChunkID, name)
    {
        _version = version;
        _translucency = translucency;
        Points = CreateSizeAwareList(points, Points_CollectionChanged);
        Colours = CreateSizeAwareList(colours, Colours_CollectionChanged);
    }
    
    private void Points_CollectionChanged(object sender, System.Collections.Specialized.NotifyCollectionChangedEventArgs e) => OnPropertyChanged(nameof(Points));
    
    private void Colours_CollectionChanged(object sender, System.Collections.Specialized.NotifyCollectionChangedEventArgs e) => OnPropertyChanged(nameof(Colours));

    public override IEnumerable<InvalidP3DException> ValidateChunk()
    {
        foreach (var error in base.ValidateChunk())
            yield return error;

        if (Points.Count != Colours.Count)
            yield return new InvalidP3DException(this, $"{nameof(Points)} and {nameof(Colours)} must have equal counts.");
    }

    protected override void WriteData(EndianAwareBinaryWriter bw)
    {
        bw.WriteP3DString(Name);
        bw.Write(Version);
        bw.Write(Translucency);
        bw.Write(NumPoints);
        foreach (var point in Points)
            bw.Write(point);
        foreach (var colour in Colours)
            bw.Write(colour);
    }

    protected override Chunk CloneSelf() => new FrontendPolygonChunk(Name, Version, Translucency, Points, Colours);
}
