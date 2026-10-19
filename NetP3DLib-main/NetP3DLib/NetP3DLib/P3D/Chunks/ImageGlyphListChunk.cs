using NetP3DLib.IO;
using NetP3DLib.P3D.Attributes;
using NetP3DLib.P3D.Collections;
using NetP3DLib.P3D.Enums;
using NetP3DLib.P3D.Extensions;
using System;
using System.Collections.Generic;
using System.IO;

namespace NetP3DLib.P3D.Chunks;

[ChunkAttributes(ChunkID)]
public class ImageGlyphListChunk : Chunk
{
    public const ChunkIdentifier ChunkID = ChunkIdentifier.Image_Glyph_List;

    public uint NumGlyphs
    {
        get => (uint)(Glyphs?.Count ?? 0);
        set
        {
            if (value == NumGlyphs)
                return;

            if (value < NumGlyphs)
            {
                Glyphs.RemoveRange((int)value, (int)(NumGlyphs - value));
            }
            else
            {
                int count = (int)(value - NumGlyphs);
                var newGlyphs = new Glyph[count];

                for (var i = 0; i < count; i++)
                    newGlyphs[i] = new();

                Glyphs.AddRange(newGlyphs);
            }
        }
    }
    public SizeAwareList<Glyph> Glyphs { get; }

    public override byte[] DataBytes
    {
        get
        {
            var data = new List<byte>((int)DataLength);

            data.AddRange(BitConverter.GetBytes(NumGlyphs));
            foreach (var glyph in Glyphs)
                data.AddRange(glyph.DataBytes);

            return [.. data];
        }
    }
    public override uint DataLength => sizeof(uint) + Glyph.Size * NumGlyphs;

    public ImageGlyphListChunk(EndianAwareBinaryReader br) : this(br.ReadArray(() => new Glyph(br), out _))
    {
    }

    public ImageGlyphListChunk(IList<Glyph> glyphs) : base(ChunkID)
    {
        Glyphs = CreateSizeAwareList(glyphs, Glyphs_CollectionChanged);
    }
    
    private void Glyphs_CollectionChanged(object sender, System.Collections.Specialized.NotifyCollectionChangedEventArgs e)
    {
        OnPropertyChanged(nameof(Glyphs));

        if (e.OldItems != null)
            foreach (Glyph oldItem in e.OldItems)
                oldItem.PropertyChanged -= Glyphs_PropertyChanged;
    
        if (e.NewItems != null)
            foreach (Glyph newItem in e.NewItems)
                newItem.PropertyChanged += Glyphs_PropertyChanged;
    }
    
    private void Glyphs_PropertyChanged() => OnPropertyChanged(nameof(Glyphs));

    protected override void WriteData(EndianAwareBinaryWriter bw)
    {
        bw.Write(NumGlyphs);
        foreach (var glyph in Glyphs)
            glyph.Write(bw);
    }

    protected override Chunk CloneSelf()
    {
        var glyphs = new Glyph[Glyphs.Count];
        for (var i = 0; i < Glyphs.Count; i++)
            glyphs[i] = Glyphs[i].Clone();
        return new ImageGlyphListChunk(glyphs);
    }

    public class Glyph
    {
        public const uint Size = sizeof(ushort) + sizeof(ushort) + sizeof(ushort) + sizeof(ushort) + sizeof(ushort) + sizeof(uint);

        public Action? PropertyChanged;

        private ushort _xOrigin;
        public ushort XOrigin
        {
            get => _xOrigin;
            set
            {
                if (_xOrigin == value)
                    return;
    
                _xOrigin = value;
                PropertyChanged?.Invoke();
            }
        }
    
        private ushort _leftBearing;
        public ushort LeftBearing
        {
            get => _leftBearing;
            set
            {
                if (_leftBearing == value)
                    return;
    
                _leftBearing = value;
                PropertyChanged?.Invoke();
            }
        }
    
        private ushort _rightBearing;
        public ushort RightBearing
        {
            get => _rightBearing;
            set
            {
                if (_rightBearing == value)
                    return;
    
                _rightBearing = value;
                PropertyChanged?.Invoke();
            }
        }
    
        private ushort _width;
        public ushort Width
        {
            get => _width;
            set
            {
                if (_width == value)
                    return;
    
                _width = value;
                PropertyChanged?.Invoke();
            }
        }
    
        private ushort _advance;
        public ushort Advance
        {
            get => _advance;
            set
            {
                if (_advance == value)
                    return;
    
                _advance = value;
                PropertyChanged?.Invoke();
            }
        }
    
        private uint _code;
        public uint Code
        {
            get => _code;
            set
            {
                if (_code == value)
                    return;
    
                _code = value;
                PropertyChanged?.Invoke();
            }
        }

        public byte[] DataBytes
        {
            get
            {
                var data = new List<byte>((int)Size);

                data.AddRange(BitConverter.GetBytes(XOrigin));
                data.AddRange(BitConverter.GetBytes(LeftBearing));
                data.AddRange(BitConverter.GetBytes(RightBearing));
                data.AddRange(BitConverter.GetBytes(Width));
                data.AddRange(BitConverter.GetBytes(Advance));
                data.AddRange(BitConverter.GetBytes(Code));

                return [.. data];
            }
        }

        public Glyph(BinaryReader br)
        {
            _xOrigin = br.ReadUInt16();
            _leftBearing = br.ReadUInt16();
            _rightBearing = br.ReadUInt16();
            _width = br.ReadUInt16();
            _advance = br.ReadUInt16();
            _code = br.ReadUInt32();
        }

        public Glyph(ushort xOrigin, ushort leftBearing, ushort rightBearing, ushort width, ushort advance, uint code)
        {
            _xOrigin = xOrigin;
            _leftBearing = leftBearing;
            _rightBearing = rightBearing;
            _width = width;
            _advance = advance;
            _code = code;
        }

        public Glyph()
        {
            _xOrigin = 0;
            _leftBearing = 0;
            _rightBearing = 0;
            _width = 0;
            _advance = 0;
            _code = 0;
        }

        internal void Write(BinaryWriter bw)
        {
            bw.Write(XOrigin);
            bw.Write(LeftBearing);
            bw.Write(RightBearing);
            bw.Write(Width);
            bw.Write(Advance);
            bw.Write(Code);
        }

        internal Glyph Clone() => new(XOrigin, LeftBearing, RightBearing, Width, Advance, Code);

        public override string ToString() => $"{XOrigin} | {LeftBearing} | {RightBearing} | {Width} | {Advance} | {Code}";
    }
}
