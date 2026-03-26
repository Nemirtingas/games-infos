using SteamKit2;
using System;
using System.Collections.Concurrent;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Runtime.InteropServices;
using System.Security.Cryptography;
using System.Text;
using System.Threading.Tasks;

namespace SteamRetriever;

public static class Util
{
    internal static string GetSteamOS()
    {
        if (RuntimeInformation.IsOSPlatform(OSPlatform.Windows))
        {
            return "windows";
        }

        if (RuntimeInformation.IsOSPlatform(OSPlatform.OSX))
        {
            return "macos";
        }

        if (RuntimeInformation.IsOSPlatform(OSPlatform.Linux))
        {
            return "linux";
        }

        if (RuntimeInformation.IsOSPlatform(OSPlatform.FreeBSD))
        {
            // Return linux as freebsd steam client doesn't exist yet
            return "linux";
        }

        return "unknown";
    }

    internal static string GetSteamArch()
    {
        return Environment.Is64BitOperatingSystem ? "64" : "32";
    }

    public static string ReadPassword()
    {
        ConsoleKeyInfo keyInfo;
        var password = new StringBuilder();

        do
        {
            keyInfo = Console.ReadKey(true);

            if (keyInfo.Key == ConsoleKey.Backspace)
            {
                if (password.Length > 0)
                {
                    password.Remove(password.Length - 1, 1);
                    Console.Write("\b \b");
                }

                continue;
            }

            /* Printable ASCII characters only */
            var c = keyInfo.KeyChar;
            if (c >= ' ' && c <= '~')
            {
                password.Append(c);
                Console.Write('*');
            }
        } while (keyInfo.Key != ConsoleKey.Enter);

        return password.ToString();
    }

    // Validate a file against Steam3 Chunk data
    internal static List<DepotManifest.ChunkData> ValidateSteam3FileChecksums(string filePath, DepotManifest.ChunkData[] chunkdata)
    {
        var neededChunks = new ConcurrentBag<DepotManifest.ChunkData>();

        Parallel.ForEach(chunkdata, data =>
        {
            // Chaque thread ouvre son propre stream en lecture
            using (var localFs = new FileStream(filePath, FileMode.Open, FileAccess.Read, FileShare.Read))
            {
                localFs.Seek((long)data.Offset, SeekOrigin.Begin);

                var adler = AdlerHash(localFs, (int)data.UncompressedLength);
                if (!adler.SequenceEqual(BitConverter.GetBytes(data.Checksum)))
                {
                    neededChunks.Add(data);
                }
            }
        });

        return neededChunks.ToList();
    }

    internal static byte[] AdlerHash(Stream stream, int length)
    {
        uint a = 0, b = 0;
        for (var i = 0; i < length; i++)
        {
            var c = (uint)stream.ReadByte();

            a = (a + c) % 65521;
            b = (b + a) % 65521;
        }

        return BitConverter.GetBytes(a | (b << 16));
    }

    internal static byte[] FileSHAHash(string filename)
    {
        using (var fs = File.Open(filename, FileMode.Open))
        using (var sha = SHA1.Create())
        {
            var output = sha.ComputeHash(fs);

            return output;
        }
    }

    internal static DepotManifest LoadManifestFromFile(string directory, uint depotId, ulong manifestId, bool badHashWarning)
    {
        // Try loading Steam format manifest first.
        var filename = Path.Combine(directory, string.Format("{0}_{1}.manifest", depotId, manifestId));

        if (File.Exists(filename))
        {
            byte[] expectedChecksum;

            try
            {
                expectedChecksum = File.ReadAllBytes(filename + ".sha");
            }
            catch (IOException)
            {
                expectedChecksum = null;
            }

            var currentChecksum = FileSHAHash(filename);

            if (expectedChecksum != null && expectedChecksum.SequenceEqual(currentChecksum))
            {
                return DepotManifest.LoadFromFile(filename);
            }
            else if (badHashWarning)
            {
                Console.WriteLine("Manifest {0} on disk did not match the expected checksum.", manifestId);
            }
        }

        // Try converting legacy manifest format.
        filename = Path.Combine(directory, string.Format("{0}_{1}.bin", depotId, manifestId));

        if (File.Exists(filename))
        {
            byte[] expectedChecksum;

            try
            {
                expectedChecksum = File.ReadAllBytes(filename + ".sha");
            }
            catch (IOException)
            {
                expectedChecksum = null;
            }

            byte[] currentChecksum;
            var oldManifest = ProtoManifest.LoadFromFile(filename, out currentChecksum);

            if (oldManifest != null && (expectedChecksum == null || !expectedChecksum.SequenceEqual(currentChecksum)))
            {
                oldManifest = null;

                if (badHashWarning)
                {
                    Console.WriteLine("Manifest {0} on disk did not match the expected checksum.", manifestId);
                }
            }

            if (oldManifest != null)
            {
                return oldManifest.ConvertToSteamManifest(depotId);
            }
        }

        return null;
    }

    internal static bool SaveManifestToFile(string directory, DepotManifest manifest)
    {
        try
        {
            var filename = Path.Combine(directory, string.Format("{0}_{1}.manifest", manifest.DepotID, manifest.ManifestGID));
            manifest.SaveToFile(filename);
            File.WriteAllBytes(filename + ".sha", FileSHAHash(filename));
            return true; // If serialization completes without throwing an exception, return true
        }
        catch (Exception)
        {
            return false; // Return false if an error occurs
        }
    }

    /// <summary>
    /// Decrypts using AES/ECB/PKCS7
    /// </summary>
    internal static byte[] SymmetricDecryptECB(byte[] input, byte[] key)
    {
        using var aes = Aes.Create();
        aes.BlockSize = 128;
        aes.KeySize = 256;
        aes.Mode = CipherMode.ECB;
        aes.Padding = PaddingMode.PKCS7;

        using var aesTransform = aes.CreateDecryptor(key, null);
        var output = aesTransform.TransformFinalBlock(input, 0, input.Length);

        return output;
    }
}