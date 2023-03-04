using Newtonsoft.Json.Linq;

namespace EpicKit.Manifest
{
    public class ChunkMetadataList
    {
        static private string GetChunkDir(uint manifest_version)
        {
            if (manifest_version >= 15)
                return "ChunksV4";

            if (manifest_version >= 6)
                return "ChunksV3";

            if (manifest_version >= 3)
                return "ChunksV2";

            return "Chunks";
        }

        public uint ManifestVersion { get; private set; } = 0;

        public uint Size { get; private set; } = 0;
        public byte Version { get; private set; } = 0;
        public List<ChunkMetadata> ChunkData { get; private set; } = new List<ChunkMetadata>();

        public void SerializeToStream(Stream stream, uint featureLevel)
        {
            var startPos = stream.Position;
            var writer = new BinaryWriter(stream);

            Serializer.WriteUInt(writer, 0);
            Serializer.WriteByte(writer, Version);
            Serializer.WriteUInt(writer, (uint)ChunkData.Count);

            foreach(var chunk in ChunkData)
                Serializer.WriteGuid(writer, chunk.Guid);

            foreach (var chunk in ChunkData)
                Serializer.WriteULong(writer, chunk.Hash);

            foreach (var chunk in ChunkData)
                Serializer.WriteBytes(writer, chunk.Sha1Hash);

            foreach (var chunk in ChunkData)
                Serializer.WriteByte(writer, chunk.GroupNum);

            foreach (var chunk in ChunkData)
                Serializer.WriteUInt(writer, chunk.WindowSize);

            foreach (var chunk in ChunkData)
                Serializer.WriteULong(writer, chunk.FileSize);

            var endPos = stream.Position;
            Size = (uint)(stream.Position - startPos);
            stream.Position = startPos;
            Serializer.WriteUInt(writer, Size);
            stream.Position = endPos;
        }

        public void Read(Stream stream, uint manifest_version)
        {
            long start_pos = stream.Position;
            BinaryReader reader = new BinaryReader(stream);

            ManifestVersion = manifest_version;

            Size = Deserializer.ReadUInt(reader);
            Version = Deserializer.ReadByte(reader);
            int chunk_count = (int)Deserializer.ReadUInt(reader);

            ChunkData = new List<ChunkMetadata>(chunk_count);
            for (int i = 0; i < chunk_count; ++i)
            {
                ChunkData.Add(new ChunkMetadata());
                ChunkData[i].ChunkFormat = GetChunkDir(ManifestVersion);
                ChunkData[i].Guid = Deserializer.ReadGuid(reader);
            }
            for (int i = 0; i < chunk_count; ++i)
            {
                ChunkData[i].Hash = Deserializer.ReadULong(reader);
            }
            for (int i = 0; i < chunk_count; ++i)
            {
                ChunkData[i].Sha1Hash = Deserializer.ReadBytes(reader, 20);
            }
            for (int i = 0; i < chunk_count; ++i)
            {
                ChunkData[i].GroupNum = Deserializer.ReadByte(reader);
            }
            for (int i = 0; i < chunk_count; ++i)
            {
                ChunkData[i].WindowSize = Deserializer.ReadUInt(reader);
            }
            for (int i = 0; i < chunk_count; ++i)
            {
                ChunkData[i].FileSize = Deserializer.ReadULong(reader);
            }

            if ((stream.Position - start_pos) != Size)
            {
                // Adjust position for datas alignement.
                stream.Seek(Size - (stream.Position - start_pos), SeekOrigin.Current);
                Version = 0;
            }
        }

        public void ReadAsJson(JObject json, uint manifest_version)
        {
            var chl = (JObject)json["ChunkHashList"];
            var csl = (JObject)json["ChunkShaList"];
            var dgl = (JObject)json["DataGroupList"];
            var cfl = (JObject)json["ChunkFilesizeList"];

            if (chl.Count != csl.Count || chl.Count != dgl.Count || chl.Count != cfl.Count)
                throw new InvalidDataException();

            ManifestVersion = manifest_version;

            var guids = cfl.Properties().Select(p => p.Name).ToList();
            ChunkData = new List<ChunkMetadata>(cfl.Count);
            int i = 0;
            foreach (var guid in guids)
            {
                ChunkData.Add(new ChunkMetadata());

                ChunkData[i].ChunkFormat = GetChunkDir(ManifestVersion);
                ChunkData[i].Guid = Deserializer.ReadJsonGuid(guid);

                ChunkData[i].Hash = Deserializer.ReadJsonULongBlob((string)chl[guid]);
                ChunkData[i].Sha1Hash = Deserializer.ReadJsonHexString((string)csl[guid]);
                ChunkData[i].GroupNum = Deserializer.ReadJsonByteBlob((string)dgl[guid]);
                ChunkData[i].FileSize = Deserializer.ReadJsonULongBlob((string)cfl[guid]);

                cfl.Remove(guid);
                chl.Remove(guid);
                csl.Remove(guid);
                dgl.Remove(guid);

                ++i;
            }
        }
    }
}