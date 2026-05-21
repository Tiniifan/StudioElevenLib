#if USE_SYSTEM_DRAWING
using System.Drawing;
#elif USE_IMAGESHARP
using SixLabors.ImageSharp;
using SixLabors.ImageSharp.PixelFormats;
#endif

namespace StudioElevenLib.Level5.Image.Color_Formats
{
    public class ETC1 : IColorFormat
    {
        public string Name => "ETC1";

        public int Size => 3;

        public byte[] Encode(Color color)
        {
            // Not implemented
            return null;
        }

        public Color Decode(byte[] data)
        {
#if USE_SYSTEM_DRAWING
            return Color.FromArgb(255, data[0], data[1], data[2]);
#elif USE_IMAGESHARP
            return Color.FromPixel(new Rgba32(data[0], data[1], data[2], 255));
#endif
        }
    }
}
