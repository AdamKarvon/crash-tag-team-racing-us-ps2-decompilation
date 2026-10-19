using NetP3DLib.IO;
using NetP3DLib.P3D.Attributes;
using NetP3DLib.P3D.Enums;
using NetP3DLib.P3D.Exceptions;
using NetP3DLib.P3D.Extensions;
using NetP3DLib.P3D.Types;
using System;
using System.Collections.Generic;
using System.ComponentModel;

namespace NetP3DLib.P3D.Chunks;

[ChunkAttributes(ChunkID)]
public class OldFrameControllerChunk : NamedChunk
{
    private readonly static Dictionary<AnimationType, Type> AnimationTypeToChunkTypeMap = [];
    private static void Add<T>(AnimationType type) where T : Chunk => AnimationTypeToChunkTypeMap.Add(type, typeof(T));

    static OldFrameControllerChunk()
    {
        Add<AnimatedObjectChunk>(AnimationType.AnimatedObject);
        Add<CameraChunk>(AnimationType.Camera);
        Add<ExpressionMixerChunk>(AnimationType.Expression);
        Add<LightChunk>(AnimationType.Light);
        Add<CompositeDrawableChunk>(AnimationType.PoseTransform);
        Add<CompositeDrawableChunk>(AnimationType.PoseVisibility);
        Add<ScenegraphChunk>(AnimationType.ScenegraphTransform);
        Add<ScenegraphChunk>(AnimationType.ScenegraphVisibility);
        Add<ShaderChunk>(AnimationType.Texture);
        Add<OldBillboardQuadGroupChunk>(AnimationType.BillboardQuadGroup);
        Add<ParticleSystemChunk>(AnimationType.Effect);
        Add<MeshChunk>(AnimationType.Vertex);
        Add<ShaderChunk>(AnimationType.Shader);
    }


    public const ChunkIdentifier ChunkID = ChunkIdentifier.Old_Frame_Controller;

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
    
    private AnimationType _type;
    public AnimationType Type
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
    
    private float _frameOffset;
    public float FrameOffset
    {
        get => _frameOffset;
        set
        {
            if (_frameOffset == value)
                return;
    
            _frameOffset = value;
            OnPropertyChanged(nameof(FrameOffset));
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
            data.AddRange(BitConverter.GetBytes(Version));
            data.AddRange(BinaryExtensions.GetP3DStringBytes(Name));
            data.AddRange(BitConverter.GetBytes((uint)Type));
            data.AddRange(BitConverter.GetBytes(FrameOffset));
            data.AddRange(BinaryExtensions.GetP3DStringBytes(HierarchyName));
            data.AddRange(BinaryExtensions.GetP3DStringBytes(AnimationName));

            return [.. data];
        }
    }
    public override uint DataLength => sizeof(uint) + BinaryExtensions.GetP3DStringLength(Name) + sizeof(uint) + sizeof(float) + BinaryExtensions.GetP3DStringLength(HierarchyName) + BinaryExtensions.GetP3DStringLength(AnimationName);

    public OldFrameControllerChunk(EndianAwareBinaryReader br) : this(br.ReadUInt32(), br.ReadP3DString(), (AnimationType)br.ReadUInt32(), br.ReadSingle(), br.ReadP3DString(), br.ReadP3DString())
    {
    }

    public OldFrameControllerChunk(uint version, string name, AnimationType type, float frameOffset, string hierarchyName, string animationName) : base(ChunkID, name)
    {
        _version = version;
        _type = type;
        _frameOffset = frameOffset;
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

        if (Type != AnimationType.AnimatedObject && Type != AnimationType.Effect && FindNamedChunkInParentHierarchy<AnimationChunk>(AnimationName) == null)
            yield return new InvalidP3DException(this, $"Could not find animation with name \"{AnimationName}\" in the parent hierarchy.");

        if (AnimationTypeToChunkTypeMap.TryGetValue(Type, out var hierarchyType) && FindNamedChunkInParentHierarchy(hierarchyType, HierarchyName) == null)
            yield return new InvalidP3DException(this, $"Could not find {hierarchyType.Name} with name \"{HierarchyName}\" in the parent hierarchy.");
    }

    protected override void WriteData(EndianAwareBinaryWriter bw)
    {
        bw.Write(Version);
        bw.WriteP3DString(Name);
        bw.Write((uint)Type);
        bw.Write(FrameOffset);
        bw.WriteP3DString(HierarchyName);
        bw.WriteP3DString(AnimationName);
    }

    protected override Chunk CloneSelf() => new OldFrameControllerChunk(Version, Name, Type, FrameOffset, HierarchyName, AnimationName);
}
