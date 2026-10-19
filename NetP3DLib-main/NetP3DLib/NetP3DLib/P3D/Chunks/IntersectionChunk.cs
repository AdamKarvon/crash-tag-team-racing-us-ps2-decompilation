using NetP3DLib.IO;
using NetP3DLib.P3D.Attributes;
using NetP3DLib.P3D.Enums;
using NetP3DLib.P3D.Exceptions;
using NetP3DLib.P3D.Extensions;
using System;
using System.Collections.Generic;
using System.Numerics;

namespace NetP3DLib.P3D.Chunks;

[ChunkAttributes(ChunkID)]
public class IntersectionChunk : NamedChunk
{
    public const ChunkIdentifier ChunkID = ChunkIdentifier.Intersection;

    public enum TrafficBehaviours : uint
    {
        NoStop,
        NWay,
        FourWay,
        NoStopN,
        NWayN,
    }

    private Vector3 _position;
    public Vector3 Position
    {
        get => _position;
        set
        {
            if (_position == value)
                return;
    
            _position = value;
            OnPropertyChanged(nameof(Position));
        }
    }
    
    private float _radius;
    public float Radius
    {
        get => _radius;
        set
        {
            if (_radius == value)
                return;
    
            _radius = value;
            OnPropertyChanged(nameof(Radius));
        }
    }
    
    private TrafficBehaviours _trafficBehaviour;
    public TrafficBehaviours TrafficBehaviour
    {
        get => _trafficBehaviour;
        set
        {
            if (_trafficBehaviour == value)
                return;
    
            _trafficBehaviour = value;
            OnPropertyChanged(nameof(TrafficBehaviour));
        }
    }
    

    public override byte[] DataBytes
    {
        get
        {
            var data = new List<byte>((int)DataLength);

            data.AddRange(BinaryExtensions.GetP3DStringBytes(Name));
            data.AddRange(BinaryExtensions.GetBytes(Position));
            data.AddRange(BitConverter.GetBytes(Radius));
            data.AddRange(BitConverter.GetBytes((uint)TrafficBehaviour));

            return [.. data];
        }
    }
    public override uint DataLength => BinaryExtensions.GetP3DStringLength(Name) + sizeof(float) * 3 + sizeof(float) + sizeof(uint);

    public IntersectionChunk(EndianAwareBinaryReader br) : this(br.ReadP3DString(), br.ReadVector3(), br.ReadSingle(), (TrafficBehaviours)br.ReadUInt32())
    {
    }

    public IntersectionChunk(string name, Vector3 position, float radius, TrafficBehaviours trafficBehaviour) : base(ChunkID, name)
    {
        _position = position;
        _radius = radius;
        _trafficBehaviour = trafficBehaviour;
    }

    public override IEnumerable<InvalidP3DException> ValidateChunk()
    {
        foreach (var error in base.ValidateChunk())
            yield return error;

        if (TrafficBehaviour != TrafficBehaviours.NoStop && TrafficBehaviour != TrafficBehaviours.NWay)
            yield return new InvalidP3DException(this, $"The Simpsons: Hit & Run only supports {TrafficBehaviours.NoStop} and {TrafficBehaviours.NWay}. {TrafficBehaviour} will default to {TrafficBehaviours.NoStop}.");
    }

    protected override void WriteData(EndianAwareBinaryWriter bw)
    {
        bw.WriteP3DString(Name);
        bw.Write(Position);
        bw.Write(Radius);
        bw.Write((uint)TrafficBehaviour);
    }

    protected override Chunk CloneSelf() => new IntersectionChunk(Name, Position, Radius, TrafficBehaviour);
}
