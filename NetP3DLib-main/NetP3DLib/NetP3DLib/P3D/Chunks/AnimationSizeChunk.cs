using NetP3DLib.IO;
using NetP3DLib.P3D.Attributes;
using NetP3DLib.P3D.Enums;
using System;
using System.Collections.Generic;
using System.ComponentModel;

namespace NetP3DLib.P3D.Chunks;

[ChunkAttributes(ChunkID)]
public class AnimationSizeChunk : Chunk
{
    public const ChunkIdentifier ChunkID = ChunkIdentifier.Animation_Size;

    private uint _version;
    [DefaultValue(1)]
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
    
    public uint PC => (ParentChunk as AnimationChunk)?.CalculateMemorySize(AnimationChunk.Platform.PC) ?? 0;
    public uint PS2 => (ParentChunk as AnimationChunk)?.CalculateMemorySize(AnimationChunk.Platform.PS2) ?? 0;
    public uint XBOX => (ParentChunk as AnimationChunk)?.CalculateMemorySize(AnimationChunk.Platform.XBOX) ?? 0;
    public uint GC => (ParentChunk as AnimationChunk)?.CalculateMemorySize(AnimationChunk.Platform.GC) ?? 0;

    public override byte[] DataBytes
    {
        get
        {
            var data = new List<byte>((int)DataLength);

            data.AddRange(BitConverter.GetBytes(Version));
            data.AddRange(BitConverter.GetBytes(PC));
            data.AddRange(BitConverter.GetBytes(PS2));
            data.AddRange(BitConverter.GetBytes(XBOX));
            data.AddRange(BitConverter.GetBytes(GC));

            return [.. data];
        }
    }
    public override uint DataLength => sizeof(uint) + sizeof(uint) + sizeof(uint) + sizeof(uint) + sizeof(uint);

    [System.Diagnostics.CodeAnalysis.SuppressMessage("Style", "IDE0059:Unnecessary assignment of a value", Justification = "We want to read the value to progress the BinaryReader, but not set the value anywhere because it's calculated dynamically.")]
    public AnimationSizeChunk(EndianAwareBinaryReader br) : this(br.ReadUInt32())
    {
        var pc = br.ReadUInt32();
        var ps2 = br.ReadUInt32();
        var xbox = br.ReadUInt32();
        var gc = br.ReadUInt32();
    }

    public AnimationSizeChunk(uint version) : base(ChunkID)
    {
        _version = version;
    }

    protected override void WriteData(EndianAwareBinaryWriter bw)
    {
        bw.Write(Version);
        bw.Write(PC);
        bw.Write(PS2);
        bw.Write(XBOX);
        bw.Write(GC);
    }

    protected override Chunk CloneSelf() => new AnimationSizeChunk(Version);
}
