
namespace EpicKit.Manifest
{
    public class FileChunk
    {
        public Guid Guid { get; set; } = new Guid();
        public uint Offset { get; set; } = 0;
        public uint Size { get; set; } = 0;
    }
}