using System;

namespace StudioElevenLib.Level5.Animation.Logic
{
    public class UVScale
    {
        public float X { get; set; }
        public float Y { get; set; }

        public UVScale()
        {

        }

        public UVScale(float x, float y)
        {
            X = x;
            Y = y;
        }

        public byte[] ToByte()
        {
            byte[] bytes = new byte[8];

            byte[] xBytes = BitConverter.GetBytes(X);
            byte[] yBytes = BitConverter.GetBytes(Y);

            Array.Copy(xBytes, 0, bytes, 0, 4);
            Array.Copy(yBytes, 0, bytes, 4, 4);

            return bytes;
        }
    }
}
