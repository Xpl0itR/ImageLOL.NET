using System;
using System.IO;
using System.Runtime.InteropServices;
using ImageLOL.NET;
using SixLabors.ImageSharp;
using SixLabors.ImageSharp.Advanced;
using SixLabors.ImageSharp.Formats.Png;
using SixLabors.ImageSharp.Memory;
using SixLabors.ImageSharp.PixelFormats;

namespace ConsoleApp1
{
    public static class Program
    {
        public static void Main(string[] args)
        {
            using FileStream   inStream = File.OpenRead("D:\\Libraries\\Downloads\\archlinux-2021.03.01-x86_64.iso.png");
            using Image<Rgb24> image    = Image.Load<Rgb24>(inStream, new PngDecoder());

            Span<byte>      rgbData = image.GetAllRgbBytes();
            ImageLolDecoder decoder = new ImageLolDecoder(rgbData.ToArray());

            Span<byte> decoded = new byte[decoder.FileLength];
            decoder.Decode(decoded);

            using FileStream outStream = File.OpenWrite(Path.Combine("D:\\Libraries\\Downloads", decoder.FileName));
            outStream.Write(decoded);
        }

        public static Span<byte> GetRowRgbBytes<TPixel>(this Image<TPixel> image, int rowIndex) 
            where TPixel : unmanaged, IPixel<TPixel> =>
                MemoryMarshal.AsBytes(image.GetPixelRowSpan(rowIndex));

        public static Span<byte> GetAllRgbBytes<TPixel>(this Image<TPixel> image)
            where TPixel : unmanaged, IPixel<TPixel>
        {
            IMemoryGroup<TPixel> memoryGroup = image.GetPixelMemoryGroup();
            Span<TPixel> pixels;

            if (memoryGroup.Count == 1)
            {
                pixels = memoryGroup[0].Span;
            }
            else
            {
                pixels = new TPixel[image.Width * image.Height];

                int offset = 0;
                foreach (Memory<TPixel> memory in memoryGroup)
                {
                    memory.Span.CopyTo(pixels.Slice(offset));
                    offset += memory.Span.Length;
                }
            }

            return MemoryMarshal.AsBytes(pixels);
        }
    }
}