namespace EpicKit.Manifest
{
    public class Guid : IEquatable<Guid>, IComparable<Guid>
    {
        private byte[] _Bytes = new byte[16];

        public byte[] Bytes
        {
            get => _Bytes;
            set
            {
                if (value.Length != 16)
                    throw new InvalidDataException();

                value.CopyTo(_Bytes, 0);
            }
        }

        public int CompareTo(Guid other) =>
            other == null ? 1 : Bytes.Aggregate(0, (total, v) => total += v) - other.Bytes.Aggregate(0, (total, v) => total += v);

        public bool Equals(Guid other) =>
            other == null ? false : other.Bytes.SequenceEqual(Bytes);

        public override string ToString() => string.Concat(
                    BitConverter.ToUInt32(_Bytes, 0).ToString("X8"),
                    BitConverter.ToUInt32(_Bytes, 4).ToString("X8"),
                    BitConverter.ToUInt32(_Bytes, 8).ToString("X8"),
                    BitConverter.ToUInt32(_Bytes, 12).ToString("X8"));
    }
}