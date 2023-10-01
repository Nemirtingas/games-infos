using System.Collections.Concurrent;
using System.IO;
using System.IO.Compression;
using System.IO.IsolatedStorage;
using ProtoBuf;

namespace steam_retriever
{
    [ProtoContract]
    class AccountSettingsData
    {
        [ProtoMember(1, IsRequired = true)]
        public uint Version { get; init; }
        [ProtoMember(2, IsRequired = false)]
        public ConcurrentDictionary<string, int> ContentServerPenalty { get; set; }
        [ProtoMember(3, IsRequired = false)]
        public ConcurrentDictionary<string, string> RefreshToken { get; set; }
        [ProtoMember(4, IsRequired = false)]
        public ConcurrentDictionary<string, byte[]> SentryData { get; set; }
    }

    class AccountSettingsStore
    {
        private const uint CurrentVersion = 1;

        public AccountSettingsData Settings { get; private set; }

        string FileName;

        AccountSettingsStore()
        {
            Settings = new AccountSettingsData
            {
                Version = CurrentVersion,
                ContentServerPenalty = new ConcurrentDictionary<string, int>(),
                RefreshToken = new ConcurrentDictionary<string, string>(),
                SentryData = new ConcurrentDictionary<string, byte[]>(),
            };
        }

        readonly IsolatedStorageFile IsolatedStorage = IsolatedStorageFile.GetUserStoreForAssembly();
        private static AccountSettingsStore _instance;
        public static AccountSettingsStore Instance { get => _instance ??= new AccountSettingsStore(); }

        public void LoadFromFile(string filename)
        {
            if (IsolatedStorage.FileExists(filename))
            {
                try
                {
                    var ms = new MemoryStream();
                    using (var fs = IsolatedStorage.OpenFile(filename, FileMode.Open, FileAccess.Read))
                    using (var ds = new DeflateStream(fs, CompressionMode.Decompress))
                    {
                        ds.CopyTo(ms);
                    }

                    ms.Seek(0, SeekOrigin.Begin);
                    var version = Serializer.Deserialize<uint>(ms);
                    ms.Seek(0, SeekOrigin.Begin);
                    switch (version)
                    {
                        case 1: HandleVersion1(ms); break;
                    }
                }
                catch (IOException ex)
                {
                    Program.Instance._logger.Error($"Failed to load account settings: {ex.Message}");
                }
                catch (ProtoException ex)
                {
                    Program.Instance._logger.Error($"Failed to load deserialize protobuf: {ex.Message}");
                }
            }

            Instance.FileName = filename;
        }

        public void Save()
        {
            try
            {
                using (var fs = IsolatedStorage.OpenFile(Instance.FileName, FileMode.Create, FileAccess.Write))
                using (var ds = new DeflateStream(fs, CompressionMode.Compress))
                {
                    Serializer.Serialize(ds, Instance.Settings);
                }
            }
            catch (IOException ex)
            {
                Program.Instance._logger.Error("Failed to save account settings: {0}", ex);
            }
        }

        private void HandleVersion1(MemoryStream ms)
        {
            Settings = Serializer.Deserialize<AccountSettingsData>(ms);
            if (Settings == null)
                Settings = new AccountSettingsData();

            Settings.ContentServerPenalty ??= new ConcurrentDictionary<string, int>();
            Settings.RefreshToken ??= new ConcurrentDictionary<string, string>();
            Settings.SentryData ??= new ConcurrentDictionary<string, byte[]>();
        }
    }
}
