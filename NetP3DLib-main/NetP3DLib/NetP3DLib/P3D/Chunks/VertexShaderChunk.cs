using NetP3DLib.IO;
using NetP3DLib.P3D.Attributes;
using NetP3DLib.P3D.Enums;
using NetP3DLib.P3D.Extensions;
using System.Collections.Generic;

namespace NetP3DLib.P3D.Chunks;

[ChunkAttributes(ChunkID)]
public class VertexShaderChunk : NamedChunk
{
    public const ChunkIdentifier ChunkID = ChunkIdentifier.Vertex_Shader;

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

    public VertexShaderChunk(EndianAwareBinaryReader br) : this(br.ReadP3DString())
    {
    }

    public VertexShaderChunk(string name) : base(ChunkID, name)
    {
    }

    protected override void WriteData(EndianAwareBinaryWriter bw)
    {
        bw.WriteP3DString(Name);
    }

    protected override Chunk CloneSelf() => new VertexShaderChunk(Name);
}