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
public class ExpressionMixerChunk : NamedChunk
{
    public const ChunkIdentifier ChunkID = ChunkIdentifier.Expression_Mixer;

    public enum MixerType : uint
    {
        Undefined,
        Pose,
        HSplineOffset,
        VertexOffset,
    }

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
    
    private MixerType _type;
    public MixerType Type
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
    
    private readonly P3DString _targetName;
    public string TargetName
    {
        get => _targetName?.Value ?? string.Empty;
        set => _targetName.Value = value;
    }
    private readonly P3DString _expressionGroupName;
    public string ExpressionGroupName
    {
        get => _expressionGroupName?.Value ?? string.Empty;
        set => _expressionGroupName.Value = value;
    }

    public override byte[] DataBytes
    {
        get
        {
            var data = new List<byte>((int)DataLength);

            data.AddRange(BitConverter.GetBytes(Version));
            data.AddRange(BinaryExtensions.GetP3DStringBytes(Name));
            data.AddRange(BitConverter.GetBytes((uint)Type));
            data.AddRange(BinaryExtensions.GetP3DStringBytes(TargetName));
            data.AddRange(BinaryExtensions.GetP3DStringBytes(ExpressionGroupName));

            return [.. data];
        }
    }
    public override uint DataLength => sizeof(uint) + BinaryExtensions.GetP3DStringLength(Name) + sizeof(uint) + BinaryExtensions.GetP3DStringLength(TargetName) + BinaryExtensions.GetP3DStringLength(ExpressionGroupName);

    public ExpressionMixerChunk(EndianAwareBinaryReader br) : this(br.ReadUInt32(), br.ReadP3DString(), (MixerType)br.ReadUInt32(), br.ReadP3DString(), br.ReadP3DString())
    {
    }

    public ExpressionMixerChunk(uint version, string name, MixerType type, string targetName, string expressionGroupName) : base(ChunkID, name)
    {
        _version = version;
        _type = type;
        _targetName = new(this, targetName, nameof(TargetName));
        _expressionGroupName = new(this, expressionGroupName, nameof(ExpressionGroupName));
    }

    public override IEnumerable<InvalidP3DException> ValidateChunk()
    {
        foreach (var error in base.ValidateChunk())
            yield return error;

        if (!TargetName.IsValidP3DString())
            yield return new InvalidP3DStringException(this, nameof(TargetName), TargetName);

        if (!ExpressionGroupName.IsValidP3DString())
            yield return new InvalidP3DStringException(this, nameof(ExpressionGroupName), ExpressionGroupName);
    }

    protected override void WriteData(EndianAwareBinaryWriter bw)
    {
        bw.Write(Version);
        bw.WriteP3DString(Name);
        bw.Write((uint)Type);
        bw.WriteP3DString(TargetName);
        bw.WriteP3DString(ExpressionGroupName);
    }

    protected override Chunk CloneSelf() => new ExpressionMixerChunk(Version, Name, Type, TargetName, ExpressionGroupName);
}
