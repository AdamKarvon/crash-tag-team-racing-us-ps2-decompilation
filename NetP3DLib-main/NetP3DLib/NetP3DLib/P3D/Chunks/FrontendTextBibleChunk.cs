using NetP3DLib.IO;
using NetP3DLib.P3D.Attributes;
using NetP3DLib.P3D.Enums;
using NetP3DLib.P3D.Extensions;
using System;
using System.Collections.Generic;
using System.Text;

namespace NetP3DLib.P3D.Chunks;

[ChunkAttributes(ChunkID)]
public class FrontendTextBibleChunk : NamedChunk
{
    public const ChunkIdentifier ChunkID = ChunkIdentifier.Frontend_Text_Bible;

    public uint NumLanguages => (uint)Languages.Length;
    public string Languages // TODO: This is probably fucked with the dynamic size changes, but also it never changes so... Future me problem
    {
        get
        {
            var sb = new StringBuilder();
            foreach (var frontendLanguageChunk in GetChunksOfType<FrontendLanguageChunk>())
                sb.Append(frontendLanguageChunk.Language);
            return sb.ToString();
        }
    }

    public override byte[] DataBytes
    {
        get
        {
            var data = new List<byte>((int)DataLength);

            data.AddRange(BinaryExtensions.GetP3DStringBytes(Name));
            data.AddRange(BitConverter.GetBytes(NumLanguages));
            data.AddRange(BinaryExtensions.GetP3DStringBytes(Languages));

            return [.. data];
        }
    }
    public override uint DataLength => BinaryExtensions.GetP3DStringLength(Name) + sizeof(uint) + BinaryExtensions.GetP3DStringLength(Languages);

    [System.Diagnostics.CodeAnalysis.SuppressMessage("Style", "IDE0059:Unnecessary assignment of a value", Justification = "We want to read the value to progress the BinaryReader, but not set the value anywhere because it's calculated dynamically.")]
    public FrontendTextBibleChunk(EndianAwareBinaryReader br) : this(br.ReadP3DString())
    {
        var numLanguages = br.ReadUInt32();
        var languages = br.ReadP3DString();
    }

    public FrontendTextBibleChunk(string name) : base(ChunkID, name)
    {
    }

    protected override void WriteData(EndianAwareBinaryWriter bw)
    {
        bw.WriteP3DString(Name);
        bw.Write(NumLanguages);
        bw.WriteP3DString(Languages);
    }

    protected override Chunk CloneSelf() => new FrontendTextBibleChunk(Name);
}