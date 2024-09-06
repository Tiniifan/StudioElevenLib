using System;

namespace StudioElevenLib.Level5.Animation.Logic
{
    public class BoneRotation
    {
        public float X { get; set; }
        public float Y { get; set; }
        public float Z { get; set; }
        public float W { get; set; }

        public BoneRotation()
        {

        }

        public BoneRotation(float x, float y, float z, float w)
        {
            X = x;
            Y = y;
            Z = z;
            W = w;
        }

        public byte[] ToByte()
        {
            byte[] bytes = new byte[16];

            byte[] xBytes = BitConverter.GetBytes(X);
            byte[] yBytes = BitConverter.GetBytes(Y);
            byte[] zBytes = BitConverter.GetBytes(Z);
            byte[] wBytes = BitConverter.GetBytes(W);

            Array.Copy(xBytes, 0, bytes, 0, 4);
            Array.Copy(yBytes, 0, bytes, 4, 4);
            Array.Copy(zBytes, 0, bytes, 8, 4);
            Array.Copy(wBytes, 0, bytes, 12, 4);

            return bytes;
        }
    }
}
