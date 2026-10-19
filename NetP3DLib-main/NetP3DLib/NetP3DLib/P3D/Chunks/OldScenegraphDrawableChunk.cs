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
public class OldScenegraphDrawableChunk : NamedChunk
{
    public const ChunkIdentifier ChunkID = ChunkIdentifier.Old_Scenegraph_Drawable;

    private readonly P3DString _drawableName;
    public string DrawableName
    {
        get => _drawableName?.Value ?? string.Empty;
        set => _drawableName.Value = value;
    }
    private uint _isTranslucent;
    public bool IsTranslucent
    {
        get => _isTranslucent == 1;
        set
        {
            if (IsTranslucent == value)
                return;

            _isTranslucent = value ? 1u : 0u;
            OnPropertyChanged(nameof(IsTranslucent));
        }
    }

    public override byte[] DataBytes
    {
        get
        {
            var data = new List<byte>((int)DataLength);

            data.AddRange(BinaryExtensions.GetP3DStringBytes(Name));
            data.AddRange(BinaryExtensions.GetP3DStringBytes(DrawableName));
            data.AddRange(BitConverter.GetBytes(_isTranslucent));

            return [.. data];
        }
    }
    public override uint DataLength => BinaryExtensions.GetP3DStringLength(Name) + BinaryExtensions.GetP3DStringLength(DrawableName) + sizeof(uint);

    public OldScenegraphDrawableChunk(EndianAwareBinaryReader br) : this(br.ReadP3DString(), br.ReadP3DString(), br.ReadUInt32())
    {
    }

    public OldScenegraphDrawableChunk(string name, string drawableName, bool isTranslucent) : this(name, drawableName, isTranslucent ? 1u : 0u)
    {
    }

    public OldScenegraphDrawableChunk(string name, string drawableName, uint isTranslucent) : base(ChunkID, name)
    {
        _drawableName = new(this, drawableName, nameof(DrawableName));
        _isTranslucent = isTranslucent;
    }

    public override IEnumerable<InvalidP3DException> ValidateChunk()
    {
        foreach (var error in base.ValidateChunk())
            yield return error;

        if (!DrawableName.IsValidP3DString())
            yield return new InvalidP3DStringException(this, nameof(DrawableName), DrawableName);
    }

    protected override void WriteData(EndianAwareBinaryWriter bw)
    {
        bw.WriteP3DString(Name);
        bw.WriteP3DString(DrawableName);
        bw.Write(_isTranslucent);
    }

    protected override Chunk CloneSelf() => new OldScenegraphDrawableChunk(Name, DrawableName, IsTranslucent);
}