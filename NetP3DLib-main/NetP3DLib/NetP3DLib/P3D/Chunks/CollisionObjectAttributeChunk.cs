using NetP3DLib.IO;
using NetP3DLib.P3D.Attributes;
using NetP3DLib.P3D.Enums;
using System;
using System.Collections.Generic;

namespace NetP3DLib.P3D.Chunks;

[ChunkAttributes(ChunkID)]
public class CollisionObjectAttributeChunk : Chunk
{
    public const ChunkIdentifier ChunkID = ChunkIdentifier.Collision_Object_Attribute;

    private ushort _isStatic;
    public bool IsStatic
    {
        get => _isStatic != 0;
        set
        {
            if (IsStatic == value)
                return;

            _isStatic = (ushort)(value ? 1 : 0);
            OnPropertyChanged(nameof(IsStatic));
        }
    }
    
    private uint _defaultArea;
    public uint DefaultArea
    {
        get => _defaultArea;
        set
        {
            if (_defaultArea == value)
                return;
    
            _defaultArea = value;
            OnPropertyChanged(nameof(DefaultArea));
        }
    }
    
    private ushort _canRoll;
    public bool CanRoll
    {
        get => _canRoll != 0;
        set
        {
            if (CanRoll == value)
                return;

            _canRoll = (ushort)(value ? 1 : 0);
            OnPropertyChanged(nameof(CanRoll));
        }
    }
    private ushort _canSlide;
    public bool CanSlide
    {
        get => _canSlide != 0;
        set
        {
            if (CanSlide == value)
                return;

            _canSlide = (ushort)(value ? 1 : 0);
            OnPropertyChanged(nameof(CanSlide));
        }
    }
    private ushort _canSpin;
    public bool CanSpin
    {
        get => _canSpin != 0;
        set
        {
            if (CanSpin == value)
                return;

            _canSpin = (ushort)(value ? 1 : 0);
            OnPropertyChanged(nameof(CanSpin));
        }
    }
    private ushort _canBounce;
    public bool CanBounce
    {
        get => _canBounce != 0;
        set
        {
            if (CanBounce == value)
                return;

            _canBounce = (ushort)(value ? 1 : 0);
            OnPropertyChanged(nameof(CanBounce));
        }
    }
    
    private uint _extraAttribute1;
    public uint ExtraAttribute1
    {
        get => _extraAttribute1;
        set
        {
            if (_extraAttribute1 == value)
                return;
    
            _extraAttribute1 = value;
            OnPropertyChanged(nameof(ExtraAttribute1));
        }
    }
    
    private uint _extraAttribute2;
    public uint ExtraAttribute2
    {
        get => _extraAttribute2;
        set
        {
            if (_extraAttribute2 == value)
                return;
    
            _extraAttribute2 = value;
            OnPropertyChanged(nameof(ExtraAttribute2));
        }
    }
    
    private uint _extraAttribute3;
    public uint ExtraAttribute3
    {
        get => _extraAttribute3;
        set
        {
            if (_extraAttribute3 == value)
                return;
    
            _extraAttribute3 = value;
            OnPropertyChanged(nameof(ExtraAttribute3));
        }
    }
    

    public override byte[] DataBytes
    {
        get
        {
            var data = new List<byte>((int)DataLength);

            data.AddRange(BitConverter.GetBytes(_isStatic));
            data.AddRange(BitConverter.GetBytes(DefaultArea));
            data.AddRange(BitConverter.GetBytes(_canRoll));
            data.AddRange(BitConverter.GetBytes(_canSlide));
            data.AddRange(BitConverter.GetBytes(_canSpin));
            data.AddRange(BitConverter.GetBytes(_canBounce));
            data.AddRange(BitConverter.GetBytes(ExtraAttribute1));
            data.AddRange(BitConverter.GetBytes(ExtraAttribute2));
            data.AddRange(BitConverter.GetBytes(ExtraAttribute3));

            return [.. data];
        }
    }
    public override uint DataLength => sizeof(ushort) + sizeof(uint) + sizeof(ushort) + sizeof(ushort) + sizeof(ushort) + sizeof(ushort) + sizeof(uint) + sizeof(uint) + sizeof(uint);

    public CollisionObjectAttributeChunk(EndianAwareBinaryReader br) : this(br.ReadUInt16(), br.ReadUInt32(), br.ReadUInt16(), br.ReadUInt16(), br.ReadUInt16(), br.ReadUInt16(), br.ReadUInt32(), br.ReadUInt32(), br.ReadUInt32())
    {
    }

    public CollisionObjectAttributeChunk(bool isStatic, uint defaultArea, bool canRoll, bool canSlide, bool canSpin, bool canBounce, uint extraAttribute1, uint extraAttribute2, uint extraAttribute3) : this((ushort)(isStatic ? 1u : 0u), defaultArea, (ushort)(canRoll ? 1 : 0), (ushort)(canSlide ? 1 : 0), (ushort)(canSpin ? 1 : 0), (ushort)(canBounce ? 1 : 0), extraAttribute1, extraAttribute2, extraAttribute3)
    {
    }

    public CollisionObjectAttributeChunk(ushort isStatic, uint defaultArea, ushort canRoll, ushort canSlide, ushort canSpin, ushort canBounce, uint extraAttribute1, uint extraAttribute2, uint extraAttribute3) : base(ChunkID)
    {
        _isStatic = isStatic;
        _defaultArea = defaultArea;
        _canRoll = canRoll;
        _canSlide = canSlide;
        _canSpin = canSpin;
        _canBounce = canBounce;
        _extraAttribute1 = extraAttribute1;
        _extraAttribute2 = extraAttribute2;
        _extraAttribute3 = extraAttribute3;
    }

    protected override void WriteData(EndianAwareBinaryWriter bw)
    {
        bw.Write(_isStatic);
        bw.Write(DefaultArea);
        bw.Write(_canRoll);
        bw.Write(_canSlide);
        bw.Write(_canSpin);
        bw.Write(_canBounce);
        bw.Write(ExtraAttribute1);
        bw.Write(ExtraAttribute2);
        bw.Write(ExtraAttribute3);
    }

    protected override Chunk CloneSelf() => new CollisionObjectAttributeChunk(IsStatic, DefaultArea, CanRoll, CanSlide, CanSpin, CanBounce, ExtraAttribute1, ExtraAttribute2, ExtraAttribute3);
}
