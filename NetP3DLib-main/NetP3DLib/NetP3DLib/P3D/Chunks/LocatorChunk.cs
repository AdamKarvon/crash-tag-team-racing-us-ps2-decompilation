using NetP3DLib.IO;
using NetP3DLib.P3D.Attributes;
using NetP3DLib.P3D.Enums;
using NetP3DLib.P3D.Extensions;
using System;
using System.Collections.Generic;
using System.Numerics;
using System.Text;

namespace NetP3DLib.P3D.Chunks;

[ChunkAttributes(ChunkID)]
public class LocatorChunk : NamedChunk
{
    public const ChunkIdentifier ChunkID = ChunkIdentifier.Locator;

    public enum LocatorTypes : uint
    {
        Event,
        Script,
        Generic,
        CarStart,
        Spline,
        DynamicZone,
        Occlusion,
        InteriorEntrance,
        Directional,
        Action,
        FOV,
        BreakableCamera,
        StaticCamera,
        PedGroup,
        Coin,
    }

    public LocatorTypes LocatorType => TypeData.LocatorType;
    public uint DataLen => TypeData.DataLen;
    private LocatorData _typeData = new UnknownLocatorData(15, []);
    public LocatorData TypeData
    {
        get => _typeData;
        set
        {
            _typeData.SizeChanged -= TypeData_SizeChanged;
            _typeData.PropertyChanged -= TypeData_PropertyChanged;
            _typeData = value;
            value.SizeChanged += TypeData_SizeChanged;
            value.PropertyChanged += TypeData_PropertyChanged;
        }
    }
    
    private Vector3 _position;
    public Vector3 Position
    {
        get => _position;
        set
        {
            if (_position == value)
                return;
    
            _position = value;
            OnPropertyChanged(nameof(Position));
        }
    }
    
    public uint TriggerCount => GetChildCount(ChunkIdentifier.Trigger_Volume);

    public override byte[] DataBytes
    {
        get
        {
            var data = new List<byte>((int)DataLength);

            data.AddRange(BinaryExtensions.GetP3DStringBytes(Name));
            data.AddRange(BitConverter.GetBytes((uint)LocatorType));
            data.AddRange(BitConverter.GetBytes(DataLen));
            foreach (var item in TypeData.DataArray)
                data.AddRange(BitConverter.GetBytes(item));
            data.AddRange(BinaryExtensions.GetBytes(Position));
            data.AddRange(BitConverter.GetBytes(TriggerCount));

            return [.. data];
        }
    }
    public override uint DataLength => BinaryExtensions.GetP3DStringLength(Name) + sizeof(uint) + sizeof(uint) + sizeof(uint) * DataLen + sizeof(float) * 3 + sizeof(uint);

    [System.Diagnostics.CodeAnalysis.SuppressMessage("Style", "IDE0059:Unnecessary assignment of a value", Justification = "We want to read the value to progress the BinaryReader, but not set the value anywhere because it's calculated dynamically.")]
    public LocatorChunk(EndianAwareBinaryReader br) : this(br.ReadP3DString(), ReadLocatorData(br), br.ReadVector3())
    {
        var triggerCount = br.ReadUInt32();
    }

    private static LocatorData ReadLocatorData(EndianAwareBinaryReader br)
    {
        var type = (LocatorTypes)br.ReadUInt32();
        var data = br.ReadUInt32Array(out _);
        return type switch
        {
            LocatorTypes.Event => new EventLocatorData(data),
            LocatorTypes.Script => new ScriptLocatorData(data),
            LocatorTypes.Generic => new GenericLocatorData(),
            LocatorTypes.CarStart => new CarStartLocatorData(data),
            LocatorTypes.Spline => new SplineLocatorData(),
            LocatorTypes.DynamicZone => new DynamicZoneLocatorData(data),
            LocatorTypes.Occlusion => new OcclusionLocatorData(data),
            LocatorTypes.InteriorEntrance => new InteriorEntranceLocatorData(data),
            LocatorTypes.Directional => new DirectionalLocatorData(data),
            LocatorTypes.Action => new ActionLocatorData(data),
            LocatorTypes.FOV => new FOVLocatorData(data),
            LocatorTypes.BreakableCamera => new BreakableCameraLocatorData(),
            LocatorTypes.StaticCamera => new StaticCameraLocatorData(data),
            LocatorTypes.PedGroup => new PedGroupLocatorData(data),
            LocatorTypes.Coin => new CoinLocatorData(),
            _ => new UnknownLocatorData(type, data)
        };
    }

    public LocatorChunk(string name, LocatorData typeData, Vector3 position) : base(ChunkID, name)
    {
        TypeData = typeData;
        _position = position;
    }

    private void TypeData_SizeChanged(int delta) => RecalculateSize((uint)(HeaderSize - delta));

    private void TypeData_PropertyChanged() => OnPropertyChanged(nameof(TypeData));

    protected override void WriteData(EndianAwareBinaryWriter bw)
    {
        bw.WriteP3DString(Name);
        bw.Write((uint)LocatorType);
        bw.Write(DataLen);
        TypeData.WriteData(bw);
        bw.Write(Position);
        bw.Write(TriggerCount);
    }

    public override string ToString() => $"\"{Name}\" ({LocatorType} {GetChunkType(this)} (Type {(int)LocatorType}) (0x{ID:X}))";

    protected override Chunk CloneSelf() => new LocatorChunk(Name, TypeData.Clone(), Position);

    public abstract class LocatorData
    {
        public event Action<int>? SizeChanged;
        protected void OnSizeChanged(int delta) => SizeChanged?.Invoke(delta);

        public event Action? PropertyChanged;
        protected void OnPropertyChanged(string propertyName) => PropertyChanged?.Invoke();

        public LocatorTypes LocatorType { get; }
        public virtual uint DataLen => (uint)DataArray.Count;
        public abstract List<uint> DataArray { get; }

        public LocatorData(LocatorTypes locatorType)
        {
            LocatorType = locatorType;
        }

        public void WriteData(EndianAwareBinaryWriter bw)
        {
            foreach (var item in DataArray)
                bw.Write(item);
        }

        internal abstract LocatorData Clone();

        internal static (string String, int Index) ParseDataString(IList<uint> data, int offset = 0)
        {
            StringBuilder result = new();
            int index = offset;

            while (index < data.Count)
            {
                uint value = data[index];
                if (value == 0)
                    break;

                for (int i = 0; i < 4; i++)
                {
                    char ch = (char)(value & 0xFF);
                    if (ch == '\0')
                        return (result.ToString(), index + 1);

                    result.Append(ch);
                    value >>= 8;
                }

                index++;
            }

            return (result.ToString(), index + 1);
        }

        internal static List<uint> CreateStringData(string input)
        {
            int diff = input.Length % 4;
            string str = $"{input}{new string('\0', 4 - diff)}";

            List<uint> result = [];
            int length = str.Length;
            for (int i = 0; i < length; i += 4)
            {
                uint value = 0;
                for (int j = 0; j < 4 && i + j < length; j++)
                {
                    value |= (uint)(str[i + j] << (8 * j));
                }
                result.Add(value);
            }
            return result;
        }

        internal static unsafe float ParseDataFloat(uint data) => *(float*)&data;

        internal static unsafe uint CreateFloatData(float input) => *(uint*)&input;
    }

    public class UnknownLocatorData : LocatorData
    {
        private uint[] _data;
        public uint[] Data
        {
            get => _data;
            set
            {
                if (ReferenceEquals(_data, value))
                    return;

                var oldSize = _data.Length;
                _data = value ?? [];
                var newSize = _data.Length;
                OnSizeChanged((newSize - oldSize) * sizeof(uint));
                OnPropertyChanged(nameof(Data));
            }
        }
        public override List<uint> DataArray => [.. Data];

        public UnknownLocatorData(LocatorTypes locatorType, IList<uint> data) : base(locatorType)
        {
            _data = [.. data];
        }

        public UnknownLocatorData(uint locatorType, IList<uint> data) : this((LocatorTypes)locatorType, data)
        { }

        internal override LocatorData Clone() => new UnknownLocatorData(LocatorType, Data);

        public override string ToString() => $"[{string.Join(", ", Data)}]";
    }

    /// <summary>
    /// Event locator data.
    /// </summary>
    public class EventLocatorData : LocatorData
    {
        public enum Events : uint
        {
            Flag = 0,
            CameraCut = 1,
            CheckPoint = 2,
            Base = 3,
            Death = 4,
            InteriorExit = 5,
            Bouncepad = 6,
            AmbientSoundCity = 7,
            AmbientSoundSeaside = 8,
            AmbientSoundSuburbs = 9,
            AmbientSoundForest = 10,
            AmbientKwikEMartRooftop = 11,
            AmbientSoundFarm = 12,
            AmbientSoundBarn = 13,
            AmbientSoundPowerplantExterior = 14,
            AmbientSoundPowerplantInterior = 15,
            AmbientSoundRiver = 16,
            AmbientSoundCityBusiness = 17,
            AmbientSoundConstruction = 18,
            AmbientSoundStadium = 19,
            AmbientSoundStadiumTunnel = 20,
            AmbientSoundExpressway = 21,
            AmbientSoundSlum = 22,
            AmbientSoundRailyard = 23,
            AmbientSoundHospital = 24,
            AmbientSoundLightCity = 25,
            AmbientSoundShipyard = 26,
            AmbientSoundQuay = 27,
            AmbientSoundLighthouse = 28,
            AmbientSoundCountryHighway = 29,
            AmbientSoundKrustylu = 30,
            AmbientSoundDam = 31,
            AmbientSoundForestHighway = 32,
            AmbientSoundRetainingWallTunnel = 33,
            AmbientSoundKrustyluExterior = 34,
            AmbientSoundDuffExterior = 35,
            AmbientSoundDuffInterior = 36,
            AmbientSoundStoneCutterTunnel = 37,
            AmbientSoundStoneCutterHall = 38,
            AmbientSoundSewers = 39,
            AmbientSoundBurnsTunnel = 40,
            AmbientSoundPPRoom1 = 41,
            AmbientSoundPPRoom2 = 42,
            AmbientSoundPPRoom3 = 43,
            AmbientSoundPPTunnel1 = 44,
            AmbientSoundPPTunnel2 = 45,
            AmbientSoundMansionInterior = 46,
            ParkedBirds = 47,
            WhackyGravity = 48,
            FarPlane = 49,
            AmbientSoundCountryNight = 50,
            AmbientSoundSuburbsNight = 51,
            AmbientSoundForestNight = 52,
            AmbientSoundHalloween1 = 53,
            AmbientSoundHalloween2 = 54,
            AmbientSoundHalloween3 = 55,
            AmbientSoundPlaceholder3 = 56,
            AmbientSoundPlaceholder4 = 57,
            AmbientSoundPlaceholder5 = 58,
            AmbientSoundPlaceholder6 = 59,
            AmbientSoundPlaceholder7 = 60,
            AmbientSoundPlaceholder8 = 61,
            AmbientSoundPlaceholder9 = 62,
            GooDamage = 63,
            CoinZone = 64,
            LightChange = 65,
            Trap = 66,
            AmbientSoundSeasideNight = 67,
            AmbientSoundLighthouseNight = 68,
            AmbientSoundBreweryNight = 69,
            AmbientSoundPlaceholder10 = 70,
            AmbientSoundPlaceholder11 = 71,
            AmbientSoundPlaceholder12 = 72,
            AmbientSoundPlaceholder13 = 73,
            AmbientSoundPlaceholder14 = 74,
            AmbientSoundPlaceholder15 = 75,
            AmbientSoundPlaceholder16 = 76,
            Special = 77,
            OcclusionZone = 78,
            CarDoor = 79,
            ActionButton = 80,
            InteriorEntrance = 81,
            GenericButtonHandlerEvent = 82,
            FountainJump = 83,
            LoadPedModelGroup = 84,
            Gag = 85,
        }

        private Events _event;
        public Events Event
        {
            get => _event;
            set
            {
                if (_event == value)
                    return;
    
                _event = value;
                OnPropertyChanged(nameof(Event));
            }
        }
    
        private uint? _parameter = null;
        public uint? Parameter
        {
            get => _parameter;
            set
            {
                if (_parameter == value)
                    return;

                var oldHasValue = _parameter.HasValue;
                _parameter = value;
                var newHasValue = _parameter.HasValue;

                if (oldHasValue != newHasValue)
                {
                    if (oldHasValue)
                        OnSizeChanged(-sizeof(uint));
                    else
                        OnSizeChanged(sizeof(uint));
                }

                OnPropertyChanged(nameof(Parameter));
            }
        }

        public override uint DataLen => Parameter.HasValue ? 2u : 1u;
        public override List<uint> DataArray
        {
            get
            {
                List<uint> data =
                [
                    (uint)Event,
                ];
                if (Parameter.HasValue)
                    data.Add(Parameter.Value);

                return [.. data];
            }
        }

        public EventLocatorData(IList<uint> data) : base(LocatorTypes.Event)
        {
            _event = (Events)data[0];
            if (data.Count > 1)
                Parameter = data[1];
        }

        public EventLocatorData(Events @event) : base(LocatorTypes.Event)
        {
            _event = @event;
            Parameter = null;
        }

        public EventLocatorData(Events @event, uint parameter) : base(LocatorTypes.Event)
        {
            _event = @event;
            Parameter = parameter;
        }

        internal override LocatorData Clone() => Parameter.HasValue ? new EventLocatorData(Event, Parameter.Value) : new EventLocatorData(Event);

        public override string ToString() => $"_event = {Event}, Parameter = {Parameter?.ToString() ?? "null"}";
    }

    /// <summary>
    /// Script locator data.
    /// </summary>
    public class ScriptLocatorData : LocatorData
    {
        private string _key = string.Empty;
        public string Key
        {
            get => _key;
            set
            {
                if (_key == value)
                    return;

                var oldSize = DataArray.Count;
                _key = value;
                var newSize = DataArray.Count;
                OnSizeChanged((newSize - oldSize) * sizeof(uint));
                OnPropertyChanged(nameof(Key));
            }
        }

        public override List<uint> DataArray => CreateStringData(Key);

        public ScriptLocatorData(IList<uint> data) : base(LocatorTypes.Script)
        {
            Key = ParseDataString(data).String;
        }

        public ScriptLocatorData(string key) : base(LocatorTypes.Script)
        {
            Key = key;
        }

        internal override LocatorData Clone() => new ScriptLocatorData(Key);

        public override string ToString() => $"Key = {Key}";
    }

    /// <summary>
    /// Generic locator data.
    /// </summary>
    public class GenericLocatorData : LocatorData
    {
        public override uint DataLen => 0;

        public override List<uint> DataArray => [];

        public GenericLocatorData() : base(LocatorTypes.Generic)
        { }

        internal override LocatorData Clone() => new GenericLocatorData();

        public override string ToString() => "No data";
    }

    /// <summary>
    /// Car Start locator data.
    /// </summary>
    public class CarStartLocatorData : LocatorData
    {
        private float _rotation;
        public float Rotation
        {
            get => _rotation;
            set
            {
                if (_rotation == value)
                    return;
    
                _rotation = value;
                OnPropertyChanged(nameof(Rotation));
            }
        }
    
        private uint? _parkedCar = null;
        public bool ParkedCar
        {
            get => _parkedCar.HasValue && _parkedCar == 1;
            set
            {
                if (ParkedCar == value)
                    return;

                var oldHasValue = _parkedCar.HasValue;
                _parkedCar = value ? 1u : 0u;
                var newHasValue = _parkedCar.HasValue;

                if (oldHasValue != newHasValue)
                {
                    if (oldHasValue)
                        OnSizeChanged(-sizeof(uint));
                    else
                        OnSizeChanged(sizeof(uint));
                }

                OnPropertyChanged(nameof(ParkedCar));
            }
        }
        private string? _freeCar = null;
        public string? FreeCar
        {
            get => _freeCar;
            set
            {
                if (_freeCar == value)
                    return;

                var oldSize = DataArray.Count;

                if (string.IsNullOrEmpty(value))
                {
                    _freeCar = null;
                }
                else
                {
                    if (!_parkedCar.HasValue)
                        _parkedCar = 0u;
                    _freeCar = value;
                }

                var newSize = DataArray.Count;
                OnSizeChanged((newSize - oldSize) * sizeof(uint));
                OnPropertyChanged(nameof(FreeCar));
            }
        }

        public override List<uint> DataArray
        {
            get
            {
                List<uint> data =
                [
                    CreateFloatData(Rotation),
                ];
                if (_parkedCar.HasValue)
                {
                    data.Add(_parkedCar.Value);
                    if (FreeCar != null)
                        data.AddRange(CreateStringData(FreeCar));
                }

                return [.. data];
            }
        }

        public CarStartLocatorData(IList<uint> data) : base(LocatorTypes.CarStart)
        {
            _rotation = ParseDataFloat(data[0]);
            _parkedCar = data.Count > 1 ? data[1] : null;
            _freeCar = data.Count > 2 ? ParseDataString(data, 2).String : null;
        }

        public CarStartLocatorData(float rotation) : this(rotation, null, null)
        {
        }

        public CarStartLocatorData(float rotation, bool parkedCar) : this(rotation, parkedCar? 1u : 0u, null)
        {
        }

        public CarStartLocatorData(float rotation, bool parkedCar, string? freeCar) : this(rotation, parkedCar ? 1u : 0u, freeCar)
        {
        }

        public CarStartLocatorData(float rotation, uint? parkedCar, string? freeCar) : base(LocatorTypes.CarStart)
        {
            _rotation = rotation;
            _parkedCar = parkedCar;
            _freeCar = freeCar;
        }

        internal override LocatorData Clone() => _parkedCar.HasValue ? new CarStartLocatorData(Rotation, _parkedCar.Value, _freeCar) : new CarStartLocatorData(Rotation);

        public override string ToString() => $"Rotation = {Rotation}, ParkedCar = {(_parkedCar.HasValue ? ParkedCar.ToString() : "null")}, FreeCar = {FreeCar ?? "null"}";
    }

    /// <summary>
    /// Spline locator data.
    /// </summary>
    public class SplineLocatorData : LocatorData
    {
        public override uint DataLen => 0;

        public override List<uint> DataArray => [];

        public SplineLocatorData() : base(LocatorTypes.Spline)
        { }

        internal override LocatorData Clone() => new SplineLocatorData();

        public override string ToString() => "No data";
    }

    /// <summary>
    /// Dynamic Zone locator data.
    /// </summary>
    public class DynamicZoneLocatorData : LocatorData
    {
        private string _dynaLoadData = string.Empty;
        public string DynaLoadData
        {
            get => _dynaLoadData;
            set
            {
                if (_dynaLoadData == value)
                    return;

                var oldSize = DataArray.Count;
                _dynaLoadData = value;
                var newSize = DataArray.Count;
                OnSizeChanged((newSize - oldSize) * sizeof(uint));
                OnPropertyChanged(nameof(DynaLoadData));
            }
        }

        public override List<uint> DataArray => CreateStringData(DynaLoadData);

        public DynamicZoneLocatorData(IList<uint> data) : base(LocatorTypes.DynamicZone)
        {
            DynaLoadData = ParseDataString(data).String;
        }

        public DynamicZoneLocatorData(string dynaLoadData) : base(LocatorTypes.DynamicZone)
        {
            DynaLoadData = dynaLoadData;
        }

        internal override LocatorData Clone() => new DynamicZoneLocatorData(DynaLoadData);

        public override string ToString() => $"DynaLoadData = {DynaLoadData}";
    }

    /// <summary>
    /// Occlusion locator data.
    /// </summary>
    public class OcclusionLocatorData : LocatorData
    {
        private uint? _occlusions = null;
        public uint? Occlusions
        {
            get => _occlusions;
            set
            {
                if (_occlusions == value)
                    return;

                var oldHasValue = _occlusions.HasValue;
                _occlusions = value;
                var newHasValue = _occlusions.HasValue;

                if (oldHasValue != newHasValue)
                {
                    if (oldHasValue)
                        OnSizeChanged(-sizeof(uint));
                    else
                        OnSizeChanged(sizeof(uint));
                }

                OnPropertyChanged(nameof(Occlusions));
            }
        }

        public override uint DataLen => Occlusions.HasValue ? 1u : 0u;
        public override List<uint> DataArray => Occlusions.HasValue ? [Occlusions.Value] : [];

        public OcclusionLocatorData(IList<uint> data) : base(LocatorTypes.Occlusion)
        {
            Occlusions = data.Count > 0 ? data[0] : null;
        }

        public OcclusionLocatorData() : base(LocatorTypes.Occlusion)
        { }

        public OcclusionLocatorData(uint occlusions) : base(LocatorTypes.Occlusion)
        {
            Occlusions = occlusions;
        }

        internal override LocatorData Clone() => Occlusions.HasValue ? new OcclusionLocatorData(Occlusions.Value) : new OcclusionLocatorData();

        public override string ToString() => $"Occlusions = {Occlusions?.ToString() ?? "null"}";
    }

    /// <summary>
    /// Interior Entrance locator data.
    /// </summary>
    public class InteriorEntranceLocatorData : LocatorData
    {
        private string _interiorName = string.Empty;
        public string InteriorName
        {
            get => _interiorName;
            set
            {
                if (_interiorName == value)
                    return;

                var oldSize = DataArray.Count;
                _interiorName = value;
                var newSize = DataArray.Count;
                OnSizeChanged((newSize - oldSize) * sizeof(uint));
                OnPropertyChanged(nameof(InteriorName));
            }
        }
    
        private Vector3 _right;
        public Vector3 Right
        {
            get => _right;
            set
            {
                if (_right == value)
                    return;
    
                _right = value;
                OnPropertyChanged(nameof(Right));
            }
        }
    
        private Vector3 _up;
        public Vector3 Up
        {
            get => _up;
            set
            {
                if (_up == value)
                    return;
    
                _up = value;
                OnPropertyChanged(nameof(Up));
            }
        }
    
        private Vector3 _front;
        public Vector3 Front
        {
            get => _front;
            set
            {
                if (_front == value)
                    return;
    
                _front = value;
                OnPropertyChanged(nameof(Front));
            }
        }

        public override List<uint> DataArray
        {
            get
            {
                List<uint> data = [];

                data.AddRange(CreateStringData(InteriorName));
                data.Add(CreateFloatData(Right.X));
                data.Add(CreateFloatData(Right.Y));
                data.Add(CreateFloatData(Right.Z));
                data.Add(CreateFloatData(Up.X));
                data.Add(CreateFloatData(Up.Y));
                data.Add(CreateFloatData(Up.Z));
                data.Add(CreateFloatData(Front.X));
                data.Add(CreateFloatData(Front.Y));
                data.Add(CreateFloatData(Front.Z));

                return [.. data];
            }
        }

        public InteriorEntranceLocatorData(IList<uint> data) : base(LocatorTypes.InteriorEntrance)
        {
            var interiorName = ParseDataString(data);
            InteriorName = interiorName.String;
            var index = interiorName.Index;
            _right = new(ParseDataFloat(data[index++]), ParseDataFloat(data[index++]), ParseDataFloat(data[index++]));
            _up = new(ParseDataFloat(data[index++]), ParseDataFloat(data[index++]), ParseDataFloat(data[index++]));
            _front = new(ParseDataFloat(data[index++]), ParseDataFloat(data[index++]), ParseDataFloat(data[index++]));
        }

        public InteriorEntranceLocatorData(string interiorName, Vector3 right, Vector3 up, Vector3 front) : base(LocatorTypes.InteriorEntrance)
        {
            InteriorName = interiorName;
            _right = right;
            _up = up;
            _front = front;
        }

        internal override LocatorData Clone() => new InteriorEntranceLocatorData(InteriorName, Right, Up, Front);

        public override string ToString() => $"InteriorName = {InteriorName}, _right = {Right}, _up = {Up}, _front = {Front}";
    }

    /// <summary>
    /// Directional locator data.
    /// </summary>
    public class DirectionalLocatorData : LocatorData
    {
        private Vector3 _right;
        public Vector3 Right
        {
            get => _right;
            set
            {
                if (_right == value)
                    return;
    
                _right = value;
                OnPropertyChanged(nameof(Right));
            }
        }
    
        private Vector3 _up;
        public Vector3 Up
        {
            get => _up;
            set
            {
                if (_up == value)
                    return;
    
                _up = value;
                OnPropertyChanged(nameof(Up));
            }
        }
    
        private Vector3 _front;
        public Vector3 Front
        {
            get => _front;
            set
            {
                if (_front == value)
                    return;
    
                _front = value;
                OnPropertyChanged(nameof(Front));
            }
        }
    

        public override uint DataLen => 9;
        public override List<uint> DataArray =>
        [
            CreateFloatData(Right.X),
            CreateFloatData(Right.Y),
            CreateFloatData(Right.Z),
            CreateFloatData(Up.X),
            CreateFloatData(Up.Y),
            CreateFloatData(Up.Z),
            CreateFloatData(Front.X),
            CreateFloatData(Front.Y),
            CreateFloatData(Front.Z),
        ];

        public DirectionalLocatorData(IList<uint> data) : base(LocatorTypes.Directional)
        {
            var index = 0;
            _right = new(ParseDataFloat(data[index++]), ParseDataFloat(data[index++]), ParseDataFloat(data[index++]));
            _up = new(ParseDataFloat(data[index++]), ParseDataFloat(data[index++]), ParseDataFloat(data[index++]));
            _front = new(ParseDataFloat(data[index++]), ParseDataFloat(data[index++]), ParseDataFloat(data[index++]));
        }

        public DirectionalLocatorData(Vector3 right, Vector3 up, Vector3 front) : base(LocatorTypes.Directional)
        {
            _right = right;
            _up = up;
            _front = front;
        }

        internal override LocatorData Clone() => new DirectionalLocatorData(Right, Up, Front);

        public override string ToString() => $"_right = {Right}, _up = {Up}, _front = {Front}";
    }

    /// <summary>
    /// Action locator data.
    /// </summary>
    public class ActionLocatorData : LocatorData
    {
        public enum Intention : uint
        {
            NONE,
            LeftStickX,
            LeftStickY,
            DoAction,
            Jump,
            Dash,
            Attack,
            DPadUp,
            DPadDown,
            DPadLeft,
            DPadRight,
            GetOutCar,
            MouseLookLeft,
            MouseLookRight,
        }

        private string _objectName = string.Empty;
        public string ObjectName
        {
            get => _objectName;
            set
            {
                if (_objectName == value)
                    return;

                var oldSize = DataArray.Count;
                _objectName = value;
                var newSize = DataArray.Count;
                OnSizeChanged((newSize - oldSize) * sizeof(uint));
                OnPropertyChanged(nameof(ObjectName));
            }
        }
        private string _jointName = string.Empty;
        public string JointName
        {
            get => _jointName;
            set
            {
                if (_jointName == value)
                    return;

                var oldSize = DataArray.Count;
                _jointName = value;
                var newSize = DataArray.Count;
                OnSizeChanged((newSize - oldSize) * sizeof(uint));
                OnPropertyChanged(nameof(JointName));
            }
        }
        private string _actionName = string.Empty;
        [KnownValues(true, "ToggleOnOff", "ToggleReverse", "PlayAnim", "PlayAnimLoop", "AutoPlayAnim", "AutoPlayAnimLoop", "AutoPlayAnimInOut", "DestroyObject", "PowerCoupling", "UseVendingMachine", "PrankPhone", "SummonVehiclePhone", "Doorbell", "OpenDoor", "TalkFood", "TalkCollectible", "TalkDialog", "TalkMission", "FoodSmall", "FoodLarge", "CollectorCard", "AlienCamera", "PlayOnce", "AutomaticDoor", "Wrench", "Teleport", "PurchaseCar", "PurchaseSkin", "Nitro")]
        public string ActionName
        {
            get => _actionName;
            set
            {
                if (_actionName == value)
                    return;

                var oldSize = DataArray.Count;
                _actionName = value;
                var newSize = DataArray.Count;
                OnSizeChanged((newSize - oldSize) * sizeof(uint));
                OnPropertyChanged(nameof(ActionName));
            }
        }
    
        private Intention _buttonInput;
        public Intention ButtonInput
        {
            get => _buttonInput;
            set
            {
                if (_buttonInput == value)
                    return;
    
                _buttonInput = value;
                OnPropertyChanged(nameof(ButtonInput));
            }
        }
    
        private uint _shouldTransform;
        public bool ShouldTransform
        {
            get => _shouldTransform == 1;
            set
            {
                if (ShouldTransform == value)
                    return;

                _shouldTransform = value ? 1u : 0u;
                OnPropertyChanged(nameof(ShouldTransform));
            }
        }

        public override List<uint> DataArray
        {
            get
            {
                List<uint> data = [];

                data.AddRange(CreateStringData(ObjectName));
                data.AddRange(CreateStringData(JointName));
                data.AddRange(CreateStringData(ActionName));
                data.Add((uint)ButtonInput);
                data.Add(ShouldTransform ? 1u : 0u);

                return [.. data];
            }
        }

        public ActionLocatorData(IList<uint> data) : base(LocatorTypes.Action)
        {
            var objectName = ParseDataString(data);
            ObjectName = objectName.String;
            var jointName = ParseDataString(data, objectName.Index);
            JointName = jointName.String;
            var actionName = ParseDataString(data, jointName.Index);
            ActionName = actionName.String;
            var index = actionName.Index;
            _buttonInput = (Intention)data[index++];
            _shouldTransform = data[index++];
        }

        public ActionLocatorData(string objectName, string jointName, string actionName, Intention buttonInput, bool shouldTransform) : base(LocatorTypes.Action)
        {
            ObjectName = objectName;
            JointName = jointName;
            ActionName = actionName;
            _buttonInput = buttonInput;
            ShouldTransform = shouldTransform;
        }

        internal override LocatorData Clone() => new ActionLocatorData(ObjectName, JointName, ActionName, ButtonInput, ShouldTransform);

        public override string ToString() => $"ObjectName = {ObjectName}, JointName = {JointName}, ActionName = {ActionName}, _buttonInput = {ButtonInput}, ShouldTransform = {ShouldTransform}";
    }

    /// <summary>
    /// FOV locator data.
    /// </summary>
    public class FOVLocatorData : LocatorData
    {
        private float _fov;
        public float FOV
        {
            get => _fov;
            set
            {
                if (_fov == value)
                    return;
    
                _fov = value;
                OnPropertyChanged(nameof(FOV));
            }
        }
    
        private float _type;
        public float Type
        {
            get => _type;
            set
            {
                if (_type == value)
                    return;
    
                _type = value;
                OnPropertyChanged(nameof(Type));
            }
        }
    
        private float _rate;
        public float Rate
        {
            get => _rate;
            set
            {
                if (_rate == value)
                    return;
    
                _rate = value;
                OnPropertyChanged(nameof(Rate));
            }
        }

        public override uint DataLen => 3;
        public override List<uint> DataArray => [
            CreateFloatData(FOV),
            CreateFloatData(Type),
            CreateFloatData(Rate),
        ];

        public FOVLocatorData(IList<uint> data) : base(LocatorTypes.FOV)
        {
            _fov = ParseDataFloat(data[0]);
            _type = ParseDataFloat(data[1]);
            _rate = ParseDataFloat(data[2]);
        }

        public FOVLocatorData(float fov, float type, float rate) : base(LocatorTypes.FOV)
        {
            _fov = fov;
            _type = type;
            _rate = rate;
        }

        internal override LocatorData Clone() => new FOVLocatorData(FOV, Type, Rate);

        public override string ToString() => $"_fOV = {FOV}, _type = {Type}, _rate = {Rate}";
    }

    /// <summary>
    /// Breakable Camera locator data.
    /// </summary>
    public class BreakableCameraLocatorData : LocatorData
    {
        public override uint DataLen => 0;
        public override List<uint> DataArray => [];

        public BreakableCameraLocatorData() : base(LocatorTypes.BreakableCamera)
        { }

        internal override LocatorData Clone() => new BreakableCameraLocatorData();

        public override string ToString() => "No data";
    }

    /// <summary>
    /// Static Camera locator data.
    /// </summary>
    public class StaticCameraLocatorData : LocatorData
    {
        private Vector3 _targetPosition;
        public Vector3 TargetPosition
        {
            get => _targetPosition;
            set
            {
                if (_targetPosition == value)
                    return;
    
                _targetPosition = value;
                OnPropertyChanged(nameof(TargetPosition));
            }
        }
    
        private float _fov;
        public float FOV
        {
            get => _fov;
            set
            {
                if (_fov == value)
                    return;
    
                _fov = value;
                OnPropertyChanged(nameof(FOV));
            }
        }
    
        private float _targetLag;
        public float TargetLag
        {
            get => _targetLag;
            set
            {
                if (_targetLag == value)
                    return;
    
                _targetLag = value;
                OnPropertyChanged(nameof(TargetLag));
            }
        }
    
        private uint _followPlayer;
        public bool FollowPlayer
        {
            get => _followPlayer == 1u;
            set
        {
            if (FollowPlayer == value)
                return;

            _followPlayer = value ? 1u : 0u;
            OnPropertyChanged(nameof(FollowPlayer));
        }
        }
        private float? _transitionTargetRate = null;
        public float? TransitionTargetRate
        {
            get => _transitionTargetRate;
            set
            {
                if (_transitionTargetRate == value)
                    return;

                var oldHasValue = _transitionTargetRate.HasValue;
                _transitionTargetRate = value;
                var newHasValue = _transitionTargetRate.HasValue;

                if (oldHasValue != newHasValue)
                {
                    if (oldHasValue)
                        OnSizeChanged(-sizeof(float));
                    else
                        OnSizeChanged(sizeof(float));

                }
                
                OnPropertyChanged(nameof(TransitionTargetRate));
            }
        }
        private uint? _flags = null;
        public bool? OneShot
        {
            get
            {
                if (_flags == null)
                    return null;

                return (_flags & 1u) == 1u;
            }
            set
            {
                bool value2 = value ?? false;
                uint current = _flags ?? 0;

                bool isSet = (current & 1u) != 0;
                if (isSet == value2)
                    return;

                if (value2)
                    current |= 1u;
                else
                    current &= ~1u;

                var oldHasValue = _flags.HasValue;
                _flags = current;
                var newHasValue = _flags.HasValue;

                if (oldHasValue != newHasValue)
                {
                    if (oldHasValue)
                        OnSizeChanged(-sizeof(uint));
                    else
                        OnSizeChanged(sizeof(uint));
                }

                OnPropertyChanged(nameof(OneShot));
            }
        }
        public bool? DisableFOV
        {
            get
            {
                if (_flags == null)
                    return null;

                return (_flags & (1u << 1)) == (1u << 1);
            }
            set
            {
                bool value2 = value ?? false;
                uint current = _flags ?? 0;

                bool isSet = (current & (1u << 1)) != 0;
                if (isSet == value2)
                    return;

                if (value2)
                    current |= (1u << 1);
                else
                    current &= ~(1u << 1);

                var oldHasValue = _flags.HasValue;
                _flags = current;
                var newHasValue = _flags.HasValue;

                if (oldHasValue != newHasValue)
                {
                    if (oldHasValue)
                        OnSizeChanged(-sizeof(uint));
                    else
                        OnSizeChanged(sizeof(uint));
                }

                OnPropertyChanged(nameof(DisableFOV));
            }
        }
        private uint? _cutInOut = null;
        public bool? CutInOut
        {
            get => _cutInOut == 1u;
            set
            {
                if (value == null)
                {
                    if (_cutInOut != null)
                    {
                        _cutInOut = null;
                        OnSizeChanged(-sizeof(uint));
                    }
                    return;
                }

                if (CutInOut == value)
                    return;

                var oldHasValue = _cutInOut.HasValue;
                _cutInOut = value.Value ? 1u : 0u;
                var newHasValue = _cutInOut.HasValue;

                if (oldHasValue != newHasValue)
                {
                    if (oldHasValue)
                        OnSizeChanged(-sizeof(uint));
                    else
                        OnSizeChanged(sizeof(uint));
                }

                OnPropertyChanged(nameof(CutInOut));
            }
        }
        private uint? _flags2 = null;
        public bool? CarOnly
        {
            get
            {
                if (_flags2 == null)
                    return null;

                return (_flags2 & 1u) == 1u;
            }
            set
            {
                bool value2 = value ?? false;
                uint current = _flags2 ?? 0;

                bool isSet = (current & 1u) == 1u;
                if (isSet == value2)
                    return;

                if (value2)
                    current |= 1u;
                else
                    current &= ~1u;

                var oldHasValue = _flags2.HasValue;
                _flags2 = current;
                var newHasValue = _flags2.HasValue;

                if (oldHasValue != newHasValue)
                {
                    if (oldHasValue)
                        OnSizeChanged(-sizeof(uint));
                    else
                        OnSizeChanged(sizeof(uint));
                }

                OnPropertyChanged(nameof(CarOnly));
            }
        }
        public bool? OnFootOnly
        {
            get
            {
                if (_flags2 == null)
                    return null;

                return (_flags2 & (1u << 1)) == (1u << 1);
            }
            set
            {
                bool value2 = value ?? false;
                uint current = _flags2 ?? 0;

                bool isSet = (current & (1u << 1)) == (1u << 1);
                if (isSet == value2)
                    return;

                if (value2)
                    current |= (1u << 1);
                else
                    current &= ~(1u << 1);

                var oldHasValue = _flags2.HasValue;
                _flags2 = current;
                var newHasValue = _flags2.HasValue;

                if (oldHasValue != newHasValue)
                {
                    if (oldHasValue)
                        OnSizeChanged(-sizeof(uint));
                    else
                        OnSizeChanged(sizeof(uint));
                }

                OnPropertyChanged(nameof(OnFootOnly));
            }
        }

        public override List<uint> DataArray
        {
            get
            {
                List<uint> data = [];

                data.Add(CreateFloatData(TargetPosition.X));
                data.Add(CreateFloatData(TargetPosition.Y));
                data.Add(CreateFloatData(TargetPosition.Z));
                data.Add(CreateFloatData(FOV));
                data.Add(CreateFloatData(TargetLag));
                data.Add(_followPlayer);

                if (!TransitionTargetRate.HasValue)
                    return [.. data];
                data.Add(CreateFloatData(TransitionTargetRate.Value));

                if (!_flags.HasValue)
                    return [.. data];
                data.Add(_flags.Value);

                if (!_cutInOut.HasValue || !_flags2.HasValue)
                    return [.. data];
                data.Add(_cutInOut.Value);
                data.Add(_flags2.Value);

                return [.. data];
            }
        }

        public StaticCameraLocatorData(IList<uint> data) : base(LocatorTypes.StaticCamera)
        {
            TargetPosition = new(ParseDataFloat(data[0]), ParseDataFloat(data[1]), ParseDataFloat(data[2]));
            _fov = ParseDataFloat(data[3]);
            _targetLag = ParseDataFloat(data[4]);
            _followPlayer = data[5];
            TransitionTargetRate = data.Count > 6 ? ParseDataFloat(data[6]) : null;
            _flags = data.Count > 7 ? data[7] : null;
            _cutInOut = data.Count > 8 ? data[8] : null;
            _flags2 = data.Count > 9 ? data[9] : null;
        }

        public StaticCameraLocatorData(Vector3 targetPosition, float fov, float targetLag, uint followPlayer) : base(LocatorTypes.StaticCamera)
        {
            TargetPosition = targetPosition;
            _fov = fov;
            _targetLag = targetLag;
            _followPlayer = followPlayer;
            TransitionTargetRate = null;
            _flags = null;
            _cutInOut = null;
            _flags2 = null;
        }

        public StaticCameraLocatorData(Vector3 targetPosition, float fov, float targetLag, uint followPlayer, float? transitionTargetRate) : base(LocatorTypes.StaticCamera)
        {
            TargetPosition = targetPosition;
            _fov = fov;
            _targetLag = targetLag;
            _followPlayer = followPlayer;
            TransitionTargetRate = transitionTargetRate;
            _flags = null;
            _cutInOut = null;
            _flags2 = null;
        }

        public StaticCameraLocatorData(Vector3 targetPosition, float fov, float targetLag, uint followPlayer, float? transitionTargetRate, bool oneShot, bool disableFOV) : base(LocatorTypes.StaticCamera)
        {
            TargetPosition = targetPosition;
            _fov = fov;
            _targetLag = targetLag;
            _followPlayer = followPlayer;
            TransitionTargetRate = transitionTargetRate;
            OneShot = oneShot;
            DisableFOV = disableFOV;
            _cutInOut = null;
            _flags2 = null;
        }

        public StaticCameraLocatorData(Vector3 targetPosition, float fov, float targetLag, uint followPlayer, float? transitionTargetRate, bool oneShot, bool disableFOV, bool cutInOut, bool carOnly, bool onFootOnly) : base(LocatorTypes.StaticCamera)
        {
            TargetPosition = targetPosition;
            _fov = fov;
            _targetLag = targetLag;
            _followPlayer = followPlayer;
            TransitionTargetRate = transitionTargetRate;
            OneShot = oneShot;
            DisableFOV = disableFOV;
            CutInOut = cutInOut;
            CarOnly = carOnly;
            OnFootOnly = onFootOnly;
        }

        internal override LocatorData Clone() => (TransitionTargetRate.HasValue, _flags.HasValue, _cutInOut.HasValue, _flags2.HasValue) switch
        {
            (true, true, true, true) =>
                new StaticCameraLocatorData(TargetPosition, FOV, TargetLag, _followPlayer,
                                        TransitionTargetRate!.Value!, OneShot!.Value!, DisableFOV!.Value!, CutInOut!.Value!, CarOnly!.Value!, OnFootOnly!.Value!),
            (true, true, _, _) =>
                new StaticCameraLocatorData(TargetPosition, FOV, TargetLag, _followPlayer,
                                        TransitionTargetRate!.Value, OneShot!.Value!, DisableFOV!.Value!),
            (true, _, _, _) =>
                new StaticCameraLocatorData(TargetPosition, FOV, TargetLag, _followPlayer,
                                        TransitionTargetRate!.Value),
            _ =>
                new StaticCameraLocatorData(TargetPosition, FOV, TargetLag, _followPlayer)
        };

        public override string ToString() => $"TargetPosition = {TargetPosition}, _fOV = {FOV}, _targetLag = {TargetLag}, FollowPlayer = {_followPlayer}, TransitionTargetRate = {TransitionTargetRate?.ToString() ?? "null"}, OneShot = {OneShot}, DisableFOV = {DisableFOV}, CutInOut = {CutInOut}, CarOnly = {CarOnly}, OnFootOnly = {OnFootOnly}";
    }

    /// <summary>
    /// Ped Group locator data.
    /// </summary>
    public class PedGroupLocatorData : LocatorData
    {
        private uint _groupNum;
        public uint GroupNum
        {
            get => _groupNum;
            set
            {
                if (_groupNum == value)
                    return;
    
                _groupNum = value;
                OnPropertyChanged(nameof(GroupNum));
            }
        }
    

        public override uint DataLen => 1;
        public override List<uint> DataArray => [GroupNum];

        public PedGroupLocatorData(IList<uint> data) : base(LocatorTypes.PedGroup)
        {
            _groupNum = data[0];
        }

        public PedGroupLocatorData(uint groupNum) : base(LocatorTypes.PedGroup)
        {
            _groupNum = groupNum;
        }

        internal override LocatorData Clone() => new PedGroupLocatorData(GroupNum);

        public override string ToString() => $"_groupNum = {GroupNum}";
    }

    /// <summary>
    /// Coin locator data.
    /// </summary>
    public class CoinLocatorData : LocatorData
    {
        public override uint DataLen => 0;
        public override List<uint> DataArray => [];

        public CoinLocatorData() : base(LocatorTypes.Coin)
        { }

        internal override LocatorData Clone() => new CoinLocatorData();

        public override string ToString() => "No data";
    }
}
