using NetP3DLib.IO;
using NetP3DLib.P3D.Attributes;
using NetP3DLib.P3D.Collections;
using NetP3DLib.P3D.Enums;
using NetP3DLib.P3D.Exceptions;
using NetP3DLib.P3D.Extensions;
using System;
using System.Collections.Generic;
using System.IO;

namespace NetP3DLib.P3D.Chunks;

[ChunkAttributes(ChunkID)]
public class MultiControllerTracksChunk : Chunk
{
    public const ChunkIdentifier ChunkID = ChunkIdentifier.Multi_Controller_Tracks;

    public uint NumTracks
    {
        get => (uint)(Tracks?.Count ?? 0);
        set
        {
            if (value == NumTracks)
                return;

            if (value < NumTracks)
            {
                Tracks.RemoveRange((int)value, (int)(NumTracks - value));
            }
            else
            {
                int count = (int)(value - NumTracks);
                var newTracks = new Track[count];

                for (var i = 0; i < count; i++)
                    newTracks[i] = new();

                Tracks.AddRange(newTracks);
            }
        }
    }
    public SizeAwareList<Track> Tracks { get; }

    public override byte[] DataBytes
    {
        get
        {
            var data = new List<byte>((int)DataLength);

            data.AddRange(BitConverter.GetBytes(NumTracks));
            foreach (var track in Tracks)
                data.AddRange(track.DataBytes);

            return [.. data];
        }
    }
    public override uint DataLength
    {
        get
        {
            uint size = sizeof(uint);

            if (Tracks != null)
                foreach (var track in Tracks)
                    size += track.DataLength;

            return size;
        }
    }

    public MultiControllerTracksChunk(EndianAwareBinaryReader br) : this(br.ReadArray(() => new Track(br), out _))
    {
    }

    public MultiControllerTracksChunk(IList<Track> tracks) : base(ChunkID)
    {
        Tracks = CreateSizeAwareList<Track>(tracks.Count);
        Tracks.CollectionChanged += Tracks_CollectionChanged;

        Tracks.AddRange(tracks);
    }

    private void Tracks_CollectionChanged(object sender, System.Collections.Specialized.NotifyCollectionChangedEventArgs e)
    {
        OnPropertyChanged(nameof(Tracks));

        if (e.OldItems != null)
        {
            foreach (Track oldTrack in e.OldItems)
            {
                oldTrack.SizeChanged -= Track_SizeChanged;
                oldTrack.PropertyChanged -= Track_PropertyChanged;
            }
        }

        if (e.NewItems != null)
        {
            foreach (Track newTrack in e.NewItems)
            {
                newTrack.SizeChanged += Track_SizeChanged;
                newTrack.PropertyChanged += Track_PropertyChanged;
            }
        }
    }

    private void Track_SizeChanged(int delta) => RecalculateSize((uint)(HeaderSize - delta));

    private void Track_PropertyChanged() => OnPropertyChanged(nameof(Tracks));

    public override IEnumerable<InvalidP3DException> ValidateChunk()
    {
        foreach (var error in base.ValidateChunk())
            yield return error;

        foreach (var track in Tracks)
            foreach (var error in track.Validate(this))
                yield return error;
    }

    protected override void WriteData(EndianAwareBinaryWriter bw)
    {
        bw.Write(NumTracks);
        foreach (var track in Tracks)
            track.Write(bw);
    }

    protected override Chunk CloneSelf()
    {
        var tracks = new Track[Tracks.Count];
        for (var i = 0; i < Tracks.Count; i++)
            tracks[i] = Tracks[i].Clone();
        return new MultiControllerTracksChunk(tracks);
    }

    public class Track
    {
        public event Action<int>? SizeChanged;
        public event Action? PropertyChanged;

        private string _name = string.Empty;
        public string Name
        {
            get => _name;
            set
            {
                if (_name == value) return;

                var oldSize = BinaryExtensions.GetP3DStringLength(_name);
                _name = value;
                var newSize = BinaryExtensions.GetP3DStringLength(_name);
                SizeChanged?.Invoke((int)(newSize - oldSize));
                PropertyChanged?.Invoke();
            }
        }
    
        private float _startTime;
        public float StartTime
        {
            get => _startTime;
            set
            {
                if (_startTime == value)
                    return;
    
                _startTime = value;
                PropertyChanged?.Invoke();
            }
        }
    
        private float _endTime;
        public float EndTime
        {
            get => _endTime;
            set
            {
                if (_endTime == value)
                    return;
    
                _endTime = value;
                PropertyChanged?.Invoke();
            }
        }
    
        private float _scale;
        public float Scale
        {
            get => _scale;
            set
            {
                if (_scale == value)
                    return;
    
                _scale = value;
                PropertyChanged?.Invoke();
            }
        }

        public byte[] DataBytes
        {
            get
            {
                var data = new List<byte>((int)DataLength);

                data.AddRange(BinaryExtensions.GetP3DStringBytes(Name));
                data.AddRange(BitConverter.GetBytes(StartTime));
                data.AddRange(BitConverter.GetBytes(EndTime));
                data.AddRange(BitConverter.GetBytes(Scale));

                return [.. data];
            }
        }

        public uint DataLength => BinaryExtensions.GetP3DStringLength(Name) + sizeof(float) + sizeof(float) + sizeof(float);

        public Track(BinaryReader br)
        {
            _name = br.ReadP3DString();
            _startTime = br.ReadSingle();
            _endTime = br.ReadSingle();
            _scale = br.ReadSingle();
        }

        public Track(string name, float startTime, float endTime, float scale)
        {
            _name = name;
            _startTime = startTime;
            _endTime = endTime;
            _scale = scale;
        }

        public Track()
        {
            Name = string.Empty;
            _startTime = 0;
            _endTime = 0;
            _scale = 0;
        }

        public IEnumerable<InvalidP3DException> Validate(MultiControllerTracksChunk chunk)
        {
            if (!Name.IsValidP3DString())
                yield return new InvalidP3DStringException(chunk, nameof(Name), Name);
        }

        internal void Write(BinaryWriter bw)
        {
            bw.WriteP3DString(Name);
            bw.Write(StartTime);
            bw.Write(EndTime);
            bw.Write(Scale);
        }

        internal Track Clone() => new(Name, StartTime, EndTime, Scale);

        public override string ToString() => $"{Name} | {StartTime} | {EndTime} | {Scale}";
    }
}
