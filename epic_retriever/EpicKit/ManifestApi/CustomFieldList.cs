using Newtonsoft.Json.Linq;

namespace EpicKit.Manifest
{
    public class CustomFieldList
    {
        internal class CustomFieldItem
        {
            public string Key;
            public string Value;
        }

        public uint Size { get; private set; } = 0;
        public byte Version { get; private set; } = 0;
        public Dictionary<string, string> CustomFields { get; private set; } = new Dictionary<string, string>();

        public void SerializeToStream(Stream stream)
        {
            var startPos = stream.Position;
            var writer = new BinaryWriter(stream);

            Serializer.WriteUInt(writer, 0);
            Serializer.WriteByte(writer, Version);
            Serializer.WriteUInt(writer, (uint)CustomFields.Count);

            foreach (var field in CustomFields)
                Serializer.WriteString(writer, field.Key);

            foreach (var field in CustomFields)
                Serializer.WriteString(writer, field.Value);

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

            int field_count = (int)Deserializer.ReadUInt(reader);

            CustomFields = new Dictionary<string, string>();
            var customFields = new List<CustomFieldItem>(field_count);
            for (int i = 0; i < field_count; ++i)
            {
                customFields.Add(new CustomFieldItem
                {
                    Key = Deserializer.ReadString(reader)
                });
            }
            for (int i = 0; i < field_count; ++i)
            {
                customFields[i].Value = Deserializer.ReadString(reader);
            }

            customFields.ForEach(e =>
            {
                CustomFields.Add(e.Key, e.Value);
            });

            if ((stream.Position - start_pos) != Size)
            {
                // Adjust position for datas alignement.
                stream.Seek(Size - (stream.Position - start_pos), SeekOrigin.Current);
                Version = 0;
            }
        }

        public void ReadAsJson(JObject json)
        {
            CustomFields = new Dictionary<string, string>();
            if (json.ContainsKey("CustomFields"))
            {
                foreach (var customField in (JObject)json["CustomFields"])
                {
                    CustomFields.Add(customField.Key, (string)customField.Value);
                }
            }
        }
    }
}