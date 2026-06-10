using System.Collections.Generic;

#if USE_SYSTEM_DRAWING
using System.Drawing;
#elif USE_IMAGESHARP
using SixLabors.ImageSharp;
#endif

namespace StudioElevenLib.Level5.Image.Swizzles
{
    public class IMGCSwizzle
    {
        private MasterSwizzle _zorderTrans;

        public int Width { get; }
        public int Height { get; }

        public IMGCSwizzle(int width, int height)
        {
            Width = (width + 0x7) & ~0x7;
            Height = (height + 0x7) & ~0x7;

            _zorderTrans = new MasterSwizzle(Width, new Point(0, 0), new[] { (0, 1), (1, 0), (0, 2), (2, 0), (0, 4), (4, 0) });
        }

        public Point Get(Point point)
        {
            return _zorderTrans.Get(point.Y * Width + point.X);
        }

        public IEnumerable<Point> GetPointSequence()
        {
            for (int i = 0; i < Width * Height; i++)
            {
                var point = new Point(i % Width, i / Width);
                point = Get(point);
                yield return point;
            }
        }
    }
}
