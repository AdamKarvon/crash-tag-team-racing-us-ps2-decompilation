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
public class CollisionEffectChunk : Chunk
{
    public const ChunkIdentifier ChunkID = ChunkIdentifier.Collision_Effect;

    public enum ClassTypes
    {
        WTF,
        Ground,
        PropStatic,
        PropMoveable,
        PropBreakable,
        AnimatedBV,
        Drawable,
        Static,
        PropDrawable,
        PropAnimBreakable,
        PropOnetimeMoveable,
    }

    private ClassTypes _classType;
    public ClassTypes ClassType
    {
        get => _classType;
        set
        {
            if (_classType == value)
                return;
    
            _classType = value;
            OnPropertyChanged(nameof(ClassType));
        }
    }
    
    private uint _physPropID;
    public uint PhysPropID
    {
        get => _physPropID;
        set
        {
            if (_physPropID == value)
                return;
    
            _physPropID = value;
            OnPropertyChanged(nameof(PhysPropID));
        }
    }
    
    private readonly P3DString _soundResourceDataName;
    public string SoundResourceDataName
    {
        get => _soundResourceDataName?.Value ?? string.Empty;
        set => _soundResourceDataName.Value = value;
    }

    public override byte[] DataBytes
    {
        get
        {
            var data = new List<byte>((int)DataLength);

            data.AddRange(BitConverter.GetBytes((uint)ClassType));
            data.AddRange(BitConverter.GetBytes(PhysPropID));
            data.AddRange(BinaryExtensions.GetP3DStringBytes(SoundResourceDataName));

            return [.. data];
        }
    }
    public override uint DataLength => sizeof(uint) + sizeof(uint) + BinaryExtensions.GetP3DStringLength(SoundResourceDataName);

    public CollisionEffectChunk(EndianAwareBinaryReader br) : this((ClassTypes)br.ReadUInt32(), br.ReadUInt32(), br.ReadP3DString())
    {
    }

    public CollisionEffectChunk(ClassTypes classType, uint phyPropID, string soundResourceDataName) : base(ChunkID)
    {
        _classType = classType;
        _physPropID = phyPropID;
        _soundResourceDataName = new(this, soundResourceDataName, nameof(SoundResourceDataName));
    }

    public override IEnumerable<InvalidP3DException> ValidateChunk()
    {
        foreach (var error in base.ValidateChunk())
            yield return error;

        if (!SoundResourceDataName.IsValidP3DString())
            yield return new InvalidP3DStringException(this, nameof(SoundResourceDataName), SoundResourceDataName);
    }

    protected override void WriteData(EndianAwareBinaryWriter bw)
    {
        bw.Write((uint)ClassType);
        bw.Write(PhysPropID);
        bw.WriteP3DString(SoundResourceDataName);
    }

    protected override Chunk CloneSelf() => new CollisionEffectChunk(ClassType, PhysPropID, SoundResourceDataName);

    public override string ToString() => $"{ClassType} ({GetChunkType(this)} (0x{ID:X}))";
}
