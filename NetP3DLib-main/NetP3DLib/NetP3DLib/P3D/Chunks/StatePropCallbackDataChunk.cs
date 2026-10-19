using NetP3DLib.IO;
using NetP3DLib.P3D.Attributes;
using NetP3DLib.P3D.Enums;
using NetP3DLib.P3D.Extensions;
using System;
using System.Collections.Generic;

namespace NetP3DLib.P3D.Chunks;

[ChunkAttributes(ChunkID)]
public class StatePropCallbackDataChunk : NamedChunk
{
    public const ChunkIdentifier ChunkID = ChunkIdentifier.State_Prop_Callback_Data;

    public enum Events : int
    {
        StateChange = -1,
        RemoveFromWorld,
        Spawn5Coins,
        RemoveCollisionVolume,
        FireEnergyBolt,
        KillSpeed,
        Spawn10Coins,
        Spawn15Coins,
        Spawn20Coins,
        RadiateForce,
        EmitLeaves,
        ObjectDestroyed,
        Spawn5CoinsZ,
        Spawn1Coin,
        ColaDestroyed,
        CamShake,
        RemoveFirstCollisionVolume,
        RemoveSecondCollisionVolume,
        RemoveThirdCollisionVolume,
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
    
    private float _onFrame;
    public float OnFrame
    {
        get => _onFrame;
        set
        {
            if (_onFrame == value)
                return;
    
            _onFrame = value;
            OnPropertyChanged(nameof(OnFrame));
        }
    }
    

    public override byte[] DataBytes
    {
        get
        {
            var data = new List<byte>((int)DataLength);

            data.AddRange(BinaryExtensions.GetP3DStringBytes(Name));
            data.AddRange(BitConverter.GetBytes((int)Event));
            data.AddRange(BitConverter.GetBytes(OnFrame));

            return [.. data];
        }
    }
    public override uint DataLength => BinaryExtensions.GetP3DStringLength(Name) + sizeof(int) + sizeof(float);

    public StatePropCallbackDataChunk(EndianAwareBinaryReader br) : this(br.ReadP3DString(), (Events)br.ReadUInt32(), br.ReadSingle())
    {
    }

    public StatePropCallbackDataChunk(string name, Events @event, float onFrame) : base(ChunkID, name)
    {
        _event = @event;
        _onFrame = onFrame;
    }

    protected override void WriteData(EndianAwareBinaryWriter bw)
    {
        bw.WriteP3DString(Name);
        bw.Write((int)Event);
        bw.Write(OnFrame);
    }

    protected override Chunk CloneSelf() => new StatePropCallbackDataChunk(Name, Event, OnFrame);
}
