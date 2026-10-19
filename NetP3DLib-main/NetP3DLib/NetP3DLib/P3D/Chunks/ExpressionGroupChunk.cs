using NetP3DLib.IO;
using NetP3DLib.P3D.Attributes;
using NetP3DLib.P3D.Collections;
using NetP3DLib.P3D.Enums;
using NetP3DLib.P3D.Exceptions;
using NetP3DLib.P3D.Extensions;
using NetP3DLib.P3D.Types;
using System;
using System.Collections.Generic;
using System.ComponentModel;

namespace NetP3DLib.P3D.Chunks;

[ChunkAttributes(ChunkID)]
public class ExpressionGroupChunk : NamedChunk
{
    public const ChunkIdentifier ChunkID = ChunkIdentifier.Expression_Group;

    public enum Stage : uint
    {
        Stage1,
        Stage2,
        Stage3,
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
    
    private readonly P3DString _targetName;
    public string TargetName
    {
        get => _targetName?.Value ?? string.Empty;
        set => _targetName.Value = value;
    }
    public uint NumStages
    {
        get => (uint)(Stages?.Count ?? 0);
        set
        {
            if (value == NumStages)
                return;

            if (value < NumStages)
            {
                Stages.RemoveRange((int)value, (int)(NumStages - value));
            }
            else
            {
                int count = (int)(value - NumStages);
                var newStages = new Stage[count];

                for (var i = 0; i < count; i++)
                    newStages[i] = default;

                Stages.AddRange(newStages);
            }
        }
    }
    public SizeAwareList<Stage> Stages { get; }

    public override byte[] DataBytes
    {
        get
        {
            var data = new List<byte>((int)DataLength);

            data.AddRange(BitConverter.GetBytes(Version));
            data.AddRange(BinaryExtensions.GetP3DStringBytes(Name));
            data.AddRange(BinaryExtensions.GetP3DStringBytes(TargetName));
            data.AddRange(BitConverter.GetBytes(NumStages));
            foreach (var stage in Stages)
                data.AddRange(BitConverter.GetBytes((uint)stage));

            return [.. data];
        }
    }
    public override uint DataLength => sizeof(uint) + BinaryExtensions.GetP3DStringLength(Name) + BinaryExtensions.GetP3DStringLength(TargetName) + sizeof(uint) + sizeof(uint) * NumStages;

    public ExpressionGroupChunk(EndianAwareBinaryReader br) : this(br.ReadUInt32(), br.ReadP3DString(), br.ReadP3DString(), br.ReadEnumArray<Stage>(out _))
    {
    }

    public ExpressionGroupChunk(uint version, string name, string targetName, IList<Stage> stages) : base(ChunkID, name)
    {
        _version = version;
        _targetName = new(this, targetName, nameof(TargetName));
        Stages = CreateSizeAwareList(stages, Stages_CollectionChanged);
    }
    
    private void Stages_CollectionChanged(object sender, System.Collections.Specialized.NotifyCollectionChangedEventArgs e) => OnPropertyChanged(nameof(Stages));

    public override IEnumerable<InvalidP3DException> ValidateChunk()
    {
        foreach (var error in base.ValidateChunk())
            yield return error;

        if (!TargetName.IsValidP3DString())
            yield return new InvalidP3DStringException(this, nameof(TargetName), TargetName);
    }

    protected override void WriteData(EndianAwareBinaryWriter bw)
    {
        bw.Write(Version);
        bw.WriteP3DString(Name);
        bw.WriteP3DString(TargetName);
        bw.Write(NumStages);
        foreach (var stage in Stages)
            bw.Write((uint)stage);
    }

    protected override Chunk CloneSelf() => new ExpressionGroupChunk(Version, Name, TargetName, Stages);
}
