using System;

namespace ImageLOL.NET
{
    public readonly struct LowerBitsReader
    {
        public delegate ReadOnlySpan<byte> ReadFunction(int offset, int length);

        private readonly ReadFunction _readFunction;
        private readonly byte         _dataBitDepth;

        public LowerBitsReader(ReadFunction readFunction, byte dataBitDepth)
        {
            if (dataBitDepth > 7)
            {
                throw new ArgumentOutOfRangeException(nameof(dataBitDepth), "Data bit depth cannot exceed 7 bits");
            }

            _readFunction = readFunction;
            _dataBitDepth = dataBitDepth;
        }

        public void ReadBytes(Span<byte> destination, int offset)
        {
            if (_dataBitDepth == 4)
            {
                FastReadBytes(destination, offset);
            }
            else
            {
                SlowReadBytes(destination, offset);
            }
        }

        private unsafe void FastReadBytes(Span<byte> destination, int offset)
        {
            ReadOnlySpan<byte> data = _readFunction(offset * 8 / _dataBitDepth, destination.Length * 2);

            fixed (byte* dataPtr        = &data[0])
            fixed (byte* destinationPtr = &destination[0])
            {
                byte* destPtr = destinationPtr;
                byte* highPtr = dataPtr;
                byte* lowPtr  = dataPtr + 1;

                for (int i = 0; i < destination.Length; i++)
                {
                    *destPtr = (byte)((*highPtr << 4) | (*lowPtr & 0x0F));

                    destPtr += 1;
                    highPtr += 2;
                    lowPtr  += 2;
                }
            }
        }

        private void SlowReadBytes(Span<byte> destination, int offset)
        {
            int bitOffset = offset    * 8;
            int position  = bitOffset / _dataBitDepth;
            int bitPos    = bitOffset % _dataBitDepth;

            ReadOnlySpan<byte> data = _readFunction(position, destination.Length * (8 / _dataBitDepth + (8 % _dataBitDepth > 0 ? 1 : 0)));
            position = 0;

            for (int i = 0; i < destination.Length; i++)
            {
                destination[i] = 0; // Let's clear this before we OR it so that the user can reuse a temp buffer if they wish

                for (int nBit = 0; nBit < 8; nBit++)
                {
                    destination[i] |= (byte)(((data[position] >> (_dataBitDepth - bitPos - 1)) & 1) << (8 - nBit - 1));

                    if (bitPos >= _dataBitDepth - 1)
                    {
                        position++;
                        bitPos = 0;
                    }
                    else
                    {
                        bitPos++;
                    }
                }
            }
        }

        public ulong ReadUInt64(int offset)
        {
            int   bitOffset = offset * 8;
            int   position  = bitOffset / _dataBitDepth;
            int   bitPos    = bitOffset % _dataBitDepth;
            ulong uint64    = 0;

            ReadOnlySpan<byte> data = _readFunction(position, 16);
            position = 0;

            for (int i = 0; i < sizeof(ulong) * 8; i++)
            {
                uint64 |= (uint)(((data[position] >> (_dataBitDepth - bitPos - 1)) & 1) << (sizeof(ulong) * 8 - i - 1));

                if (bitPos >= _dataBitDepth - 1)
                {
                    position++;
                    bitPos = 0;
                }
                else
                {
                    bitPos++;
                }
            }

            return uint64;
        }
    }
}