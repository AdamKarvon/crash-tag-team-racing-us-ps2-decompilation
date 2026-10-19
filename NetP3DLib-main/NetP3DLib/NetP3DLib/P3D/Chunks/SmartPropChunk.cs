using NetP3DLib.IO;
using NetP3DLib.P3D.Attributes;
using NetP3DLib.P3D.Enums;
using NetP3DLib.P3D.Exceptions;
using NetP3DLib.P3D.Extensions;
using NetP3DLib.P3D.Types;
using System;
using System.Collections.Generic;

namespace NetP3DLib.P3D.Chunks;

[ChunkAttributes(ChunkID)]
public class SmartPropChunk : NamedChunk
{
    public const ChunkIdentifier ChunkID = ChunkIdentifier.Smart_Prop;

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
    
    private readonly P3DString _objectFactoryName;
    public string ObjectFactoryName
    {
        get => _objectFactoryName?.Value ?? string.Empty;
        set => _objectFactoryName.Value = value;
    }
    private readonly P3DString _material;
    public string Material
    {
        get => _material?.Value ?? string.Empty;
        set => _material.Value = value;
    }
    
    private uint _materialEnum;
    public uint MaterialEnum
    {
        get => _materialEnum;
        set
        {
            if (_materialEnum == value)
                return;
    
            _materialEnum = value;
            OnPropertyChanged(nameof(MaterialEnum));
        }
    }
    
    private uint _numBreakables;
    public uint NumBreakables
    {
        get => _numBreakables;
        set
        {
            if (_numBreakables == value)
                return;
    
            _numBreakables = value;
            OnPropertyChanged(nameof(NumBreakables));
        }
    }
    
    private uint _renderingCost;
    public uint RenderingCost
    {
        get => _renderingCost;
        set
        {
            if (_renderingCost == value)
                return;
    
            _renderingCost = value;
            OnPropertyChanged(nameof(RenderingCost));
        }
    }
    
    private uint _simulationCost;
    public uint SimulationCost
    {
        get => _simulationCost;
        set
        {
            if (_simulationCost == value)
                return;
    
            _simulationCost = value;
            OnPropertyChanged(nameof(SimulationCost));
        }
    }
    
    private uint _numStates;
    public uint NumStates
    {
        get => _numStates;
        set
        {
            if (_numStates == value)
                return;
    
            _numStates = value;
            OnPropertyChanged(nameof(NumStates));
        }
    }
    

    public override byte[] DataBytes
    {
        get
        {
            var data = new List<byte>((int)DataLength);

            data.AddRange(BitConverter.GetBytes(Version));
            data.AddRange(BinaryExtensions.GetP3DStringBytes(Name));
            data.AddRange(BinaryExtensions.GetP3DStringBytes(ObjectFactoryName));
            data.AddRange(BinaryExtensions.GetP3DStringBytes(Material));
            data.AddRange(BitConverter.GetBytes(MaterialEnum));
            data.AddRange(BitConverter.GetBytes(NumBreakables));
            data.AddRange(BitConverter.GetBytes(RenderingCost));
            data.AddRange(BitConverter.GetBytes(SimulationCost));
            data.AddRange(BitConverter.GetBytes(NumStates));

            return [.. data];
        }
    }
    public override uint DataLength => sizeof(uint) + BinaryExtensions.GetP3DStringLength(Name) + BinaryExtensions.GetP3DStringLength(ObjectFactoryName) + BinaryExtensions.GetP3DStringLength(Material) + sizeof(uint) + sizeof(uint) + sizeof(uint) + sizeof(uint) + sizeof(uint);

    public SmartPropChunk(EndianAwareBinaryReader br) : this(br.ReadUInt32(), br.ReadP3DString(), br.ReadP3DString(), br.ReadP3DString(), br.ReadUInt32(), br.ReadUInt32(), br.ReadUInt32(), br.ReadUInt32(), br.ReadUInt32())
    {
    }

    public SmartPropChunk(uint version, string name, string objectFactoryName, string material, uint materialEnum, uint numBreakables, uint renderingCost, uint simulationCost, uint numStates) : base(ChunkID, name)
    {
        _version = version;
        _objectFactoryName = new(this, objectFactoryName, nameof(ObjectFactoryName));
        _material = new(this, material, nameof(Material));
        _materialEnum = materialEnum;
        _numBreakables = numBreakables;
        _renderingCost = renderingCost;
        _simulationCost = simulationCost;
        _numStates = numStates;
    }

    public override IEnumerable<InvalidP3DException> ValidateChunk()
    {
        foreach (var error in base.ValidateChunk())
            yield return error;

        if (!ObjectFactoryName.IsValidP3DString())
            yield return new InvalidP3DStringException(this, nameof(ObjectFactoryName), ObjectFactoryName);

        if (!Material.IsValidP3DString())
            yield return new InvalidP3DStringException(this, nameof(Material), Material);
    }

    protected override void WriteData(EndianAwareBinaryWriter bw)
    {
        bw.Write(Version);
        bw.WriteP3DString(Name);
        bw.WriteP3DString(ObjectFactoryName);
        bw.WriteP3DString(Material);
        bw.Write(MaterialEnum);
        bw.Write(NumBreakables);
        bw.Write(RenderingCost);
        bw.Write(SimulationCost);
        bw.Write(NumStates);
    }

    protected override Chunk CloneSelf() => new SmartPropChunk(Version, Name, ObjectFactoryName, Material, MaterialEnum, NumBreakables, RenderingCost, SimulationCost, NumStates);
}
