namespace EpicKit.Manifest
{
    public class Hash
    {
        private const ulong polynomial = 0xC96C5795D7870F42ul;

        private ulong[] _HashTable;

        public Hash()
        {
            _HashTable = Enumerable.Range(0, 256).Select(i =>
            {
                ulong v = (ulong)i;
                for (int j = 0; j < 8; ++j)
                {
                    if ((v & 1) != 0)
                    {
                        v >>= 1;
                        v ^= polynomial;
                    }
                    else
                    {
                        v >>= 1;
                    }
                }
                return v;
            }).ToArray();
        }

        public ulong Get<T>(IEnumerable<T> byteStream)
        {
            try
            {
                // Initialize checksumRegister to 0 and calculate the checksum.
                return byteStream.Aggregate(0ul, (checksumRegister, currentByte) =>
                          (checksumRegister << 1 | checksumRegister >> 63) ^ _HashTable[Convert.ToByte(currentByte)]);
            }
            catch (FormatException e)
            {
                throw new InvalidDataException("Could not read the stream out as bytes.", e);
            }
            catch (InvalidCastException e)
            {
                throw new InvalidDataException("Could not read the stream out as bytes.", e);
            }
            catch (OverflowException e)
            {
                throw new InvalidDataException("Could not read the stream out as bytes.", e);
            }
        }
    }
}