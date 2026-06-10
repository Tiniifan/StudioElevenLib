#if USE_SYSTEM_DRAWING
using System.Drawing;
#elif USE_IMAGESHARP
using SixLabors.ImageSharp;
#endif

namespace StudioElevenLib.Common.Imaging
{
    public interface IPixelFormat
    {
        string Name { get; }

        int Size { get; }

        bool IsBlock { get; }

        byte[] Encode(Color color);

        Color Decode(byte[] data);
    }
}