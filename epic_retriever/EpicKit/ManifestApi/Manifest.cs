using System.Security.Cryptography;
using Newtonsoft.Json.Linq;

namespace EpicKit.Manifest
{
    public class Manifest
    {
        [Flags]
        public enum ManifestStoreFlags : byte
        {
            None = 0x00,
            Deflate = 0x01,
        }

        public const uint ManifestMagic = 0x44BEC00C;

        public uint Magic { get; private set; } = 0;
        public uint Size { get; private set; } = 0;
        public uint SizeUncompressed { get; private set; } = 0;
        public uint SizeCompressed { get; private set; } = 0;
        public byte[] Sha1Hash { get; private set; } = new byte[20];
        public ManifestStoreFlags StoreFlags { get; private set; } = ManifestStoreFlags.None;
        public uint Version { get; private set; } = 0;

        public ManifestMetadatas Metadatas { get; private set; } = new ManifestMetadatas();
        public ChunkMetadataList ChunkDataList { get; private set; } = new ChunkMetadataList();
        public FileManifestList FileManifestList { get; private set; } = new FileManifestList();
        public CustomFieldList CustomFieldsList { get; private set; } = new CustomFieldList();
        public bool IsValid => Magic == ManifestMagic;

        public void SerializeToStream(Stream stream)
        {
            using (var manifestDataStream = new MemoryStream())
            {
                var startPos = stream.Position;

                Metadatas.SerializeToStream(manifestDataStream);
                ChunkDataList.SerializeToStream(manifestDataStream, Metadatas.FeatureLevel);
                FileManifestList.SerializeToStream(manifestDataStream);
                CustomFieldsList.SerializeToStream(manifestDataStream);

                var endPos = manifestDataStream.Position;
                manifestDataStream.Position = 0;
                Sha1Hash = SHA1.Create().ComputeHash(manifestDataStream);
                manifestDataStream.Position = 0;

                StoreFlags = ManifestStoreFlags.Deflate;

                using (var compressedStream = Tools.CompressManifestData(manifestDataStream, StoreFlags))
                {
                    var writer = new BinaryWriter(stream);
                    Size = 4 + 4 + 4 + 4 + 20 + 1 + 4;  // uint, uint, uint, uint, sha1, flags, version
                    SizeUncompressed = (uint)(endPos - startPos);
                    SizeCompressed = (uint)compressedStream.Position;

                    Serializer.WriteUInt(writer, ManifestMagic);
                    Serializer.WriteUInt(writer, Size);
                    Serializer.WriteUInt(writer, SizeUncompressed);
                    Serializer.WriteUInt(writer, SizeCompressed);
                    Serializer.WriteBytes(writer, Sha1Hash);
                    Serializer.WriteByte(writer, (byte)StoreFlags);
                    Serializer.WriteUInt(writer, Version);

                    compressedStream.Position = 0;
                    compressedStream.CopyTo(stream);

                    stream.Position = 0;
                }
            }
        }

        public void Read(Stream stream)
        {
            long start_pos = stream.Position;

            BinaryReader reader = new BinaryReader(stream);

            Magic = Deserializer.ReadUInt(reader);
            if (!IsValid)
            {
                stream.Position = start_pos;
                ReadAsJson(stream);
                return;
            }

            Size = Deserializer.ReadUInt(reader);
            SizeUncompressed = Deserializer.ReadUInt(reader);
            SizeCompressed = Deserializer.ReadUInt(reader);
            Sha1Hash = Deserializer.ReadBytes(reader, 20);
            StoreFlags = (ManifestStoreFlags)Deserializer.ReadByte(reader);
            Version = Deserializer.ReadUInt(reader);

            if((stream.Position - start_pos) != Size)
            {
                // Adjust position for datas alignement.
                stream.Seek(Size - (stream.Position - start_pos), SeekOrigin.Current);
            }

            // Old stream is managed by caller, discard
            using (MemoryStream m_stream = Tools.ReadCompressedData(stream, StoreFlags))
            {
                SHA1 sha1 = SHA1.Create();
                byte[] h = sha1.ComputeHash(m_stream);

                if(!h.SequenceEqual(Sha1Hash))
                    throw new InvalidDataException("Manifest data doesn't match sha1.");

                m_stream.Position = 0;

                Metadatas.Read(m_stream);
                ChunkDataList.Read(m_stream, Metadatas.FeatureLevel);
                FileManifestList.Read(m_stream);
                CustomFieldsList.Read(m_stream);
            }
        }

        private void ReadAsJson(Stream stream)
        {
            JObject manifest;

            using (var reader = new StreamReader(stream))
            {
                try
                {
                    manifest = JObject.Parse(reader.ReadToEnd());
                }
                catch
                {
                    throw new InvalidDataException("Header doesn't match Magic number and manifest is not a json manifest.");
                }
            }

            try
            {
                Metadatas.ReadAsJson(manifest);
                ChunkDataList.ReadAsJson(manifest, Metadatas.FeatureLevel);
                FileManifestList.ReadAsJson(manifest);
                CustomFieldsList.ReadAsJson(manifest);
            }
            catch
            {

            }
        }
    }
}
