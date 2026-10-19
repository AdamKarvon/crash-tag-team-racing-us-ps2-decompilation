using NetP3DLib.IO;
using NetP3DLib.P3D.Attributes;
using NetP3DLib.P3D.Collections;
using NetP3DLib.P3D.Enums;
using NetP3DLib.P3D.Extensions;
using System;
using System.Collections.Generic;
using System.IO;
using System.Numerics;

namespace NetP3DLib.P3D.Chunks;

[ChunkAttributes(ChunkID)]
public class TextureGlyphListChunk : Chunk
{
    public const ChunkIdentifier ChunkID = ChunkIdentifier.Texture_Glyph_List;

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

    public TextureGlyphListChunk(EndianAwareBinaryReader br) : this(br.ReadArray(() => new Glyph(br), out _))
    {
    }

    public TextureGlyphListChunk(IList<Glyph> glyphs) : base(ChunkID)
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
        return new TextureGlyphListChunk(glyphs);
    }

    public class Glyph
    {
        public const uint Size = sizeof(uint) + sizeof(float) * 2 + sizeof(float) * 2 + sizeof(float) + sizeof(float) + sizeof(float) + sizeof(float) + sizeof(uint);

        public event Action? PropertyChanged;

        private uint _textureNum;
        public uint TextureNum
        {
            get => _textureNum;
            set
            {
                if (_textureNum == value)
                    return;
    
                _textureNum = value;
                PropertyChanged?.Invoke();
            }
        }
    
        private Vector2 _bottomLeft;
        public Vector2 BottomLeft
        {
            get => _bottomLeft;
            set
            {
                if (_bottomLeft == value)
                    return;
    
                _bottomLeft = value;
                PropertyChanged?.Invoke();
            }
        }
    
        private Vector2 _topRight;
        public Vector2 TopRight
        {
            get => _topRight;
            set
            {
                if (_topRight == value)
                    return;
    
                _topRight = value;
                PropertyChanged?.Invoke();
            }
        }
    
        private float _leftBearing;
        public float LeftBearing
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
    
        private float _rightBearing;
        public float RightBearing
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
    
        private float _width;
        public float Width
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
    
        private float _advance;
        public float Advance
        {
            get => _advance;
            set
            {
                if (_advance == value)
                    return;

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

                data.AddRange(BitConverter.GetBytes(TextureNum));
                data.AddRange(BinaryExtensions.GetBytes(BottomLeft));
                data.AddRange(BinaryExtensions.GetBytes(TopRight));
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
            _textureNum = br.ReadUInt32();
            _bottomLeft = br.ReadVector2();
            _topRight = br.ReadVector2();
            _leftBearing = br.ReadSingle();
            _rightBearing = br.ReadSingle();
            _width = br.ReadSingle();
            _advance = br.ReadSingle();
            _code = br.ReadUInt32();
        }

        public Glyph(uint textureNum, Vector2 bottomLeft, Vector2 topRight, float leftBearing, float rightBearing, float width, float advance, uint code)
        {
            _textureNum = textureNum;
            _bottomLeft = bottomLeft;
            _topRight = topRight;
            _leftBearing = leftBearing;
            _rightBearing = rightBearing;
            _width = width;
            _advance = advance;
            _code = code;
        }

        public Glyph()
        {
            _textureNum = 0;
            _bottomLeft = Vector2.Zero;
            _topRight = Vector2.Zero;
            _leftBearing = 0;
            _rightBearing = 0;
            _width = 0;
            _advance = 0;
            _code = 0;
        }

        internal void Write(BinaryWriter bw)
        {
            bw.Write(TextureNum);
            bw.Write(BottomLeft);
            bw.Write(TopRight);
            bw.Write(LeftBearing);
            bw.Write(RightBearing);
            bw.Write(Width);
            bw.Write(Advance);
            bw.Write(Code);
        }

        internal Glyph Clone() => new(TextureNum, BottomLeft, TopRight, LeftBearing, RightBearing, Width, Advance, Code);

        public override string ToString() => $"{TextureNum} | {BottomLeft} | {TopRight} | {LeftBearing} | {RightBearing} | {Width} | {Advance} | {Code}";
    }
}
