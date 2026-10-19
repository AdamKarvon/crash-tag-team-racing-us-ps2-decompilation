using NetP3DLib.IO;
using NetP3DLib.P3D.Attributes;
using NetP3DLib.P3D.Enums;
using System;
using System.Collections.Generic;
using System.ComponentModel;

namespace NetP3DLib.P3D.Chunks;

[ChunkAttributes(ChunkID)]
public class OldGeneratorAnimationChunk : Chunk
{
    public const ChunkIdentifier ChunkID = ChunkIdentifier.Old_Generator_Animation;

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

    public OldGeneratorAnimationChunk(EndianAwareBinaryReader br) : this(br.ReadUInt32())
    {
    }

    public OldGeneratorAnimationChunk(uint version) : base(ChunkID)
    {
        _version = version;
    }

    protected override void WriteData(EndianAwareBinaryWriter bw)
    {
        bw.Write(Version);
    }

    protected override Chunk CloneSelf() => new OldGeneratorAnimationChunk(Version);
}
