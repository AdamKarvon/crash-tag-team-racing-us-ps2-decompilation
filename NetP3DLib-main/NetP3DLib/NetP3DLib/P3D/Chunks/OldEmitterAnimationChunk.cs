using NetP3DLib.IO;
using NetP3DLib.P3D.Attributes;
using NetP3DLib.P3D.Enums;
using NetP3DLib.P3D.Exceptions;
using System;
using System.Collections.Generic;
using System.ComponentModel;

namespace NetP3DLib.P3D.Chunks;

[ChunkAttributes(ChunkID)]
public class OldEmitterAnimationChunk : Chunk
{
    public const ChunkIdentifier ChunkID = ChunkIdentifier.Old_Emitter_Animation;

    private uint _version;
    [DefaultValue(0)]
    public uint Version
    {
        get => _version;
        set
        {
            if (_version == value)
                return;
    
            _version = value;
            OnPropertyChanged(nameof(Version));
        }
    }
    

    public override byte[] DataBytes
    {
        get
        {
            var data = new List<byte>((int)DataLength);

            data.AddRange(BitConverter.GetBytes(Version));

            return [.. data];
        }
    }
    public override uint DataLength => sizeof(uint);

    public OldEmitterAnimationChunk(EndianAwareBinaryReader br) : this(br.ReadUInt32())
    {
    }

    public OldEmitterAnimationChunk(uint version) : base(ChunkID)
    {
        _version = version;
    }

    public override IEnumerable<InvalidP3DException> ValidateChunk()
    {
        foreach (var error in base.ValidateChunk())
            yield return error;

        if (Children.Count < 1)
            yield return new InvalidP3DException(this, $"First child must be {nameof(AnimationChunk)}.");
        else if (Children[0].ID != (uint)ChunkIdentifier.Animation)
            yield return new InvalidP3DException(this, $"First child is not {nameof(AnimationChunk)}.");
    }

    protected override void WriteData(EndianAwareBinaryWriter bw)
    {
        bw.Write(Version);
    }

    protected override Chunk CloneSelf() => new OldEmitterAnimationChunk(Version);
}
