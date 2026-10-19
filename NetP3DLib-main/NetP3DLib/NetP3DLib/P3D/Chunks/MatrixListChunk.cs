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
public class MatrixListChunk : Chunk
{
    public const ChunkIdentifier ChunkID = ChunkIdentifier.Matrix_List;

    public uint NumMatrices
    {
        get => (uint)(Matrices?.Count ?? 0);
        set
        {
            if (value == NumMatrices)
                return;

            if (value < NumMatrices)
            {
                Matrices.RemoveRange((int)value, (int)(NumMatrices - value));
            }
            else
            {
                int count = (int)(value - NumMatrices);
                var newMatrices = new Matrix[count];

                for (var i = 0; i < count; i++)
                    newMatrices[i] = new();

                Matrices.AddRange(newMatrices);
            }
        }
    }
    public SizeAwareList<Matrix> Matrices { get; }

    public override byte[] DataBytes
    {
        get
        {
            var data = new List<byte>((int)DataLength);

            data.AddRange(BitConverter.GetBytes(NumMatrices));
            foreach (var mat in Matrices)
                data.AddRange(mat.DataBytes);

            return [.. data];
        }
    }
    public override uint DataLength => sizeof(uint) + sizeof(uint) * NumMatrices;

    public MatrixListChunk(EndianAwareBinaryReader br) : this(br.ReadArray(() => new Matrix(br), out _))
    {
    }

    public MatrixListChunk(IList<Matrix> matrices) : base(ChunkID)
    {
        Matrices = CreateSizeAwareList(matrices, Matrices_CollectionChanged);
    }
    
    private void Matrices_CollectionChanged(object sender, System.Collections.Specialized.NotifyCollectionChangedEventArgs e)
    {
        OnPropertyChanged(nameof(Matrices));

        if (e.OldItems != null)
            foreach (Matrix oldItem in e.OldItems)
                oldItem.PropertyChanged -= Matrices_PropertyChanged;
    
        if (e.NewItems != null)
            foreach (Matrix newItem in e.NewItems)
                newItem.PropertyChanged += Matrices_PropertyChanged;
    }
    
    private void Matrices_PropertyChanged() => OnPropertyChanged(nameof(Matrices));

    public override IEnumerable<InvalidP3DException> ValidateChunk()
    {
        foreach (var error in base.ValidateChunk())
            yield return error;

        if (ParentChunk is OldPrimitiveGroupChunk oldPrimitiveGroup && oldPrimitiveGroup.NumVertices != NumMatrices)
            yield return new InvalidP3DException(this, $"Num Matrices value {NumMatrices} does not match parent Num Vertices value {oldPrimitiveGroup.NumVertices}.");
    }

    protected override void WriteData(EndianAwareBinaryWriter bw)
    {
        bw.Write(NumMatrices);
        foreach (var mat in Matrices)
            mat.Write(bw);
    }

    protected override Chunk CloneSelf()
    {
        var matrices = new Matrix[Matrices.Count];
        for (var i = 0; i < Matrices.Count; i++)
            matrices[i] = Matrices[i].Clone();
        return new MatrixListChunk(matrices);
    }

    public class Matrix
    {
        public event Action? PropertyChanged;

        private byte _a;
        public byte A
        {
            get => _a;
            set
            {
                if (_a == value)
                    return;
    
                _a = value;
                PropertyChanged?.Invoke();
            }
        }
    
        private byte _b;
        public byte B
        {
            get => _b;
            set
            {
                if (_b == value)
                    return;
    
                _b = value;
                PropertyChanged?.Invoke();
            }
        }
    
        private byte _c;
        public byte C
        {
            get => _c;
            set
            {
                if (_c == value)
                    return;
    
                _c = value;
                PropertyChanged?.Invoke();
            }
        }
    
        private byte _d;
        public byte D
        {
            get => _d;
            set
            {
                if (_d == value)
                    return;
    
                _d = value;
                PropertyChanged?.Invoke();
            }
        }
    
        public byte[] DataBytes => [D, C, B, A];

        public Matrix(BinaryReader br)
        {
            _d = br.ReadByte();
            _c = br.ReadByte();
            _b = br.ReadByte();
            _a = br.ReadByte();
        }

        public Matrix(byte a, byte b, byte c, byte d)
        {
            _a = a;
            _b = b;
            _c = c;
            _d = d;
        }

        public Matrix()
        {
            _a = 0;
            _b = 0;
            _c = 0;
            _d = 0;
        }

        internal void Write(BinaryWriter bw)
        {
            bw.Write(D);
            bw.Write(C);
            bw.Write(B);
            bw.Write(A);
        }

        internal Matrix Clone() => new(A, B, C, D);

        public override string ToString() => $"{A} | {B} | {C} | {D}";
    }
}
