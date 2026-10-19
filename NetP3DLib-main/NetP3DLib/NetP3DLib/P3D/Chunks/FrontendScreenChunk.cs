using NetP3DLib.IO;
using NetP3DLib.P3D.Attributes;
using NetP3DLib.P3D.Collections;
using NetP3DLib.P3D.Enums;
using NetP3DLib.P3D.Exceptions;
using NetP3DLib.P3D.Extensions;
using System;
using System.Collections.Generic;
using System.ComponentModel;

namespace NetP3DLib.P3D.Chunks;

[ChunkAttributes(ChunkID)]
public class FrontendScreenChunk : NamedChunk
{
    public const ChunkIdentifier ChunkID = ChunkIdentifier.Frontend_Screen;

    private uint _version;
    [DefaultValue(0)]
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
    
    public uint NumPageNames
    {
        get => (uint)(PageNames?.Count ?? 0);
        set
        {
            if (value == NumPageNames)
                return;

            if (value < NumPageNames)
            {
                PageNames.RemoveRange((int)value, (int)(NumPageNames - value));
            }
            else
            {
                int count = (int)(value - NumPageNames);
                var newPageNames = new string[count];

                for (var i = 0; i < count; i++)
                    newPageNames[i] = string.Empty;

                PageNames.AddRange(newPageNames);
            }
        }
    }
    public SizeAwareList<string> PageNames { get; }

    public override byte[] DataBytes
    {
        get
        {
            var data = new List<byte>((int)DataLength);

            data.AddRange(BinaryExtensions.GetP3DStringBytes(Name));
            data.AddRange(BitConverter.GetBytes(Version));
            data.AddRange(BitConverter.GetBytes(NumPageNames));
            foreach (var pageName in PageNames)
                data.AddRange(BinaryExtensions.GetP3DStringBytes(pageName));

            return [.. data];
        }
    }
    public override uint DataLength
    {
        get
        {
            uint size = BinaryExtensions.GetP3DStringLength(Name) + sizeof(uint) + sizeof(uint);

            if (PageNames != null)
                foreach (var pageName in PageNames)
                    size += BinaryExtensions.GetP3DStringLength(pageName);

            return size;
        }
    }

    public FrontendScreenChunk(EndianAwareBinaryReader br) : this(br.ReadP3DString(), br.ReadUInt32(), br.ReadP3DStringArray(out _))
    {
    }

    public FrontendScreenChunk(string name, uint version, IList<string> pageNames) : base(ChunkID, name)
    {
        _version = version;
        PageNames = CreateSizeAwareList(pageNames, PageNames_CollectionChanged);
    }
    
    private void PageNames_CollectionChanged(object sender, System.Collections.Specialized.NotifyCollectionChangedEventArgs e) => OnPropertyChanged(nameof(PageNames));

    public override IEnumerable<InvalidP3DException> ValidateChunk()
    {
        foreach (var error in base.ValidateChunk())
            yield return error;

        foreach (var pageName in PageNames)
            if (!pageName.IsValidP3DString())
                yield return new InvalidP3DStringException(this, nameof(PageNames), pageName);
    }

    protected override void WriteData(EndianAwareBinaryWriter bw)
    {
        bw.WriteP3DString(Name);
        bw.Write(Version);
        bw.Write(NumPageNames);
        foreach (var pageName in PageNames)
            bw.WriteP3DString(pageName);
    }

    protected override Chunk CloneSelf() => new FrontendScreenChunk(Name, Version, PageNames);
}
