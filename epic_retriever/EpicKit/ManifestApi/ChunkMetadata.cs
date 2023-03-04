
namespace EpicKit.Manifest
{
    public class ChunkMetadata
    {
        public string ChunkFormat { get; set; } = string.Empty;

        public Guid Guid { get; set; } = new Guid();
        public ulong Hash { get; set; } = 0;
        public byte[] Sha1Hash { get; set; } = new byte[20];
        public byte GroupNum { get; set; } = 0;
        public uint WindowSize { get; set; } = 0;
        public ulong FileSize { get; set; } = 0;

        public string Path() => string.Format("{0}/{1:d2}/{2:X16}_{3}.chunk",
                ChunkFormat,
                new CRC32().Get(Guid.Bytes) % 100,
                Hash,
                Guid.ToString());
    }
}