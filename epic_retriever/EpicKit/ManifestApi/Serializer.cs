using System.Text;

namespace EpicKit.Manifest
{
    class Serializer
    {
        public static void WriteByte(BinaryWriter writer, byte v)
        {
            writer.Write(v);
        }

        public static void WriteShort(BinaryWriter writer, short v)
        {
            writer.Write(v);
        }

        public static void WriteInt(BinaryWriter writer, int v)
        {
            writer.Write(v);
        }

        public static void WriteLong(BinaryWriter writer, long v)
        {
            writer.Write(v);
        }

        public static void WriteUShort(BinaryWriter writer, ushort v)
        {
            writer.Write(v);
        }

        public static void WriteUInt(BinaryWriter writer, uint v)
        {
            writer.Write(v);
        }

        public static void WriteULong(BinaryWriter writer, ulong v)
        {
            writer.Write(v);
        }

        public static void WriteString(BinaryWriter writer, string v)
        {
            int str_len = v.Length;
            if (str_len == 0)
            {
                WriteInt(writer, str_len);
            }
            else
            {
                try
                {
                    var bytes = Encoding.ASCII.GetBytes(v);
                    WriteInt(writer, str_len + 1);
                    WriteBytes(writer, bytes);
                    WriteByte(writer, 0);
                }
                catch
                {
                    var bytes = Encoding.Unicode.GetBytes(v);
                    str_len = -str_len;
                    WriteInt(writer, str_len - 1);
                    WriteBytes(writer, bytes);
                    WriteByte(writer, 0);
                    WriteByte(writer, 0);
                }
            }
        }

        public static void WriteGuid(BinaryWriter writer, Guid v)
        {
            WriteBytes(writer, v.Bytes);
        }

        public static void WriteBytes(BinaryWriter writer, byte[] bytes)
        {
            writer.Write(bytes);
        }
    }
}