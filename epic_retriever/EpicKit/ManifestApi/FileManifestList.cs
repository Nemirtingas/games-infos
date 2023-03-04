using Newtonsoft.Json.Linq;

namespace EpicKit.Manifest
{
    public class FileManifestList
    {
        public uint Size { get; private set; } = 0;
        public byte Version { get; private set; } = 0;

        public List<FileManifest> FileManifests { get; private set; } = new List<FileManifest>();

        public void SerializeToStream(Stream stream)
        {
            var startPos = stream.Position;
            var writer = new BinaryWriter(stream);

            Serializer.WriteUInt(writer, 0);
            Serializer.WriteByte(writer, Version);
            Serializer.WriteUInt(writer, (uint)FileManifests.Count);

            foreach (var fileManifest in FileManifests)
                Serializer.WriteString(writer, fileManifest.Filename);

            foreach (var fileManifest in FileManifests)
                Serializer.WriteString(writer, fileManifest.SymlinkTarget);

            foreach (var fileManifest in FileManifests)
                Serializer.WriteBytes(writer, fileManifest.Sha1Hash);

            foreach (var fileManifest in FileManifests)
                Serializer.WriteByte(writer, fileManifest.Flags);

            foreach (var fileManifest in FileManifests)
            {
                Serializer.WriteUInt(writer, (uint)fileManifest.InstallTags.Count);
                foreach(var tag in fileManifest.InstallTags)
                    Serializer.WriteString(writer, tag);
            }

            foreach (var fileManifest in FileManifests)
            {
                Serializer.WriteUInt(writer, (uint)fileManifest.FileChunks.Count);
                foreach (var chunk in fileManifest.FileChunks)
                {
                    Serializer.WriteUInt(writer, 4 + 16 + 4 + 4); // Size + Guid + Offset + Chunk Size
                    Serializer.WriteGuid(writer, chunk.Guid);
                    Serializer.WriteUInt(writer, chunk.Offset);
                    Serializer.WriteUInt(writer, chunk.Size);
                }
            }

            if (Version > 0)
            {
                foreach (var fileManifest in FileManifests)
                    Serializer.WriteBytes(writer, fileManifest.Md5Hash);

                foreach (var fileManifest in FileManifests)
                    Serializer.WriteString(writer, fileManifest.MimeType);
            }

            if (Version > 1)
            {
                foreach (var fileManifest in FileManifests)
                    Serializer.WriteBytes(writer, fileManifest.Sha256Hash);
            }

            var endPos = stream.Position;
            Size = (uint)(stream.Position - startPos);
            stream.Position = startPos;
            Serializer.WriteUInt(writer, Size);
            stream.Position = endPos;
        }

        public void Read(Stream stream)
        {
            long start_pos = stream.Position;
            BinaryReader reader = new BinaryReader(stream);

            Size = Deserializer.ReadUInt(reader);
            Version = Deserializer.ReadByte(reader);

            int manifest_count = (int)Deserializer.ReadUInt(reader);

            FileManifests = new List<FileManifest>(manifest_count);

            for (int i = 0; i < manifest_count; ++i)
            {
                FileManifests.Add(new FileManifest());
                FileManifests[i].Filename = Deserializer.ReadString(reader);
            }
            for (int i = 0; i < manifest_count; ++i)
            {
                FileManifests[i].SymlinkTarget = Deserializer.ReadString(reader);
            }
            for (int i = 0; i < manifest_count; ++i)
            {
                FileManifests[i].Sha1Hash = Deserializer.ReadBytes(reader, 20);
            }
            for (int i = 0; i < manifest_count; ++i)
            {
                FileManifests[i].Flags = Deserializer.ReadByte(reader);
            }
            for (int i = 0; i < manifest_count; ++i)
            {
                int tag_count = (int)Deserializer.ReadUInt(reader);

                FileManifests[i].InstallTags = new List<string>(tag_count);
                for (int j = 0; j < tag_count; ++j)
                {
                    FileManifests[i].InstallTags.Add(Deserializer.ReadString(reader));
                }
            }
            for (int i = 0; i < manifest_count; ++i)
            {
                int chunk_count = (int)Deserializer.ReadUInt(reader);

                FileManifests[i].FileSize = 0;
                FileManifests[i].FileChunks = new List<FileChunk>(chunk_count);

                for (int j = 0; j < chunk_count; ++j)
                {
                    long chunk_start_pos = stream.Position;
                    uint chunk_part_size = Deserializer.ReadUInt(reader);

                    FileManifests[i].FileChunks.Add(new FileChunk());
                    FileManifests[i].FileChunks[j].Guid = Deserializer.ReadGuid(reader);
                    FileManifests[i].FileChunks[j].Offset = Deserializer.ReadUInt(reader);
                    FileManifests[i].FileChunks[j].Size = Deserializer.ReadUInt(reader);
                    FileManifests[i].FileSize += FileManifests[i].FileChunks[j].Size;

                    if ((stream.Position - chunk_start_pos) != chunk_part_size)
                    {
                        // Adjust position for datas alignement.
                        stream.Seek(chunk_part_size - (stream.Position - chunk_start_pos), SeekOrigin.Current);
                    }
                }
            }

            if (Version > 0)
            {
                for (int i = 0; i < manifest_count; ++i)
                {
                    // if has md5 sum
                    if (Deserializer.ReadUInt(reader) != 0)
                    {
                        FileManifests[i].Md5Hash = Deserializer.ReadBytes(reader, 16);
                    }
                }
                for (int i = 0; i < manifest_count; ++i)
                {
                    FileManifests[i].MimeType = Deserializer.ReadString(reader);
                }
            }

            if (Version > 1)
            {
                for (int i = 0; i < manifest_count; ++i)
                {
                    FileManifests[i].Sha256Hash = Deserializer.ReadBytes(reader, 32);
                }
            }

            if ((stream.Position - start_pos) != Size)
            {
                // Adjust position for datas alignement.
                stream.Seek(Size - (stream.Position - start_pos), SeekOrigin.Current);
                Version = 0;
            }
        }

        public void ReadAsJson(JObject json)
        {
            var fml = (JArray)json["FileManifestList"];

            int i = 0;
            FileManifests = new List<FileManifest>(fml.Count);
            foreach (JObject fileManifest in fml)
            {
                FileManifests.Add(new FileManifest());

                FileManifests[i].Filename = (string)fileManifest["Filename"];
                FileManifests[i].Sha1Hash = Deserializer.ReadJsonBytesBlob((string)fileManifest["FileHash"]);

                int j = 0;
                FileManifests[i].FileChunks = new List<FileChunk>(((JArray)fileManifest["FileChunkParts"]).Count);
                foreach (JObject fileChunk in (JArray)fileManifest["FileChunkParts"])
                {
                    FileManifests[i].FileChunks.Add(new FileChunk());

                    FileManifests[i].FileChunks[j].Guid = Deserializer.ReadJsonGuid((string)fileChunk["Guid"]);
                    FileManifests[i].FileChunks[j].Offset = Deserializer.ReadJsonUIntBlob((string)fileChunk["Offset"]);
                    FileManifests[i].FileChunks[j].Size = Deserializer.ReadJsonUIntBlob((string)fileChunk["Size"]);


                    FileManifests[i].FileSize += FileManifests[i].FileChunks[j].Size;
                    ++j;
                }

                ++i;
            }
        }
    }
}