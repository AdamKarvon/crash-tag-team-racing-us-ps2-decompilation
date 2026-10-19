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
public class FrontendStringHardCodedChunk : Chunk
{
    public const ChunkIdentifier ChunkID = ChunkIdentifier.Frontend_String_Hard_Coded;

    private readonly P3DString _string;
    public string String
    {
        get => _string?.Value ?? string.Empty;
        set => _string.Value = value;
    }

    public override byte[] DataBytes
    {
        get
        {
            var data = new List<byte>((int)DataLength);

            data.AddRange(BinaryExtensions.GetP3DStringBytes(String));

            return [.. data];
        }
    }
    public override uint DataLength => BinaryExtensions.GetP3DStringLength(String);

    public FrontendStringHardCodedChunk(EndianAwareBinaryReader br) : this(br.ReadP3DString())
    {
    }

    public FrontendStringHardCodedChunk(string @string) : base(ChunkID)
    {
        _string = new(this, @string, nameof(String));
    }

    public override IEnumerable<InvalidP3DException> ValidateChunk()
    {
        foreach (var error in base.ValidateChunk())
            yield return error;

        if (!String.IsValidP3DString())
            yield return new InvalidP3DStringException(this, nameof(String), String);
    }

    protected override void WriteData(EndianAwareBinaryWriter bw)
    {
        bw.WriteP3DString(String);
    }

    protected override Chunk CloneSelf() => new FrontendStringHardCodedChunk(String);

    public override string ToString() => $"\"{String}\" ({GetChunkType(this)} (0x{ID:X}))";
}