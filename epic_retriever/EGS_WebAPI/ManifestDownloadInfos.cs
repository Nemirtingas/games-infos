using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace EGS
{
    internal class ManifestDownloadInfos
    {
        public string ManifestHash { get; set; }
        public byte[] ManifestData { get; set; }
        public List<string> BaseUrls { get; set; } = new List<string>();
        public List<string> ManifestUrls { get; set; } = new List<string>();
    }
}
