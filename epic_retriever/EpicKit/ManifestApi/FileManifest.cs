namespace EpicKit.Manifest
{
    public class FileManifest
    {
        public string Filename { get; set; } = string.Empty;
        public string SymlinkTarget { get; set; } = string.Empty;
        public byte[] Sha1Hash { get; set; } = new byte[20];
        public byte Flags { get; set; } = 0;
        public List<string> InstallTags { get; set; } = new List<string>();
        public List<FileChunk> FileChunks { get; set; } = new List<FileChunk>();
        public byte[] Md5Hash { get; set; } = new byte[16];
        public string MimeType { get; set; } = string.Empty;
        public byte[] Sha256Hash { get; set; } = new byte[32];

        // Custom attributes
        public ulong FileSize { get; set; } = 0;
    }
}