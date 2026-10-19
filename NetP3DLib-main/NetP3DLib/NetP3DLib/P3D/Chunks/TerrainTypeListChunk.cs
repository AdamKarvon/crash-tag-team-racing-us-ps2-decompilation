using NetP3DLib.IO;
using NetP3DLib.P3D.Attributes;
using NetP3DLib.P3D.Collections;
using NetP3DLib.P3D.Enums;
using NetP3DLib.P3D.Extensions;
using System;
using System.Collections.Generic;
using System.ComponentModel;
using System.IO;

namespace NetP3DLib.P3D.Chunks;

[ChunkAttributes(ChunkID)]
public class TerrainTypeListChunk : Chunk
{
    public const ChunkIdentifier ChunkID = ChunkIdentifier.Terrain_Type_List;

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
    
    public uint NumTypes
    {
        get => (uint)(Types?.Count ?? 0);
        set
        {
            if (value == NumTypes)
                return;

            if (value < NumTypes)
            {
                Types.RemoveRange((int)value, (int)(NumTypes - value));
            }
            else
            {
                int count = (int)(value - NumTypes);
                var newTypes = new TerrainType[count];

                for (var i = 0; i < count; i++)
                    newTypes[i] = new();

                Types.AddRange(newTypes);
            }
        }
    }
    public SizeAwareList<TerrainType> Types { get; }

    public override byte[] DataBytes
    {
        get
        {
            var data = new List<byte>((int)DataLength);

            data.AddRange(BitConverter.GetBytes(Version));
            data.AddRange(BitConverter.GetBytes(NumTypes));
            foreach (var type in Types)
                data.Add(type.Value);

            return [.. data];
        }
    }
    public override uint DataLength => sizeof(uint) + sizeof(uint) + sizeof(byte) * NumTypes;

    public TerrainTypeListChunk(EndianAwareBinaryReader br) : this(br.ReadUInt32(), br.ReadArray(() => new TerrainType(br), out _))
    {
    }

    public TerrainTypeListChunk(uint version, IList<TerrainType> types) : base(ChunkID)
    {
        _version = version;
        Types = CreateSizeAwareList(types, Types_CollectionChanged);
    }
    
    private void Types_CollectionChanged(object sender, System.Collections.Specialized.NotifyCollectionChangedEventArgs e)
    {
        OnPropertyChanged(nameof(Types));

        if (e.OldItems != null)
            foreach (TerrainType oldItem in e.OldItems)
                oldItem.PropertyChanged -= Types_PropertyChanged;
    
        if (e.NewItems != null)
            foreach (TerrainType newItem in e.NewItems)
                newItem.PropertyChanged += Types_PropertyChanged;
    }
    
    private void Types_PropertyChanged() => OnPropertyChanged(nameof(Types));

    protected override void WriteData(EndianAwareBinaryWriter bw)
    {
        bw.Write(Version);
        bw.Write(NumTypes);
        foreach (var type in Types)
            type.Write(bw);
    }

    protected override Chunk CloneSelf() => new TerrainTypeListChunk(Version, Types);

    public class TerrainType
    {
        public enum Types : byte
        {
            /// <summary>
            /// Default road terrain. Also used for sidewalk. This is default. If not set, it's this.
            /// </summary>
            Road,
            /// <summary>
            /// Grass type terrain most everything else which isn't road or sidewalk.
            /// </summary>
            Grass,
            /// <summary>
            /// Sand type terrain.
            /// </summary>
            Sand,
            /// <summary>
            /// Loose gravel type terrain.
            /// </summary>
            Gravel,
            /// <summary>
            /// Water on surface type terrain.
            /// </summary>
            Water,
            /// <summary>
            /// Boardwalks, docks type terrain.
            /// </summary>
            Wood,
            /// <summary>
            /// Powerplant and other structures.
            /// </summary>
            Metal,
            /// <summary>
            /// Dirt type terrain.
            /// </summary>
            Dirt
        }

        public event Action? PropertyChanged;

        private Types _type;
        public Types Type
        {
            get => _type;
            set
            {
                if (_type == value)
                    return;
    
                _type = value;
                PropertyChanged?.Invoke();
            }
        }
    
        private bool _interior;
        public bool Interior
        {
            get => _interior;
            set
            {
                if (_interior == value)
                    return;
    
                _interior = value;
                PropertyChanged?.Invoke();
            }
        }
    
        internal byte Value
        {
            get
            {
                var value = (byte)Type;
                if (Interior)
                    value |= 0x80;
                return value;
            }
            set
            {
                _type = (Types)(value & ~0x80);
                _interior = (value & 0x80) == 0x80;
            }
        }

        public TerrainType(BinaryReader br)
        {
            Value = br.ReadByte();
        }

        public TerrainType(Types type, bool interior)
        {
            _type = type;
            _interior = interior;
        }

        public TerrainType()
        {
            _type = Types.Road;
            _interior = false;
        }

        internal void Write(BinaryWriter bw)
        {
            bw.Write(Value);
        }

        internal TerrainType Clone() => new(Type, Interior);

        public override string ToString() => $"{Type} | {Interior}";
    }
}
