using NetP3DLib.IO;
using NetP3DLib.P3D.Attributes;
using NetP3DLib.P3D.Collections;
using NetP3DLib.P3D.Enums;
using NetP3DLib.P3D.Exceptions;
using NetP3DLib.P3D.Extensions;
using System;
using System.Collections.Generic;

namespace NetP3DLib.P3D.Chunks;

[ChunkAttributes(ChunkID)]
public class LightGroupChunk : NamedChunk
{
    public const ChunkIdentifier ChunkID = ChunkIdentifier.Light_Group;

    public uint NumLights
    {
        get => (uint)(Lights?.Count ?? 0);
        set
        {
            if (value == NumLights)
                return;

            if (value < NumLights)
            {
                Lights.RemoveRange((int)value, (int)(NumLights - value));
            }
            else
            {
                int count = (int)(value - NumLights);
                var newLights = new string[count];

                for (var i = 0; i < count; i++)
                    newLights[i] = string.Empty;

                Lights.AddRange(newLights);
            }
        }
    }
    public SizeAwareList<string> Lights { get; }

    public override byte[] DataBytes
    {
        get
        {
            var data = new List<byte>((int)DataLength);

            data.AddRange(BinaryExtensions.GetP3DStringBytes(Name));
            data.AddRange(BitConverter.GetBytes(NumLights));
            foreach (var light in Lights)
                data.AddRange(BinaryExtensions.GetP3DStringBytes(light));

            return [.. data];
        }
    }
    public override uint DataLength
    {
        get
        {
            uint size = BinaryExtensions.GetP3DStringLength(Name) + sizeof(uint);

            if (Lights != null)
                foreach (var light in Lights)
                    size += BinaryExtensions.GetP3DStringLength(light);

            return size;
        }
    }

    public LightGroupChunk(EndianAwareBinaryReader br) : this(br.ReadP3DString(), br.ReadP3DStringArray(out _))
    {
    }

    public LightGroupChunk(string name, IList<string> lights) : base(ChunkID, name)
    {
        Lights = CreateSizeAwareList(lights, Lights_CollectionChanged);
    }
    
    private void Lights_CollectionChanged(object sender, System.Collections.Specialized.NotifyCollectionChangedEventArgs e) => OnPropertyChanged(nameof(Lights));

    public override IEnumerable<InvalidP3DException> ValidateChunk()
    {
        foreach (var error in base.ValidateChunk())
            yield return error;

        foreach (var light in Lights)
            if (!light.IsValidP3DString())
                yield return new InvalidP3DStringException(this, nameof(Lights), light);
    }

    protected override void WriteData(EndianAwareBinaryWriter bw)
    {
        bw.WriteP3DString(Name);
        bw.Write(NumLights);
        foreach (var light in Lights)
            bw.WriteP3DString(light);
    }

    protected override Chunk CloneSelf() => new LightGroupChunk(Name, Lights);
}
