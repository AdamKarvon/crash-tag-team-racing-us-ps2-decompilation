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
public class OldFrameController2Chunk : NamedChunk
{
    public const ChunkIdentifier ChunkID = ChunkIdentifier.Old_Frame_Controller_2;

    public enum Types
    {
        Undefined,
        Camera,
        Expression,
        Light,
        PolySkin,
        CompoundMesh,
        ScenegraphVisibility,
        DeformPolySkin,
        Texture,
        ScenegraphTransform,
        HSplineOffsetAbsolute,
        HSplineOffsetRelative,
        HSplineSkin,
        Effect,
        CompositeDrawable,
        CompositeDrawableVisibility,
    }

    private uint _version;
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
    
    private Types _type;
    public Types Type
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
    
    private readonly P3DString _hierarchyName;
    public string HierarchyName
    {
        get => _hierarchyName?.Value ?? string.Empty;
        set => _hierarchyName.Value = value;
    }
    private readonly P3DString _animationName;
    public string AnimationName
    {
        get => _animationName?.Value ?? string.Empty;
        set => _animationName.Value = value;
    }

    public override byte[] DataBytes
    {
        get
        {
            var data = new List<byte>((int)DataLength);

            data.AddRange(BinaryExtensions.GetP3DStringBytes(Name));
            data.AddRange(BitConverter.GetBytes(Version));
            data.AddRange(BitConverter.GetBytes((uint)Type));
            data.AddRange(BinaryExtensions.GetP3DStringBytes(HierarchyName));
            data.AddRange(BinaryExtensions.GetP3DStringBytes(AnimationName));

            return [.. data];
        }
    }
    public override uint DataLength => BinaryExtensions.GetP3DStringLength(Name) + sizeof(uint) + sizeof(uint) + BinaryExtensions.GetP3DStringLength(HierarchyName) + BinaryExtensions.GetP3DStringLength(AnimationName);

    public OldFrameController2Chunk(EndianAwareBinaryReader br) : this(br.ReadP3DString(), br.ReadUInt32(), (Types)br.ReadUInt32(), br.ReadP3DString(), br.ReadP3DString())
    {
    }

    public OldFrameController2Chunk(string name, uint version, Types type, string hierarchyName, string animationName) : base(ChunkID, name)
    {
        _version = version;
        _type = type;
        _hierarchyName = new(this, hierarchyName, nameof(HierarchyName));
        _animationName = new(this, animationName, nameof(AnimationName));
    }

    public override IEnumerable<InvalidP3DException> ValidateChunk()
    {
        foreach (var error in base.ValidateChunk())
            yield return error;

        if (!HierarchyName.IsValidP3DString())
            yield return new InvalidP3DStringException(this, nameof(HierarchyName), HierarchyName);

        if (!AnimationName.IsValidP3DString())
            yield return new InvalidP3DStringException(this, nameof(AnimationName), AnimationName);
    }

    protected override void WriteData(EndianAwareBinaryWriter bw)
    {
        bw.WriteP3DString(Name);
        bw.Write(Version);
        bw.Write((uint)Type);
        bw.WriteP3DString(HierarchyName);
        bw.WriteP3DString(AnimationName);
    }

    protected override Chunk CloneSelf() => new OldFrameController2Chunk(Name, Version, Type, HierarchyName, AnimationName);
}
