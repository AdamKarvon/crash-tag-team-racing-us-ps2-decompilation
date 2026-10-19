using NetP3DLib.IO;
using NetP3DLib.P3D.Attributes;
using NetP3DLib.P3D.Enums;
using NetP3DLib.P3D.Extensions;
using System.Collections.Generic;

namespace NetP3DLib.P3D.Chunks;

[ChunkAttributes(ChunkID)]
public class ExportInfoChunk : NamedChunk
{
    public const ChunkIdentifier ChunkID = ChunkIdentifier.Export_Info;

    public override byte[] DataBytes
    {
        get
        {
            var data = new List<byte>((int)DataLength);

            data.AddRange(BinaryExtensions.GetP3DStringBytes(Name));

            return [.. data];
        }
    }
    public override uint DataLength => BinaryExtensions.GetP3DStringLength(Name);

    public ExportInfoChunk(EndianAwareBinaryReader br) : this(br.ReadP3DString())
    {
    }

    public ExportInfoChunk(string name) : base(ChunkID, name)
    {
    }

    protected override void WriteData(EndianAwareBinaryWriter bw)
    {
        bw.WriteP3DString(Name);
    }

    protected override Chunk CloneSelf() => new ExportInfoChunk(Name);
}