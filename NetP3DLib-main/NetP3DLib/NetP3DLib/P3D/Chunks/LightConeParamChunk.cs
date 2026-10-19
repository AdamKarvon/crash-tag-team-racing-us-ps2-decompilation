using NetP3DLib.IO;
using NetP3DLib.P3D.Attributes;
using NetP3DLib.P3D.Enums;
using System;
using System.Collections.Generic;

namespace NetP3DLib.P3D.Chunks;

[ChunkAttributes(ChunkID)]
public class LightConeParamChunk : Chunk
{
    public const ChunkIdentifier ChunkID = ChunkIdentifier.Light_Cone_Param;

    private float _phi;
    public float Phi
    {
        get => _phi;
        set
        {
            if (_phi == value)
                return;
    
            _phi = value;
            OnPropertyChanged(nameof(Phi));
        }
    }
    
    private float _theta;
    public float Theta
    {
        get => _theta;
        set
        {
            if (_theta == value)
                return;
    
            _theta = value;
            OnPropertyChanged(nameof(Theta));
        }
    }
    
    private float _falloff;
    public float Falloff
    {
        get => _falloff;
        set
        {
            if (_falloff == value)
                return;
    
            _falloff = value;
            OnPropertyChanged(nameof(Falloff));
        }
    }
    
    private float _range;
    public float Range
    {
        get => _range;
        set
        {
            if (_range == value)
                return;
    
            _range = value;
            OnPropertyChanged(nameof(Range));
        }
    }
    

    public override byte[] DataBytes
    {
        get
        {
            var data = new List<byte>((int)DataLength);

            data.AddRange(BitConverter.GetBytes(Phi));
            data.AddRange(BitConverter.GetBytes(Theta));
            data.AddRange(BitConverter.GetBytes(Falloff));
            data.AddRange(BitConverter.GetBytes(Range));

            return [.. data];
        }
    }
    public override uint DataLength => sizeof(float) + sizeof(float) + sizeof(float) + sizeof(float);

    public LightConeParamChunk(EndianAwareBinaryReader br) : this(br.ReadSingle(), br.ReadSingle(), br.ReadSingle(), br.ReadSingle())
    {
    }

    public LightConeParamChunk(float phi, float theta, float falloff, float range) : base(ChunkID)
    {
        _phi = phi;
        _theta = theta;
        _falloff = falloff;
        _range = range;
    }

    protected override void WriteData(EndianAwareBinaryWriter bw)
    {
        bw.Write(Phi);
        bw.Write(Theta);
        bw.Write(Falloff);
        bw.Write(Range);
    }

    protected override Chunk CloneSelf() => new LightConeParamChunk(Phi, Theta, Falloff, Range);
}
