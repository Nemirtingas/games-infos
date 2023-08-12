using System.IO.Compression;

namespace EpicKit.Manifest
{
    internal class Tools
    {
        static internal MemoryStream StreamReadToEnd(Stream stream)
        {
            const int bufferSize = 4096;

            MemoryStream ms = new MemoryStream();
            byte[] buffer = new byte[bufferSize];
            int count;
            while ((count = stream.Read(buffer, 0, buffer.Length)) != 0)
                ms.Write(buffer, 0, count);

            ms.Position = 0;
            return ms;
        }

        static internal MemoryStream CompressManifestData(Stream stream, Manifest.ManifestStoreFlags store_flags)
        {
            var memStream = default(MemoryStream);

            if (store_flags.HasFlag(Manifest.ManifestStoreFlags.Deflate))
            {
                memStream = new MemoryStream();
                var pos = stream.Position;
                memStream.WriteByte(0x78);
                memStream.WriteByte(0x9c);
                byte[] data = StreamReadToEnd(stream).ToArray();
                using (var dstream = new DeflateStream(memStream, CompressionMode.Compress, leaveOpen: true))
                {
                    dstream.Write(data);
                }

                uint a1 = 1, a2 = 0;
                foreach (byte b in data)
                {
                    a1 = (a1 + b) % 65521;
                    a2 = (a2 + a1) % 65521;
                }
                // append the checksum-trailer:
                memStream.WriteByte((byte)(a2 >> 8));
                memStream.WriteByte((byte)a2);
                memStream.WriteByte((byte)(a1 >> 8));
                memStream.WriteByte((byte)a1);

                stream.Position = pos;
            }

            return memStream;
        }

        static internal MemoryStream ReadCompressedData(Stream stream, Manifest.ManifestStoreFlags store_flags)
        {
            MemoryStream data_stream = StreamReadToEnd(stream);

            if (store_flags.HasFlag(Manifest.ManifestStoreFlags.Deflate))
            {
                using (MemoryStream tmp_ms = data_stream)
                {
                    try
                    {
                        DeflateStream dstream = new DeflateStream(tmp_ms, CompressionMode.Decompress);
                        data_stream = StreamReadToEnd(dstream);
                    }
                    catch (InvalidDataException)
                    {
                        tmp_ms.Seek(0, SeekOrigin.Begin);
                        byte[] x = new byte[2];
                        tmp_ms.Read(x);
                        DeflateStream dstream = new DeflateStream(tmp_ms, CompressionMode.Decompress);
                        data_stream = StreamReadToEnd(dstream);
                    }
                }
            }

            return data_stream;
        }
    }
}