using NetP3DLib.IO;
using NetP3DLib.P3D.Attributes;
using NetP3DLib.P3D.Enums;

namespace NetP3DLib.P3D.Chunks;

[ChunkAttributes(ChunkID)]
public class BlackMagicChunk : Chunk
{
    public const ChunkIdentifier ChunkID = ChunkIdentifier.Black_Magic;

    public override byte[] DataBytes
    {
        get
        {
            return [];
        }
    }
    public override uint DataLength => 0;

    [System.Diagnostics.CodeAnalysis.SuppressMessage("Style", "IDE0060:Remove unused parameter", Justification = "Required for ChunkLoader.")]
    public BlackMagicChunk(EndianAwareBinaryReader br) : this()
    { }

    public BlackMagicChunk() : base(ChunkID)
    { }

    protected override void WriteData(EndianAwareBinaryWriter bw)
    { }

    protected override Chunk CloneSelf() => new BlackMagicChunk();
}