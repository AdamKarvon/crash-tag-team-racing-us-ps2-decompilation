using NetP3DLib.IO;
using NetP3DLib.P3D.Attributes;
using NetP3DLib.P3D.Enums;
using NetP3DLib.P3D.Exceptions;
using NetP3DLib.P3D.Extensions;
using System.Collections.Generic;

namespace NetP3DLib.P3D.Chunks;

[ChunkAttributes(ChunkID)]
public class InstanceListChunk : NamedChunk
{
    public const ChunkIdentifier ChunkID = ChunkIdentifier.Instance_List;
    private static readonly ChunkIdentifier[] ExpectedHierarchy =
    {
        ChunkIdentifier.Scenegraph,
        ChunkIdentifier.Old_Scenegraph_Root,
        ChunkIdentifier.Old_Scenegraph_Branch,
        ChunkIdentifier.Old_Scenegraph_Transform
    };

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

    public InstanceListChunk(EndianAwareBinaryReader br) : this(br.ReadP3DString())
    {
    }

    public InstanceListChunk(string name) : base(ChunkID, name)
    {
    }

    public override IEnumerable<InvalidP3DException> ValidateChunk()
    {
        foreach (var error in base.ValidateChunk())
            yield return error;

        Chunk current = this;

        for (int i = 0; i < ExpectedHierarchy.Length; i++)
        {
            var expected = ExpectedHierarchy[i];

            if (current.Children.Count != 1 || current.Children[0].ID != (uint)expected)
            {
                yield return new InvalidP3DException(current, $"Expected hierarchy: {string.Join(" > ", ExpectedHierarchy)}. Missing or incorrect {expected} at level {i + 1}.");
                yield break;
            }

            current = current.Children[0];
        }

        if (current.Children.Count != current.GetChildCount(ChunkIdentifier.Old_Scenegraph_Transform))
        {
            yield return new InvalidP3DException(current, $"{nameof(OldScenegraphTransformChunk)} children must all be {nameof(OldScenegraphTransformChunk)}.");
        }
    }

    protected override void WriteData(EndianAwareBinaryWriter bw)
    {
        bw.WriteP3DString(Name);
    }

    protected override Chunk CloneSelf() => new InstanceListChunk(Name);
}