using Newtonsoft.Json.Linq;

namespace EpicKit.Manifest
{
    public class ManifestMetadatas
    {
        public uint Size { get; private set; } = 0;
        public byte Version { get; private set; } = 0;
        public uint FeatureLevel { get; private set; } = 0;
        public byte IsFileData { get; private set; } = 0;
        public uint AppId { get; private set; } = 0;
        public string AppName { get; private set; } = string.Empty;
        public string BuildVersion { get; private set; } = string.Empty;
        public string StartExec { get; private set; } = string.Empty;
        public string StartCmd { get; private set; } = string.Empty;
        public List<string> PrereqIds { get; private set; } = new List<string>();
        public string PrereqName { get; private set; } = string.Empty;
        public string PrereqPath { get; private set; } = string.Empty;
        public string PrereqArgs { get; private set; } = string.Empty;
        public string BuildId { get; private set; } = string.Empty;

        public void SerializeToStream(Stream stream)
        {
            var writer = new BinaryWriter(stream);
            var startPos = stream.Position;

            Serializer.WriteUInt(writer, 0);
            Serializer.WriteByte(writer, Version);
            Serializer.WriteUInt(writer, FeatureLevel);
            Serializer.WriteByte(writer, IsFileData);
            Serializer.WriteUInt(writer, AppId);
            Serializer.WriteString(writer, AppName);
            Serializer.WriteString(writer, BuildVersion);
            Serializer.WriteString(writer, StartExec);
            Serializer.WriteString(writer, StartCmd);

            Serializer.WriteUInt(writer, (uint)PrereqIds.Count);

            foreach(var prereq in PrereqIds)
                Serializer.WriteString(writer, prereq);

            Serializer.WriteString(writer, PrereqName);
            Serializer.WriteString(writer, PrereqPath);
            Serializer.WriteString(writer, PrereqArgs);

            if (Version > 0)
                Serializer.WriteString(writer, BuildId);

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
            FeatureLevel = Deserializer.ReadUInt(reader);
            IsFileData = Deserializer.ReadByte(reader);
            AppId = Deserializer.ReadUInt(reader);
            AppName = Deserializer.ReadString(reader);
            BuildVersion = Deserializer.ReadString(reader);
            StartExec = Deserializer.ReadString(reader);
            StartCmd = Deserializer.ReadString(reader);

            int entry_count = (int)Deserializer.ReadUInt(reader);

            PrereqIds = new List<string>(entry_count);
            for (int i = 0; i < entry_count; ++i)
            {
                PrereqIds.Add(Deserializer.ReadString(reader));
            }

            PrereqName = Deserializer.ReadString(reader);
            PrereqPath = Deserializer.ReadString(reader);
            PrereqArgs = Deserializer.ReadString(reader);
            if (Version > 0)
            {
                BuildId = Deserializer.ReadString(reader);
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
            FeatureLevel = json.ContainsKey("ManifestFileVersion") ? Deserializer.ReadJsonUIntBlob((string)json["ManifestFileVersion"]) : 19;
            IsFileData = (byte)((json.ContainsKey("bIsFileData") ? (bool)json["bIsFileData"] : false) ? 1 : 0);
            AppId = json.ContainsKey("AppID") ? Deserializer.ReadJsonUIntBlob((string)json["AppID"]) : 0;
            AppName = json.ContainsKey("AppNameString") ? (string)json["AppNameString"]: string.Empty;
            BuildVersion = json.ContainsKey("BuildVersionString") ? (string)json["BuildVersionString"] : string.Empty;
            StartExec = json.ContainsKey("LaunchExeString") ? (string)json["LaunchExeString"] : string.Empty;
            StartCmd = json.ContainsKey("LaunchCommand") ? (string)json["LaunchCommand"] : string.Empty;
            if (json.ContainsKey("PrereqIds"))
            {
                foreach (var item in (JArray)json["PrereqIds"])
                    PrereqIds.Add((string)item);
            }
            PrereqName = json.ContainsKey("PrereqName") ? (string)json["PrereqName"] : string.Empty;
            PrereqPath = json.ContainsKey("PrereqPath") ? (string)json["PrereqPath"] : string.Empty;
            PrereqArgs = json.ContainsKey("PrereqArgs") ? (string)json["PrereqArgs"] : string.Empty;
        }
    }
}