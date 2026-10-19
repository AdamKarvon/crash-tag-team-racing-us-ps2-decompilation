using NetP3DLib.IO;
using NetP3DLib.P3D.Attributes;
using NetP3DLib.P3D.Collections;
using NetP3DLib.P3D.Enums;
using NetP3DLib.P3D.Exceptions;
using NetP3DLib.P3D.Extensions;
using System;
using System.Collections.Generic;
using System.IO;
using System.Numerics;

namespace NetP3DLib.P3D.Chunks;

[ChunkAttributes(ChunkID)]
public class PhotonMapChunk : NamedChunk
{
    public const ChunkIdentifier ChunkID = ChunkIdentifier.Photon_Map;

    private uint _version;
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
            NumLightScales = value;
        }
    }
    public SizeAwareList<string> Lights { get; }
    public uint NumLightScales
    {
        get => (uint)(LightScales?.Count ?? 0);
        set
        {
            if (value == NumLightScales)
                return;

            if (value < NumLightScales)
            {
                LightScales.RemoveRange((int)value, (int)(NumLightScales - value));
            }
            else
            {
                int count = (int)(value - NumLightScales);
                var newLightScales = new float[count];

                for (var i = 0; i < count; i++)
                    newLightScales[i] = default;

                LightScales.AddRange(newLightScales);
            }
            NumLights = value;
        }
    }
    public SizeAwareList<float> LightScales { get; }
    public uint NumPhotons
    {
        get => (uint)(Photons?.Count ?? 0);
        set
        {
            if (value == NumPhotons)
                return;

            if (value < NumPhotons)
            {
                Photons.RemoveRange((int)value, (int)(NumPhotons - value));
            }
            else
            {
                int count = (int)(value - NumPhotons);
                var newPhotons = new Photon[count];

                for (var i = 0; i < count; i++)
                    newPhotons[i] = new();

                Photons.AddRange(newPhotons);
            }
        }
    }
    public SizeAwareList<Photon> Photons { get; }

    public override byte[] DataBytes
    {
        get
        {
            var data = new List<byte>((int)DataLength);

            data.AddRange(BinaryExtensions.GetP3DStringBytes(Name));
            data.AddRange(BitConverter.GetBytes(Version));
            data.AddRange(BitConverter.GetBytes(NumLights));
            foreach (var light in Lights)
                data.AddRange(BinaryExtensions.GetP3DStringBytes(light));
            foreach (var lightScale in LightScales)
                data.AddRange(BitConverter.GetBytes(lightScale));
            data.AddRange(BitConverter.GetBytes(NumPhotons));
            foreach (var photon in Photons)
                data.AddRange(photon.DataBytes);

            return [.. data];
        }
    }
    public override uint DataLength
    {
        get
        {
            uint size = BinaryExtensions.GetP3DStringLength(Name) + sizeof(uint) + sizeof(uint) + sizeof(float) * NumLights + sizeof(uint) + Photon.Size * NumPhotons;

            if (Lights != null)
                foreach (var light in Lights)
                    size += BinaryExtensions.GetP3DStringLength(light);

            return size;
        }
    }

    public PhotonMapChunk(EndianAwareBinaryReader br) : this(br.ReadP3DString(), br.ReadUInt32(), br.ReadP3DStringArray(out var numLights), br.ReadSingleArray(numLights), br.ReadArray(() => new Photon(br), out _))
    {
    }

    public PhotonMapChunk(string name, uint version, IList<string> lights, IList<float> lightScales, IList<Photon> photons) : base(ChunkID, name)
    {
        _version = version;
        Lights = CreateSizeAwareList(lights, Lights_CollectionChanged);
        LightScales = CreateSizeAwareList(lightScales, LightScales_CollectionChanged);
        Photons = CreateSizeAwareList(photons, Photons_CollectionChanged);
    }
    
    private void Lights_CollectionChanged(object sender, System.Collections.Specialized.NotifyCollectionChangedEventArgs e) => OnPropertyChanged(nameof(Lights));
    
    private void LightScales_CollectionChanged(object sender, System.Collections.Specialized.NotifyCollectionChangedEventArgs e) => OnPropertyChanged(nameof(LightScales));
    
    private void Photons_CollectionChanged(object sender, System.Collections.Specialized.NotifyCollectionChangedEventArgs e)
    {
        OnPropertyChanged(nameof(Photons));

        if (e.OldItems != null)
            foreach (Photon oldItem in e.OldItems)
                oldItem.PropertyChanged -= Photons_PropertyChanged;
    
        if (e.NewItems != null)
            foreach (Photon newItem in e.NewItems)
                newItem.PropertyChanged += Photons_PropertyChanged;
    }
    
    private void Photons_PropertyChanged() => OnPropertyChanged(nameof(Photons));

    public override IEnumerable<InvalidP3DException> ValidateChunk()
    {
        foreach (var error in base.ValidateChunk())
            yield return error;

        if (Lights.Count != LightScales.Count)
            yield return new InvalidP3DException(this, $"{nameof(Lights)} and {nameof(LightScales)} must have equal counts.");
    }

    protected override void WriteData(EndianAwareBinaryWriter bw)
    {
        bw.WriteP3DString(Name);
        bw.Write(Version);
        bw.Write(NumLights);
        foreach (var light in Lights)
            bw.WriteP3DString(light);
        foreach (var lightScale in LightScales)
            bw.Write(lightScale);
        bw.Write(NumPhotons);
        foreach (var photon in Photons)
            photon.Write(bw);
    }

    protected override Chunk CloneSelf()
    {
        var photons = new Photon[Photons.Count];
        for (var i = 0; i < Photons.Count; i++)
            photons[i] = Photons[i].Clone();
        return new PhotonMapChunk(Name, Version, Lights, LightScales, photons);
    }

    public class Photon
    {
        public const uint Size = sizeof(float) * 3 + sizeof(short) + sizeof(byte) + sizeof(byte) + sizeof(float) + sizeof(float) + sizeof(float) + sizeof(float) + sizeof(byte) + sizeof(byte);

        public event Action? PropertyChanged;

        private Vector3 _position;
        public Vector3 Position
        {
            get => _position;
            set
            {
                if (_position == value)
                    return;
    
                _position = value;
                PropertyChanged?.Invoke();
            }
        }
    
        private short _plane;
        public short Plane
        {
            get => _plane;
            set
            {
                if (_plane == value)
                    return;
    
                _plane = value;
                PropertyChanged?.Invoke();
            }
        }
    
        private byte _theta;
        public byte Theta
        {
            get => _theta;
            set
            {
                if (_theta == value)
                    return;
    
                _theta = value;
                PropertyChanged?.Invoke();
            }
        }
    
        private byte _phi;
        public byte Phi
        {
            get => _phi;
            set
            {
                if (_phi == value)
                    return;
    
                _phi = value;
                PropertyChanged?.Invoke();
            }
        }
    
        private float _redPower;
        public float RedPower
        {
            get => _redPower;
            set
            {
                if (_redPower == value)
                    return;
    
                _redPower = value;
                PropertyChanged?.Invoke();
            }
        }
    
        private float _greenPower;
        public float GreenPower
        {
            get => _greenPower;
            set
            {
                if (_greenPower == value)
                    return;
    
                _greenPower = value;
                PropertyChanged?.Invoke();
            }
        }
    
        private float _bluePower;
        public float BluePower
        {
            get => _bluePower;
            set
            {
                if (_bluePower == value)
                    return;
    
                _bluePower = value;
                PropertyChanged?.Invoke();
            }
        }
    
        private float _alphaPower;
        public float AlphaPower
        {
            get => _alphaPower;
            set
            {
                if (_alphaPower == value)
                    return;
    
                _alphaPower = value;
                PropertyChanged?.Invoke();
            }
        }
    
        private byte _numScatterings;
        public byte NumScatterings
        {
            get => _numScatterings;
            set
            {
                if (_numScatterings == value)
                    return;
    
                _numScatterings = value;
                PropertyChanged?.Invoke();
            }
        }
    
        private byte _lightIndex;
        public byte LightIndex
        {
            get => _lightIndex;
            set
            {
                if (_lightIndex == value)
                    return;
    
                _lightIndex = value;
                PropertyChanged?.Invoke();
            }
        }

        public byte[] DataBytes
        {
            get
            {
                var data = new List<byte>((int)Size);

                data.AddRange(BinaryExtensions.GetBytes(Position));
                data.AddRange(BitConverter.GetBytes(Plane));
                data.Add(Theta);
                data.Add(Phi);
                data.AddRange(BitConverter.GetBytes(RedPower));
                data.AddRange(BitConverter.GetBytes(GreenPower));
                data.AddRange(BitConverter.GetBytes(BluePower));
                data.AddRange(BitConverter.GetBytes(AlphaPower));
                data.Add(NumScatterings);
                data.Add(LightIndex);

                return [.. data];
            }
        }

        public Photon(BinaryReader br)
        {
            _position = br.ReadVector3();
            _plane = br.ReadInt16();
            _theta = br.ReadByte();
            _phi = br.ReadByte();
            _redPower = br.ReadSingle();
            _greenPower = br.ReadSingle();
            _bluePower = br.ReadSingle();
            _alphaPower = br.ReadSingle();
            _numScatterings = br.ReadByte();
            _lightIndex = br.ReadByte();
        }

        public Photon(Vector3 position, short plane, byte theta, byte phi, float redPower, float greenPower, float bluePower, float alphaPower, byte numScatterings, byte lightIndex)
        {
            _position = position;
            _plane = plane;
            _theta = theta;
            _phi = phi;
            _redPower = redPower;
            _greenPower = greenPower;
            _bluePower = bluePower;
            _alphaPower = alphaPower;
            _numScatterings = numScatterings;
            _lightIndex = lightIndex;
        }

        public Photon()
        {
            _position = new();
            _plane = 0;
            _theta = 0;
            _phi = 0;
            _redPower = 0;
            _greenPower = 0;
            _bluePower = 0;
            _alphaPower = 0;
            _numScatterings = 0;
            _lightIndex = 0;
        }

        internal void Write(BinaryWriter bw)
        {
            bw.Write(Position);
            bw.Write(Plane);
            bw.Write(Theta);
            bw.Write(Phi);
            bw.Write(RedPower);
            bw.Write(GreenPower);
            bw.Write(BluePower);
            bw.Write(AlphaPower);
            bw.Write(NumScatterings);
            bw.Write(LightIndex);
        }

        internal Photon Clone() => new(Position, Plane, Theta, Phi, RedPower, GreenPower, BluePower, AlphaPower, NumScatterings, LightIndex);

        public override string ToString() => $"{Position} | {Plane} | {Theta} | {Phi} | {RedPower} | {GreenPower} | {BluePower} | {AlphaPower} | {NumScatterings} | {LightIndex}";
    }
}
