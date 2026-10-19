using NetP3DLib.IO;
using NetP3DLib.P3D.Attributes;
using NetP3DLib.P3D.Collections;
using NetP3DLib.P3D.Enums;
using NetP3DLib.P3D.Exceptions;
using System;
using System.Collections.Generic;

namespace NetP3DLib.P3D.Chunks;

[ChunkAttributes(ChunkID)]
public class ImageDataChunk : Chunk
{
    public const ChunkIdentifier ChunkID = ChunkIdentifier.Image_Data;

    private readonly Action _imageDataChanged;
    private ObservableByteArray _imageData;
    /// <summary>
    /// Property <c>Data</c> is the chunk's header data.
    /// </summary>
    public ObservableByteArray ImageData
    {
        get => _imageData;
        set
        {
            if (ReferenceEquals(_imageData, value))
                return;

            var oldSize = HeaderSize;
            if (value is ObservableByteArray oba)
                _imageData = new(oba.RawArray, _imageDataChanged);
            else
                _imageData = new(value?.ToArray() ?? [], _imageDataChanged);
            RecalculateSize(oldSize);
            _imageDataChanged();
        }
    }

    public override byte[] DataBytes
    {
        get
        {
            var data = new List<byte>((int)DataLength);

            data.AddRange(BitConverter.GetBytes(ImageData.Length));
            data.AddRange([.. ImageData]);

            return [.. data];
        }
    }
    public override uint DataLength => sizeof(uint) + (uint)ImageData.Length;

    public ImageDataChunk(EndianAwareBinaryReader br) : this(br.ReadBytes(br.ReadInt32()))
    {
    }

    public ImageDataChunk(byte[] imageData) : base(ChunkID)
    {
        _imageDataChanged = () => OnPropertyChanged(nameof(ImageData));
        _imageData = new((byte[])imageData.Clone(), _imageDataChanged);
    }

    public override IEnumerable<InvalidP3DException> ValidateChunk()
    {
        foreach (var error in base.ValidateChunk())
            yield return error;

        if (ImageData.Length > int.MaxValue)
            yield return new InvalidP3DException(this, $"The max length of {nameof(ImageData)} is {int.MaxValue} bytes.");
    }

    protected override void WriteData(EndianAwareBinaryWriter bw)
    {
        bw.Write(ImageData.Length);
        bw.Write(ImageData.RawArray);
    }

    protected override Chunk CloneSelf() => new ImageDataChunk(ImageData.RawArray);
}