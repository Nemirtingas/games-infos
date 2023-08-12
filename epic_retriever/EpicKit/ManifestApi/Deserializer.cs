using System.Text;

namespace EpicKit.Manifest
{
    class Deserializer
    {
        public static byte ReadByte(BinaryReader reader) =>
            reader.ReadByte();

        public static short ReadShort(BinaryReader reader) =>
            reader.ReadInt16();

        public static int ReadInt(BinaryReader reader) =>
            reader.ReadInt32();

        public static long ReadLong(BinaryReader reader) =>
            reader.ReadInt64();

        public static ushort ReadUShort(BinaryReader reader) =>
            reader.ReadUInt16();

        public static uint ReadUInt(BinaryReader reader) =>
            reader.ReadUInt32();

        public static ulong ReadULong(BinaryReader reader) =>
            reader.ReadUInt64();

        public static string ReadString(BinaryReader reader)
        {
            int str_len = reader.ReadInt32();
            if (str_len == 0)
                return string.Empty;

            // Ascii string
            if (str_len > 0)
            {
                // Skip null terminator
                return Encoding.ASCII.GetString(reader.ReadBytes(str_len), 0, str_len - 1);
            }
            // UTF16 LE string
            else
            {
                // Skip null terminator
                str_len = -str_len;
                return Encoding.Unicode.GetString(reader.ReadBytes(str_len * 2), 0, (str_len - 1) * 2);
            }
        }

        public static Guid ReadGuid(BinaryReader reader)
        {
            Guid v = new Guid();
            v.Bytes = ReadBytes(reader, 16);
            return v;
        }

        public static byte[] ReadBytes(BinaryReader reader, int size) =>
            reader.ReadBytes(size);

        private static IEnumerable<string> StringByteBlob(string s, int partLength)
        {
            if (s == null)
                throw new ArgumentNullException(nameof(s));
            if (partLength <= 0)
                throw new ArgumentException("Part length has to be positive.", nameof(partLength));

            for (var i = 0; i < s.Length; i += partLength)
                yield return s.Substring(i, Math.Min(partLength, s.Length - i));
        }

        public static byte ReadJsonByteBlob(string blob)
        {
            if (blob == null || blob.Length != 3)
                throw new InvalidDataException("");

            return Convert.ToByte(blob);
        }

        public static uint ReadJsonUIntBlob(string value)
        {
            byte[] bytes = new byte[4];
            int i = 0;
            foreach(var part in StringByteBlob(value, 3))
                bytes[i++] = ReadJsonByteBlob(part);

            return BitConverter.ToUInt32(BitConverter.IsLittleEndian ? bytes : bytes.Reverse().ToArray());
        }

        public static ulong ReadJsonULongBlob(string value)
        {
            byte[] bytes = new byte[8];
            int i = 0;
            foreach (var part in StringByteBlob(value, 3))
                bytes[i++] = ReadJsonByteBlob(part);

            return BitConverter.ToUInt64(BitConverter.IsLittleEndian ? bytes : bytes.Reverse().ToArray());
        }

        public static byte[] ReadJsonBytesBlob(string value)
        {
            byte[] bytes = new byte[value.Length / 3];
            int i = 0;
            foreach (var part in StringByteBlob(value, 3))
                bytes[i++] = ReadJsonByteBlob(part);

            return bytes;
        }

        public static Guid ReadJsonGuid(string blob)
        {
            var guid = new Guid();
            var bytes = ReadJsonHexString(blob);

            if (BitConverter.IsLittleEndian)
            {// Guid is big endian
                void swap(ref byte b1, ref byte b2)
                {
                    byte b = b1;
                    b1 = b2;
                    b2 = b;
                }

                for(int i = 0; i < 4; ++i)
                {
                    swap(ref bytes[i*4], ref bytes[3+i*4]);
                    swap(ref bytes[1+i*4], ref bytes[2+i*4]);
                }
            }

            guid.Bytes = bytes;

            return guid;
        }

        public static byte[] ReadJsonHexString(string blob)
        {
            if (blob == null)
                return new byte[0];

            var chars = blob.Length;
            var bytes = new byte[chars / 2];

            for (var i = 0; i < chars; i += 2)
                bytes[i / 2] = Convert.ToByte(blob.Substring(i, 2), 16);

            return bytes;
        }
    }

}