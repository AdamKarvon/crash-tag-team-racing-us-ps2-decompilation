using NetP3DLib.IO;
using NetP3DLib.P3D.Attributes;
using NetP3DLib.P3D.Enums;
using NetP3DLib.P3D.Exceptions;
using NetP3DLib.P3D.Extensions;
using NetP3DLib.P3D.Types;
using System.Collections.Generic;
using System.Numerics;

namespace NetP3DLib.P3D.Chunks;

[ChunkAttributes(ChunkID)]
public class RoadSegmentChunk : NamedChunk
{
    public const ChunkIdentifier ChunkID = ChunkIdentifier.Road_Segment;

    private readonly P3DString _roadDataSegment;
    public string RoadDataSegment
    {
        get => _roadDataSegment?.Value ?? string.Empty;
        set => _roadDataSegment.Value = value;
    }
    
    private Matrix4x4 _transform;
    public Matrix4x4 Transform
    {
        get => _transform;
        set
        {
            if (_transform == value)
                return;
    
            _transform = value;
            OnPropertyChanged(nameof(Transform));
        }
    }
    
    private Matrix4x4 _scale;
    public Matrix4x4 Scale
    {
        get => _scale;
        set
        {
            if (_scale == value)
                return;
    
            _scale = value;
            OnPropertyChanged(nameof(Scale));
        }
    }
    

    public override byte[] DataBytes
    {
        get
        {
            var data = new List<byte>((int)DataLength);

            data.AddRange(BinaryExtensions.GetP3DStringBytes(Name));
            data.AddRange(BinaryExtensions.GetP3DStringBytes(RoadDataSegment));
            data.AddRange(BinaryExtensions.GetBytes(Transform));
            data.AddRange(BinaryExtensions.GetBytes(Scale));

            return [.. data];
        }
    }
    public override uint DataLength => BinaryExtensions.GetP3DStringLength(Name) + BinaryExtensions.GetP3DStringLength(RoadDataSegment) + sizeof(float) * 16 + sizeof(float) * 16;

    public RoadSegmentChunk(EndianAwareBinaryReader br) : this(br.ReadP3DString(), br.ReadP3DString(), br.ReadMatrix4x4(), br.ReadMatrix4x4())
    {
    }

    public RoadSegmentChunk(string name, string roadDataSegment, Matrix4x4 transform, Matrix4x4 scale) : base(ChunkID, name)
    {
        _roadDataSegment = new(this, roadDataSegment, nameof(RoadDataSegment));
        _transform = transform;
        _scale = scale;
    }

    public override IEnumerable<InvalidP3DException> ValidateChunk()
    {
        foreach (var error in base.ValidateChunk())
            yield return error;

        if (!RoadDataSegment.IsValidP3DString())
            yield return new InvalidP3DStringException(this, nameof(RoadDataSegment), RoadDataSegment);
    }

    protected override void WriteData(EndianAwareBinaryWriter bw)
    {
        bw.WriteP3DString(Name);
        bw.WriteP3DString(RoadDataSegment);
        bw.Write(Transform);
        bw.Write(Scale);
    }

    protected override Chunk CloneSelf() => new RoadSegmentChunk(Name, RoadDataSegment, Transform, Scale);
}
