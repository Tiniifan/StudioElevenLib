using System.Collections.Generic;

#if USE_SYSTEM_DRAWING
using System.Drawing;
#elif USE_IMAGESHARP
using SixLabors.ImageSharp;
#endif

namespace StudioElevenLib.Level5.Image.Swizzles
{
    public class IMGNSwizzle
    {
        private readonly int _origWidth;
        private readonly int _origHeight;

        public IMGNSwizzle(int width, int height)
        {
            _origWidth = (width + 7) & ~7;
            _origHeight = (height + 7) & ~7;
        }

        public IEnumerable<Point> GetPointSequence()
        {
            for (int tileX = 0; tileX < _origWidth; tileX += 8)
            {
                for (int tileY = 0; tileY < _origHeight; tileY += 8)
                {
                    for (int h = 0; h < 64; h++)
                    {
                        int x1 = h / 8;
                        int y1 = h % 8;

                        int origX = tileX + x1;
                        int origY = tileY + y1;

                        yield return new Point(origY, origX);
                    }
                }
            }
        }

    }
}
