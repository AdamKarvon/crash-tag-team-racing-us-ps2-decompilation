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
public class TopologyChunk : Chunk
{
    public const ChunkIdentifier ChunkID = ChunkIdentifier.Topology;

    public uint NumTopology
    {
        get => (uint)(Topologies?.Count ?? 0);
        set
        {
            if (value == NumTopology)
                return;

            if (value < NumTopology)
            {
                Topologies.RemoveRange((int)value, (int)(NumTopology - value));
            }
            else
            {
                int count = (int)(value - NumTopology);
                var newTopologies = new Topology[count];

                for (var i = 0; i < count; i++)
                    newTopologies[i] = new();

                Topologies.AddRange(newTopologies);
            }
        }
    }
    public SizeAwareList<Topology> Topologies { get; }

    public override byte[] DataBytes
    {
        get
        {
            var data = new List<byte>((int)DataLength);

            data.AddRange(BitConverter.GetBytes(NumTopology));
            foreach (var entry in Topologies)
                data.AddRange(entry.DataBytes);

            return [.. data];
        }
    }
    public override uint DataLength => sizeof(uint) + Topology.Size * NumTopology;

    public TopologyChunk(EndianAwareBinaryReader br) : this(br.ReadArray(() => new Topology(br), out _))
    {
    }

    public TopologyChunk(IList<Topology> topologies) : base(ChunkID)
    {
        Topologies = CreateSizeAwareList(topologies, Topologies_CollectionChanged);
    }
    
    private void Topologies_CollectionChanged(object sender, System.Collections.Specialized.NotifyCollectionChangedEventArgs e)
    {
        OnPropertyChanged(nameof(Topologies));

        if (e.OldItems != null)
            foreach (Topology oldItem in e.OldItems)
                oldItem.PropertyChanged -= Topologies_PropertyChanged;
    
        if (e.NewItems != null)
            foreach (Topology newItem in e.NewItems)
                newItem.PropertyChanged += Topologies_PropertyChanged;
    }
    
    private void Topologies_PropertyChanged() => OnPropertyChanged(nameof(Topologies));

    protected override void WriteData(EndianAwareBinaryWriter bw)
    {
        bw.Write(NumTopology);
        foreach (var entry in Topologies)
            entry.Write(bw);
    }

    protected override Chunk CloneSelf()
    {
        var topologies = new Topology[Topologies.Count];
        for (var i = 0; i < Topologies.Count; i++)
            topologies[i] = Topologies[i].Clone();
        return new TopologyChunk(topologies);
    }

    public class Topology
    {
        public const uint Size = sizeof(ushort) + sizeof(ushort) + sizeof(ushort) + sizeof(ushort) + sizeof(ushort) + sizeof(ushort);

        public event Action? PropertyChanged;

        private ushort _v0;
        public ushort V0
        {
            get => _v0;
            set
            {
                if (_v0 == value)
                    return;
    
                _v0 = value;
                PropertyChanged?.Invoke();
            }
        }
    
        private ushort _v1;
        public ushort V1
        {
            get => _v1;
            set
            {
                if (_v1 == value)
                    return;
    
                _v1 = value;
                PropertyChanged?.Invoke();
            }
        }
    
        private ushort _v2;
        public ushort V2
        {
            get => _v2;
            set
            {
                if (_v2 == value)
                    return;
    
                _v2 = value;
                PropertyChanged?.Invoke();
            }
        }
    
        private ushort _n0;
        public ushort N0
        {
            get => _n0;
            set
            {
                if (_n0 == value)
                    return;
    
                _n0 = value;
                PropertyChanged?.Invoke();
            }
        }
    
        private ushort _n1;
        public ushort N1
        {
            get => _n1;
            set
            {
                if (_n1 == value)
                    return;
    
                _n1 = value;
                PropertyChanged?.Invoke();
            }
        }
    
        private ushort _n2;
        public ushort N2
        {
            get => _n2;
            set
            {
                if (_n2 == value)
                    return;
    
                _n2 = value;
                PropertyChanged?.Invoke();
            }
        }

        public byte[] DataBytes
        {
            get
            {
                var data = new List<byte>((int)Size);

                data.AddRange(BitConverter.GetBytes(V0));
                data.AddRange(BitConverter.GetBytes(V1));
                data.AddRange(BitConverter.GetBytes(V2));
                data.AddRange(BitConverter.GetBytes(N0));
                data.AddRange(BitConverter.GetBytes(N1));
                data.AddRange(BitConverter.GetBytes(N2));

                return [.. data];
            }
        }

        public Topology(BinaryReader br)
        {
            _v0 = br.ReadUInt16();
            _v1 = br.ReadUInt16();
            _v2 = br.ReadUInt16();
            _n0 = br.ReadUInt16();
            _n1 = br.ReadUInt16();
            _n2 = br.ReadUInt16();
        }

        public Topology(ushort v0, ushort v1, ushort v2, ushort n0, ushort n1, ushort n2)
        {
            _v0 = v0;
            _v1 = v1;
            _v2 = v2;
            _n0 = n0;
            _n1 = n1;
            _n2 = n2;
        }

        public Topology()
        {
            _v0 = 0;
            _v1 = 0;
            _v2 = 0;
            _n0 = 0;
            _n1 = 0;
            _n2 = 0;
        }

        internal void Write(BinaryWriter bw)
        {
            bw.Write(V0);
            bw.Write(V1);
            bw.Write(V2);
            bw.Write(N0);
            bw.Write(N1);
            bw.Write(N2);
        }

        internal Topology Clone() => new(V0, V1, V2, N0, N1, N2);

        public override string ToString() => $"{V0} | {V1} | {V2} | {N0} | {N1} | {N2}";
    }
}
