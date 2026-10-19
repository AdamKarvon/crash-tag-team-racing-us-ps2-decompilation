using NetP3DLib.IO;
using NetP3DLib.P3D.Attributes;
using NetP3DLib.P3D.Collections;
using NetP3DLib.P3D.Extensions;
using System;
using System.Collections.Generic;

namespace NetP3DLib.P3D.UnknownChunks;

[ChunkAttributes(0x7000008)]
public class Unknown7000008Chunk : Chunk
{
    public uint NumUnknown
    {
        get => (uint)(Unknown?.Count ?? 0);
        set
        {
            if (value == NumUnknown)
                return;

            if (value < NumUnknown)
            {
                Unknown.RemoveRange((int)value, (int)(NumUnknown - value));
            }
            else
            {
                int count = (int)(value - NumUnknown);
                var newUnknown = new short[count];

                for (var i = 0; i < count; i++)
                    newUnknown[i] = default;

                Unknown.AddRange(newUnknown);
            }
        }
    }
    public SizeAwareList<short> Unknown { get; }

    public override byte[] DataBytes
    {
        get
        {
            var data = new List<byte>((int)DataLength);

            data.AddRange(BitConverter.GetBytes(NumUnknown));
            foreach (var unknown in Unknown)
                data.AddRange(BitConverter.GetBytes(unknown));

            return [.. data];
        }
    }
    public override uint DataLength => sizeof(uint) + sizeof(short) * NumUnknown;

    public Unknown7000008Chunk(EndianAwareBinaryReader br) : this(br.ReadInt16Array(out _))
    {
    }

    public Unknown7000008Chunk(IList<short> unknown) : base(0x7000008)
    {
        Unknown = CreateSizeAwareList(unknown, Unknown_CollectionChanged);
    }

    private void Unknown_CollectionChanged(object sender, System.Collections.Specialized.NotifyCollectionChangedEventArgs e) => OnPropertyChanged(nameof(Unknown));

    protected override void WriteData(EndianAwareBinaryWriter bw)
    {
        bw.Write(NumUnknown);
        foreach (var unknown in Unknown)
            bw.Write(unknown);
    }

    protected override Chunk CloneSelf() => new Unknown7000008Chunk(Unknown);
}