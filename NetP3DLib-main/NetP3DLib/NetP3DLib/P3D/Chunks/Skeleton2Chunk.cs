using NetP3DLib.IO;
using NetP3DLib.P3D.Attributes;
using NetP3DLib.P3D.Enums;
using NetP3DLib.P3D.Extensions;
using System;
using System.Collections.Generic;

namespace NetP3DLib.P3D.Chunks;

[ChunkAttributes(ChunkID)]
public class Skeleton2Chunk : NamedChunk
{
    public const ChunkIdentifier ChunkID = ChunkIdentifier.Skeleton_2;

    private uint _version;
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
    
    public uint NumJoints => GetChildCount(ChunkIdentifier.Skeleton_Joint_2);
    public uint NumPartitions => GetChildCount(ChunkIdentifier.Skeleton_Partition);
    // TODO: Check if these are somehow automated
    private uint _numLimbs;
    public uint NumLimbs
    {
        get => _numLimbs;
        set
        {
            if (_numLimbs == value)
                return;
    
            _numLimbs = value;
            OnPropertyChanged(nameof(NumLimbs));
        }
    }
    

    public override byte[] DataBytes
    {
        get
        {
            var data = new List<byte>((int)DataLength);

            data.AddRange(BinaryExtensions.GetP3DStringBytes(Name));
            data.AddRange(BitConverter.GetBytes(Version));
            data.AddRange(BitConverter.GetBytes(NumJoints));
            data.AddRange(BitConverter.GetBytes(NumPartitions));
            data.AddRange(BitConverter.GetBytes(NumLimbs));

            return [.. data];
        }
    }
    public override uint DataLength => BinaryExtensions.GetP3DStringLength(Name) + sizeof(uint) + sizeof(uint) + sizeof(uint) + sizeof(uint);

    public Skeleton2Chunk(EndianAwareBinaryReader br) : this(br.ReadP3DString(), br.ReadUInt32(), br.SkipAndRead(sizeof(uint) + sizeof(uint), br.ReadUInt32))
    {
    }

    public Skeleton2Chunk(string name, uint version, uint numLimbs) : base(ChunkID, name)
    {
        _version = version;
        _numLimbs = numLimbs;
    }

    protected override void WriteData(EndianAwareBinaryWriter bw)
    {
        bw.WriteP3DString(Name);
        bw.Write(Version);
        bw.Write(NumJoints);
        bw.Write(NumPartitions);
        bw.Write(NumLimbs);
    }

    protected override Chunk CloneSelf() => new Skeleton2Chunk(Name, Version, NumLimbs);
}
