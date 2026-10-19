using NetP3DLib.IO;
using NetP3DLib.P3D.Attributes;
using NetP3DLib.P3D.Enums;
using NetP3DLib.P3D.Extensions;
using System;
using System.Collections.Generic;

namespace NetP3DLib.P3D.Chunks;

[ChunkAttributes(ChunkID)]
public class StatePropEventDataChunk : NamedChunk
{
    public const ChunkIdentifier ChunkID = ChunkIdentifier.State_Prop_Event_Data;

    public enum Events : uint
    {
        Idle,
        FadeIn,
        FadeOut,
        Moving,
        AttackCharging,
        AttackCharged,
        Attacking,
        Destroyed,
        Hit,
        FeatherTouch,
        Stomp,
        VehicleHit,
    }

    private uint _toState;
    public uint ToState
    {
        get => _toState;
        set
        {
            if (_toState == value)
                return;
    
            _toState = value;
            OnPropertyChanged(nameof(ToState));
        }
    }
    
    private Events _event;
    public Events Event
    {
        get => _event;
        set
        {
            if (_event == value)
                return;
    
            _event = value;
            OnPropertyChanged(nameof(Event));
        }
    }
    

    public override byte[] DataBytes
    {
        get
        {
            var data = new List<byte>((int)DataLength);

            data.AddRange(BinaryExtensions.GetP3DStringBytes(Name));
            data.AddRange(BitConverter.GetBytes(ToState));
            data.AddRange(BitConverter.GetBytes((uint)Event));

            return [.. data];
        }
    }
    public override uint DataLength => BinaryExtensions.GetP3DStringLength(Name) + sizeof(uint) + sizeof(uint);

    public StatePropEventDataChunk(EndianAwareBinaryReader br) : this(br.ReadP3DString(), br.ReadUInt32(), (Events)br.ReadUInt32())
    {
    }

    public StatePropEventDataChunk(string name, uint toState, Events @event) : base(ChunkID, name)
    {
        _toState = toState;
        _event = @event;
    }

    protected override void WriteData(EndianAwareBinaryWriter bw)
    {
        bw.WriteP3DString(Name);
        bw.Write(ToState);
        bw.Write((uint)Event);
    }

    protected override Chunk CloneSelf() => new StatePropEventDataChunk(Name, ToState, Event);
}
