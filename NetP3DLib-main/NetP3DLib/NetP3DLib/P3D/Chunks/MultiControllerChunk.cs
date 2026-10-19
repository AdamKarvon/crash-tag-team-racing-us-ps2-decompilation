using NetP3DLib.IO;
using NetP3DLib.P3D.Attributes;
using NetP3DLib.P3D.Enums;
using NetP3DLib.P3D.Extensions;
using System;
using System.Collections.Generic;
using System.ComponentModel;

namespace NetP3DLib.P3D.Chunks;

[ChunkAttributes(ChunkID)]
public class MultiControllerChunk : NamedChunk
{
    public const ChunkIdentifier ChunkID = ChunkIdentifier.Multi_Controller;

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
    
    private float _length;
    public float Length
    {
        get => _length;
        set
        {
            if (_length == value)
                return;
    
            _length = value;
            OnPropertyChanged(nameof(Length));
        }
    }
    
    private float _framerate;
    public float Framerate
    {
        get => _framerate;
        set
        {
            if (_framerate == value)
                return;
    
            _framerate = value;
            OnPropertyChanged(nameof(Framerate));
        }
    }
    
    public uint NumTracks
    {
        get
        {
            uint numTracks = 0;
            foreach (var child in Children)
                if (child is MultiControllerTracksChunk multiControllerTracksChunk)
                    numTracks += multiControllerTracksChunk.NumTracks;
            return numTracks;
        }
    }

    public override byte[] DataBytes
    {
        get
        {
            var data = new List<byte>((int)DataLength);

            data.AddRange(BinaryExtensions.GetP3DStringBytes(Name));
            data.AddRange(BitConverter.GetBytes(Version));
            data.AddRange(BitConverter.GetBytes(Length));
            data.AddRange(BitConverter.GetBytes(Framerate));
            data.AddRange(BitConverter.GetBytes(NumTracks));

            return [.. data];
        }
    }
    public override uint DataLength => BinaryExtensions.GetP3DStringLength(Name) + sizeof(uint) + sizeof(float) + sizeof(float) + sizeof(uint);

    [System.Diagnostics.CodeAnalysis.SuppressMessage("Style", "IDE0059:Unnecessary assignment of a value", Justification = "We want to read the value to progress the BinaryReader, but not set the value anywhere because it's calculated dynamically.")]
    public MultiControllerChunk(EndianAwareBinaryReader br) : this(br.ReadP3DString(), br.ReadUInt32(), br.ReadSingle(), br.ReadSingle())
    {
        var numTracks = br.ReadUInt32();
    }

    public MultiControllerChunk(string name, uint version, float length, float framerate) : base(ChunkID, name)
    {
        _version = version;
        _length = length;
        _framerate = framerate;
    }

    protected override void WriteData(EndianAwareBinaryWriter bw)
    {
        bw.WriteP3DString(Name);
        bw.Write(Version);
        bw.Write(Length);
        bw.Write(Framerate);
        bw.Write(NumTracks);
    }

    protected override Chunk CloneSelf() => new MultiControllerChunk(Name, Version, Length, Framerate);
}
