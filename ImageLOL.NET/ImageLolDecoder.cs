using System;
using System.Text;

namespace ImageLOL.NET
{
    public readonly struct ImageLolDecoder
    {
        public string FileName   { get; }
        public ulong  FileLength { get; }
        
        private readonly LowerBitsReader _reader;
        private readonly int _dataPos;

        public ImageLolDecoder(byte[] rgb24Bytes)
        {
            byte dataBitDepth = CountOneBits(rgb24Bytes[0]);

            _reader = new LowerBitsReader((offset, length) =>
                    new ReadOnlySpan<byte>(rgb24Bytes, 1 + offset, length),
                dataBitDepth);

            int position = 0;

            ulong nameLength = _reader.ReadUInt64(position);
            position += sizeof(ulong);

            Span<byte> filename = new byte[nameLength];
            _reader.ReadBytes(filename, position);
            position += filename.Length;
            FileName = Encoding.ASCII.GetString(filename);

            FileLength = _reader.ReadUInt64(position);
            position += sizeof(ulong);

            _dataPos = position;
        }

        public void Decode(Span<byte> destination)
        {
            _reader.ReadBytes(destination, _dataPos);
        }

        private static byte CountOneBits(int b)
        {
            byte count = 0;

            while (b != 0)
            {
                count++;
                b &= b - 1;
            }

            return count;
        }
    }
}