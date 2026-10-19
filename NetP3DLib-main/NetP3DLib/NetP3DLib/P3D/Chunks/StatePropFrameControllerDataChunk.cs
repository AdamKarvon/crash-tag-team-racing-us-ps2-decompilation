using NetP3DLib.IO;
using NetP3DLib.P3D.Attributes;
using NetP3DLib.P3D.Enums;
using NetP3DLib.P3D.Extensions;
using System;
using System.Collections.Generic;

namespace NetP3DLib.P3D.Chunks;

[ChunkAttributes(ChunkID)]
public class StatePropFrameControllerDataChunk : NamedChunk
{
    public const ChunkIdentifier ChunkID = ChunkIdentifier.State_Prop_Frame_Controller_Data;

    private uint _cyclic;
    public bool Cyclic
    {
        get => _cyclic != 0;
        set
        {
            if (Cyclic == value)
                return;

            _cyclic = value ? 1u : 0u;
            OnPropertyChanged(nameof(Cyclic));
        }
    }
    
    private uint _numCycles;
    public uint NumCycles
    {
        get => _numCycles;
        set
        {
            if (_numCycles == value)
                return;
    
            _numCycles = value;
            OnPropertyChanged(nameof(NumCycles));
        }
    }
    
    private uint _holdFrame;
    public bool HoldFrame
    {
        get => _holdFrame != 0;
        set
        {
            if (HoldFrame == value)
                return;

            _holdFrame = value ? 1u : 0u;
            OnPropertyChanged(nameof(HoldFrame));
        }
    }
    
    private float _minFrame;
    public float MinFrame
    {
        get => _minFrame;
        set
        {
            if (_minFrame == value)
                return;
    
            _minFrame = value;
            OnPropertyChanged(nameof(MinFrame));
        }
    }
    
    private float _maxFrame;
    public float MaxFrame
    {
        get => _maxFrame;
        set
        {
            if (_maxFrame == value)
                return;
    
            _maxFrame = value;
            OnPropertyChanged(nameof(MaxFrame));
        }
    }
    
    private float _relativeSpeed;
    public float RelativeSpeed
    {
        get => _relativeSpeed;
        set
        {
            if (_relativeSpeed == value)
                return;
    
            _relativeSpeed = value;
            OnPropertyChanged(nameof(RelativeSpeed));
        }
    }
    

    public override byte[] DataBytes
    {
        get
        {
            var data = new List<byte>((int)DataLength);

            data.AddRange(BinaryExtensions.GetP3DStringBytes(Name));
            data.AddRange(BitConverter.GetBytes(_cyclic));
            data.AddRange(BitConverter.GetBytes(NumCycles));
            data.AddRange(BitConverter.GetBytes(_holdFrame));
            data.AddRange(BitConverter.GetBytes(MinFrame));
            data.AddRange(BitConverter.GetBytes(MaxFrame));
            data.AddRange(BitConverter.GetBytes(RelativeSpeed));

            return [.. data];
        }
    }
    public override uint DataLength => BinaryExtensions.GetP3DStringLength(Name) + sizeof(uint) + sizeof(uint) + sizeof(uint) + sizeof(float) + sizeof(float) + sizeof(float);

    public StatePropFrameControllerDataChunk(EndianAwareBinaryReader br) : this(br.ReadP3DString(), br.ReadUInt32(), br.ReadUInt32(), br.ReadUInt32(), br.ReadSingle(), br.ReadSingle(), br.ReadSingle())
    {
    }

    public StatePropFrameControllerDataChunk(string name, bool cyclic, uint numCycles, bool holdFrame, float minFrame, float maxFrame, float relativeSpeed) : this(name, cyclic ? 1u : 0u, numCycles, holdFrame ? 1u : 0u, minFrame, maxFrame, relativeSpeed)
    {
    }

    public StatePropFrameControllerDataChunk(string name, uint cyclic, uint numCycles, uint holdFrame, float minFrame, float maxFrame, float relativeSpeed) : base(ChunkID, name)
    {
        _cyclic = cyclic;
        _numCycles = numCycles;
        _holdFrame = holdFrame;
        _minFrame = minFrame;
        _maxFrame = maxFrame;
        _relativeSpeed = relativeSpeed;
    }

    protected override void WriteData(EndianAwareBinaryWriter bw)
    {
        bw.WriteP3DString(Name);
        bw.Write(_cyclic);
        bw.Write(NumCycles);
        bw.Write(_holdFrame);
        bw.Write(MinFrame);
        bw.Write(MaxFrame);
        bw.Write(RelativeSpeed);
    }

    protected override Chunk CloneSelf() => new StatePropFrameControllerDataChunk(Name, Cyclic, NumCycles, HoldFrame, MinFrame, MaxFrame, RelativeSpeed);
}
