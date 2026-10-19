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
public class CollisionMeshTriangleListChunk : Chunk
{
    public const ChunkIdentifier ChunkID = ChunkIdentifier.Collision_Mesh_Triangle_list;

    public uint NumTriangles
    {
        get => (uint)(Triangles?.Count ?? 0);
        set
        {
            if (value == NumTriangles)
                return;

            if (value < NumTriangles)
            {
                Triangles.RemoveRange((int)value, (int)(NumTriangles - value));
            }
            else
            {
                int count = (int)(value - NumTriangles);
                var newTriangles = new Triangle[count];

                for (var i = 0; i < count; i++)
                    newTriangles[i] = new();

                Triangles.AddRange(newTriangles);
            }
        }
    }
    public SizeAwareList<Triangle> Triangles { get; }

    public override byte[] DataBytes
    {
        get
        {
            var data = new List<byte>((int)DataLength);

            data.AddRange(BitConverter.GetBytes(NumTriangles));
            foreach (var entry in Triangles)
                data.AddRange(entry.DataBytes);

            return [.. data];
        }
    }
    public override uint DataLength => sizeof(uint) + Triangle.Size * NumTriangles;

    public CollisionMeshTriangleListChunk(EndianAwareBinaryReader br) : this(br.ReadArray(() => new Triangle(br), out _))
    {
    }

    public CollisionMeshTriangleListChunk(IList<Triangle> triangles) : base(ChunkID)
    {
        Triangles = CreateSizeAwareList(triangles, Triangles_CollectionChanged);
    }
    
    private void Triangles_CollectionChanged(object sender, System.Collections.Specialized.NotifyCollectionChangedEventArgs e)
    {
        OnPropertyChanged(nameof(Triangles));

        if (e.OldItems != null)
            foreach (Triangle oldItem in e.OldItems)
                oldItem.PropertyChanged -= Triangles_PropertyChanged;
    
        if (e.NewItems != null)
            foreach (Triangle newItem in e.NewItems)
                newItem.PropertyChanged += Triangles_PropertyChanged;
    }
    
    private void Triangles_PropertyChanged() => OnPropertyChanged(nameof(Triangles));

    protected override void WriteData(EndianAwareBinaryWriter bw)
    {
        bw.Write(NumTriangles);
        foreach (var entry in Triangles)
            entry.Write(bw);
    }

    protected override Chunk CloneSelf()
    {
        var triangles = new Triangle[Triangles.Count];
        for (var i = 0; i < Triangles.Count; i++)
            triangles[i] = Triangles[i].Clone();
        return new CollisionMeshTriangleListChunk(triangles);
    }

    public class Triangle
    {
        public const uint Size = sizeof(ushort) + sizeof(ushort) + sizeof(ushort) + sizeof(ushort);

        public event Action? PropertyChanged;

        private ushort _index0;
        public ushort Index0
        {
            get => _index0;
            set
            {
                if (_index0 == value)
                    return;
    
                _index0 = value;
                PropertyChanged?.Invoke();
            }
        }
    
        private ushort _index1;
        public ushort Index1
        {
            get => _index1;
            set
            {
                if (_index1 == value)
                    return;
    
                _index1 = value;
                PropertyChanged?.Invoke();
            }
        }
    
        private ushort _index2;
        public ushort Index2
        {
            get => _index2;
            set
            {
                if (_index2 == value)
                    return;
    
                _index2 = value;
                PropertyChanged?.Invoke();
            }
        }
    
        private ushort _flags;
        public ushort Flags
        {
            get => _flags;
            set
            {
                if (_flags == value)
                    return;
    
                _flags = value;
                PropertyChanged?.Invoke();
            }
        }

        public byte[] DataBytes
        {
            get
            {
                var data = new List<byte>((int)Size);

                data.AddRange(BitConverter.GetBytes(Index0));
                data.AddRange(BitConverter.GetBytes(Index1));
                data.AddRange(BitConverter.GetBytes(Index2));
                data.AddRange(BitConverter.GetBytes(Flags));

                return [.. data];
            }
        }

        public Triangle(BinaryReader br)
        {
            _index0 = br.ReadUInt16();
            _index1 = br.ReadUInt16();
            _index2 = br.ReadUInt16();
            _flags = br.ReadUInt16();
        }

        public Triangle(ushort index0, ushort index1, ushort index2, ushort flags)
        {
            _index0 = index0;
            _index1 = index1;
            _index2 = index2;
            _flags = flags;
        }

        public Triangle()
        {
            _index0 = 0;
            _index1 = 0;
            _index2 = 0;
            _flags = 0;
        }

        internal void Write(BinaryWriter bw)
        {
            bw.Write(Index0);
            bw.Write(Index1);
            bw.Write(Index2);
            bw.Write(Flags);
        }

        internal Triangle Clone() => new(Index0, Index1, Index2, Flags);

        public override string ToString() => $"{Index0} | {Index1} | {Index2} | {Flags}";
    }
}
