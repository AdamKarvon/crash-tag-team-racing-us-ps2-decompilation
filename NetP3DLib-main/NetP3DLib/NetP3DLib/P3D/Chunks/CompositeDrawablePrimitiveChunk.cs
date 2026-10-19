using NetP3DLib.IO;
using NetP3DLib.P3D.Attributes;
using NetP3DLib.P3D.Enums;
using NetP3DLib.P3D.Extensions;
using System;
using System.Collections.Generic;

namespace NetP3DLib.P3D.Chunks;

[ChunkAttributes(ChunkID)]
public class CompositeDrawablePrimitiveChunk : NamedChunk
{
    public const ChunkIdentifier ChunkID = ChunkIdentifier.Composite_Drawable_Primitive;

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
    
    private uint _createInstance;
    public uint CreateInstance
    {
        get => _createInstance;
        set
        {
            if (_createInstance == value)
                return;
    
            _createInstance = value;
            OnPropertyChanged(nameof(CreateInstance));
        }
    }
    
    // TODO: Type enum
    private uint _type;
    public uint Type
    {
        get => _type;
        set
        {
            if (_type == value)
                return;
    
            _type = value;
            OnPropertyChanged(nameof(Type));
        }
    }
    
    private uint _skeletonJointID;
    public uint SkeletonJointID
    {
        get => _skeletonJointID;
        set
        {
            if (_skeletonJointID == value)
                return;
    
            _skeletonJointID = value;
            OnPropertyChanged(nameof(SkeletonJointID));
        }
    }
    

    public override byte[] DataBytes
    {
        get
        {
            var data = new List<byte>((int)DataLength);

            data.AddRange(BitConverter.GetBytes(Version));
            data.AddRange(BitConverter.GetBytes(CreateInstance));
            data.AddRange(BinaryExtensions.GetP3DStringBytes(Name));
            data.AddRange(BitConverter.GetBytes(Type));
            data.AddRange(BitConverter.GetBytes(SkeletonJointID));

            return [.. data];
        }
    }
    public override uint DataLength => sizeof(uint) + sizeof(uint) + BinaryExtensions.GetP3DStringLength(Name) + sizeof(uint) + sizeof(uint);

    public CompositeDrawablePrimitiveChunk(EndianAwareBinaryReader br) : this(br.ReadUInt32(), br.ReadUInt32(), br.ReadP3DString(), br.ReadUInt32(), br.ReadUInt32())
    {
    }

    public CompositeDrawablePrimitiveChunk(uint version, uint createInstance, string name, uint type, uint skeletonJointId) : base(ChunkID, name)
    {
        _version = version;
        _createInstance = createInstance;
        _type = type;
        _skeletonJointID = skeletonJointId;
    }

    protected override void WriteData(EndianAwareBinaryWriter bw)
    {
        bw.Write(Version);
        bw.Write(CreateInstance);
        bw.WriteP3DString(Name);
        bw.Write(Type);
        bw.Write(SkeletonJointID);
    }

    protected override Chunk CloneSelf() => new CompositeDrawablePrimitiveChunk(Version, CreateInstance, Name, Type, SkeletonJointID);
}
