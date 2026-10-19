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
public class RoadChunk : NamedChunk
{
    public const ChunkIdentifier ChunkID = ChunkIdentifier.Road;
    private const uint SpeedMask = 0x000000FF;
    private const uint IntelligenceMask = 0x0000FF00;
    private const uint ShortcutMask = 0x00010000;

    private uint _type;
    public uint Type
    {
        get => _type;
        set
        {
            if (_type == value)
                return;
    
            _type = value;
            OnPropertyChanged(nameof(Type));
        }
    }
    
    private readonly P3DString _startIntersection;
    public string StartIntersection
    {
        get => _startIntersection?.Value ?? string.Empty;
        set => _startIntersection.Value = value;
    }
    private readonly P3DString _endIntersection;
    public string EndIntersection
    {
        get => _endIntersection?.Value ?? string.Empty;
        set => _endIntersection.Value = value;
    }
    
    private uint _maximumCars;
    public uint MaximumCars
    {
        get => _maximumCars;
        set
        {
            if (_maximumCars == value)
                return;
    
            _maximumCars = value;
            OnPropertyChanged(nameof(MaximumCars));
        }
    }
    
    private uint _bitmask;
    public byte Speed
    {
        get => (byte)(_bitmask & SpeedMask);
        set
        {
            if (Speed == value)
                return;

            _bitmask &= ~SpeedMask;
            _bitmask |= value;
            OnPropertyChanged(nameof(Speed));
        }
    }
    public byte Intelligence
    {
        get => (byte)((_bitmask & IntelligenceMask) >> 8);
        set
        {
            if (Intelligence == value)
                return;

            _bitmask &= ~IntelligenceMask;
            _bitmask |= (uint)(value << 8);
            OnPropertyChanged(nameof(Intelligence));
        }
    }
    public bool Shortcut
    {
        get => (_bitmask & ShortcutMask) != 0;
        set
        {
            if (Shortcut == value)
                return;

            if (value)
                _bitmask |= ShortcutMask;
            else
                _bitmask &= ~ShortcutMask;
            OnPropertyChanged(nameof(Shortcut));
        }
    }

    public override byte[] DataBytes
    {
        get
        {
            var data = new List<byte>((int)DataLength);

            data.AddRange(BinaryExtensions.GetP3DStringBytes(Name));
            data.AddRange(BitConverter.GetBytes(Type));
            data.AddRange(BinaryExtensions.GetP3DStringBytes(StartIntersection));
            data.AddRange(BinaryExtensions.GetP3DStringBytes(EndIntersection));
            data.AddRange(BitConverter.GetBytes(MaximumCars));
            data.AddRange(BitConverter.GetBytes(_bitmask));

            return [.. data];
        }
    }
    public override uint DataLength => BinaryExtensions.GetP3DStringLength(Name) + sizeof(uint) + BinaryExtensions.GetP3DStringLength(StartIntersection) + BinaryExtensions.GetP3DStringLength(EndIntersection) + sizeof(uint) + sizeof(byte) + sizeof(byte) + sizeof(byte) + sizeof(byte);

    [System.Diagnostics.CodeAnalysis.SuppressMessage("Style", "IDE0059:Unnecessary assignment of a value", Justification = "We want to read the value to progress the BinaryReader, but not set the value anywhere because it's calculated dynamically.")]
    public RoadChunk(EndianAwareBinaryReader br) : this(br.ReadP3DString(), br.ReadUInt32(), br.ReadP3DString(), br.ReadP3DString(), br.ReadUInt32(), br.ReadUInt32())
    {
    }

    public RoadChunk(string name, uint type, string startIntersection, string endIntersection, uint maximumCars, byte speed, byte intelligence, bool shortcut) : this(name, type, startIntersection, endIntersection, maximumCars, 0u)
    {
        Speed = speed;
        Intelligence = intelligence;
        Shortcut = shortcut;
    }

    public RoadChunk(string name, uint type, string startIntersection, string endIntersection, uint maximumCars, uint bitmask) : base(ChunkID, name)
    {
        _type = type;
        _startIntersection = new(this, startIntersection, nameof(StartIntersection));
        _endIntersection = new(this, endIntersection, nameof(EndIntersection));
        _maximumCars = maximumCars;
        _bitmask = bitmask;
    }

    public override IEnumerable<InvalidP3DException> ValidateChunk()
    {
        foreach (var error in base.ValidateChunk())
            yield return error;

        if (!StartIntersection.IsValidP3DString())
            yield return new InvalidP3DStringException(this, nameof(StartIntersection), StartIntersection);

        if (!EndIntersection.IsValidP3DString())
            yield return new InvalidP3DStringException(this, nameof(EndIntersection), EndIntersection);

        if (Children.Count == 0)
            yield return new InvalidP3DException(this, $"There must be at least one Road Segment child chunk.");
        foreach (var child in Children)
            if (child.ID != (uint)ChunkIdentifier.Road_Segment)
                yield return new InvalidP3DException(this, $"Child chunk {child} is invalid. Child chunks must be an instance of Road Segment.");
    }

    protected override void WriteData(EndianAwareBinaryWriter bw)
    {
        bw.WriteP3DString(Name);
        bw.Write(Type);
        bw.WriteP3DString(StartIntersection);
        bw.WriteP3DString(EndIntersection);
        bw.Write(MaximumCars);
        bw.Write(_bitmask);
    }

    protected override Chunk CloneSelf() => new RoadChunk(Name, Type, StartIntersection, EndIntersection, MaximumCars, Speed, Intelligence, Shortcut);
}
