using System;
using System.IO;

namespace EpicKit.Manifest
{
    public class DataChunk : IDisposable
    {
        public const uint DataChunkMagic = 0xB1FE3AA2;

        public uint Magic { get; private set; } = 0;
        public uint Version { get; private set; } = 0;
        public uint Size { get; private set; } = 0;
        public uint SizeCompressed { get; private set; } = 0;
        public Guid Guid { get; private set; } = new Guid();
        public ulong Hash { get; private set; } = 0;
        public byte StoreFlags { get; private set; } = 0;
        public byte[] Sha1Hash { get; private set; } = new byte[20];
        // 1: EpicHash
        // 2: Sha1Hash
        // 3: Both
        public byte HashType { get; private set; } = 0;
        public uint SizeUncompressed { get; private set; } = 0;

        public byte[] Data { get; private set; } = new byte[0];

        public bool IsValid => Magic == DataChunkMagic;

        public void Dispose()
        {
            Data = new byte[0];
        }

        public void Read(Stream stream)
        {
            long start_pos = stream.Position;

            BinaryReader reader = new BinaryReader(stream);

            Magic = Deserializer.ReadUInt(reader);
            if (!IsValid)
                throw new InvalidDataException("Header doesn't match Magic number.");

            Version = Deserializer.ReadUInt(reader);
            Size = Deserializer.ReadUInt(reader);
            SizeCompressed = Deserializer.ReadUInt(reader);
            Guid.Bytes = Deserializer.ReadBytes(reader, 16);
            Hash = Deserializer.ReadULong(reader);
            StoreFlags = Deserializer.ReadByte(reader);

            if (Version >= 2)
            {
                Sha1Hash = Deserializer.ReadBytes(reader, 20);
                HashType = Deserializer.ReadByte(reader);
            }
            else
            {
                HashType = 0;
            }

            if (Version >= 3)
            {
                SizeUncompressed = Deserializer.ReadUInt(reader);
            }

            Data = Tools.ReadCompressedData(stream, (Manifest.ManifestStoreFlags)StoreFlags).ToArray();

            if ((stream.Position - start_pos) != Size)
            {
                // Adjust position for datas alignement.
                stream.Seek(Size - (stream.Position - start_pos), SeekOrigin.Current);
            }
        }
    }
}