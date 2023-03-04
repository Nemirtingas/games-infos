
namespace EpicKit
{
    public class ManifestDownloadInfos
    {
        public string ManifestHash { get; set; }
        public byte[] ManifestData { get; set; }
        public List<string> BaseUrls { get; set; } = new List<string>();
        public List<string> ManifestUrls { get; set; } = new List<string>();
    }
}
