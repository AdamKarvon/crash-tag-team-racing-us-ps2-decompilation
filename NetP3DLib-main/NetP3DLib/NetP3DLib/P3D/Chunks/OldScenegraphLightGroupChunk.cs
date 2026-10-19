using NetP3DLib.IO;
using NetP3DLib.P3D.Attributes;
using NetP3DLib.P3D.Enums;
using NetP3DLib.P3D.Exceptions;
using NetP3DLib.P3D.Extensions;
using NetP3DLib.P3D.Types;
using System.Collections.Generic;

namespace NetP3DLib.P3D.Chunks;

[ChunkAttributes(ChunkID)]
public class OldScenegraphLightGroupChunk : NamedChunk
{
    public const ChunkIdentifier ChunkID = ChunkIdentifier.Old_Scenegraph_Light_Group;

    private readonly P3DString _lightGroupName;
    public string LightGroupName
    {
        get => _lightGroupName?.Value ?? string.Empty;
        set => _lightGroupName.Value = value;
    }

    public override byte[] DataBytes
    {
        get
        {
            var data = new List<byte>((int)DataLength);

            data.AddRange(BinaryExtensions.GetP3DStringBytes(Name));
            data.AddRange(BinaryExtensions.GetP3DStringBytes(LightGroupName));

            return [.. data];
        }
    }
    public override uint DataLength => BinaryExtensions.GetP3DStringLength(Name) + BinaryExtensions.GetP3DStringLength(LightGroupName);

    public OldScenegraphLightGroupChunk(EndianAwareBinaryReader br) : this(br.ReadP3DString(), br.ReadP3DString())
    {
    }

    public OldScenegraphLightGroupChunk(string name, string lightGroupName) : base(ChunkID, name)
    {
        _lightGroupName = new(this, lightGroupName, nameof(LightGroupName));
    }

    public override IEnumerable<InvalidP3DException> ValidateChunk()
    {
        foreach (var error in base.ValidateChunk())
            yield return error;

        if (!LightGroupName.IsValidP3DString())
            yield return new InvalidP3DStringException(this, nameof(LightGroupName), LightGroupName);
    }

    protected override void WriteData(EndianAwareBinaryWriter bw)
    {
        bw.WriteP3DString(Name);
        bw.WriteP3DString(LightGroupName);
    }

    protected override Chunk CloneSelf() => new OldScenegraphLightGroupChunk(Name, LightGroupName);
}