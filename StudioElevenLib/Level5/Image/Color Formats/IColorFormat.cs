#if USE_SYSTEM_DRAWING
using System.Drawing;
#elif USE_IMAGESHARP
using SixLabors.ImageSharp;
#endif

namespace StudioElevenLib.Level5.Image
{
    public interface IColorFormat
    {
        string Name { get; }

        int Size { get; }

        byte[] Encode(Color color);

        Color Decode(byte[] data);
    }
}