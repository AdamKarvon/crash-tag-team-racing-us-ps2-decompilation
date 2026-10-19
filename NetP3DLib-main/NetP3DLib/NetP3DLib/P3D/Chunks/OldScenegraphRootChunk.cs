using NetP3DLib.IO;
using NetP3DLib.P3D.Attributes;
using NetP3DLib.P3D.Enums;

namespace NetP3DLib.P3D.Chunks;

[ChunkAttributes(ChunkID)]
public class OldScenegraphRootChunk : Chunk
{
    public const ChunkIdentifier ChunkID = ChunkIdentifier.Old_Scenegraph_Root;

    public override byte[] DataBytes
    {
        get
        {
            return [];
        }
    }
    public override uint DataLength => 0;

    [System.Diagnostics.CodeAnalysis.SuppressMessage("Style", "IDE0060:Remove unused parameter", Justification = "Required for ChunkLoader.")]
    public OldScenegraphRootChunk(EndianAwareBinaryReader br) : this()
    { }

    public OldScenegraphRootChunk() : base(ChunkID)
    { }

    protected override void WriteData(EndianAwareBinaryWriter bw)
    { }

    protected override Chunk CloneSelf() => new OldScenegraphRootChunk();
}