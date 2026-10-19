using NetP3DLib.IO;
using NetP3DLib.P3D.Attributes;
using NetP3DLib.P3D.Enums;
using NetP3DLib.P3D.Extensions;
using System;
using System.Collections.Generic;

namespace NetP3DLib.P3D.Chunks;

[ChunkAttributes(ChunkID)]
public class StatePropVisibilitiesDataChunk : NamedChunk
{
    public const ChunkIdentifier ChunkID = ChunkIdentifier.State_Prop_Visibilities_Data;

    private uint _isVisible;
    public bool IsVisible
    {
        get => _isVisible != 0;
        set
        {
            if (IsVisible == value)
                return;

            _isVisible = value ? 1u : 0u;
            OnPropertyChanged(nameof(IsVisible));
        }
    }

    public override byte[] DataBytes
    {
        get
        {
            var data = new List<byte>((int)DataLength);

            data.AddRange(BinaryExtensions.GetP3DStringBytes(Name));
            data.AddRange(BitConverter.GetBytes(_isVisible));

            return [.. data];
        }
    }
    public override uint DataLength => BinaryExtensions.GetP3DStringLength(Name) + sizeof(uint);

    public StatePropVisibilitiesDataChunk(EndianAwareBinaryReader br) : this(br.ReadP3DString(), br.ReadUInt32())
    {
    }

    public StatePropVisibilitiesDataChunk(string name, bool isVisible) : this(name, isVisible ? 1u : 0u)
    {
    }

    public StatePropVisibilitiesDataChunk(string name, uint isVisible) : base(ChunkID, name)
    {
        _isVisible = isVisible;
    }

    protected override void WriteData(EndianAwareBinaryWriter bw)
    {
        bw.WriteP3DString(Name);
        bw.Write(_isVisible);
    }

    protected override Chunk CloneSelf() => new StatePropVisibilitiesDataChunk(Name, IsVisible);
}