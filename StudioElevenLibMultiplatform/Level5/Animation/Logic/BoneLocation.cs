using System;

namespace StudioElevenLib.Level5.Animation.Logic
{
    public class BoneLocation
    {
        public float X { get; set; }
        public float Y { get; set; }
        public float Z { get; set; }

        public BoneLocation()
        {

        }

        public BoneLocation(float x, float y, float z)
        {
            X = x;
            Y = y;
            Z = z;
        }

        public override string ToString()
        {
            return "(" + X + ", " + Y + ", " + Z + ")";
        }

        public byte[] ToByte()
        {
            byte[] bytes = new byte[12];

            byte[] xBytes = BitConverter.GetBytes(X);
            byte[] yBytes = BitConverter.GetBytes(Y);
            byte[] zBytes = BitConverter.GetBytes(Z);

            Array.Copy(xBytes, 0, bytes, 0, 4);
            Array.Copy(yBytes, 0, bytes, 4, 4);
            Array.Copy(zBytes, 0, bytes, 8, 4);

            return bytes;
        }

        public override bool Equals(object obj)
        {
            if (obj is BoneLocation other)
            {
                return X == other.X && Y == other.Y && Z == other.Z;
            }
            return false;
        }

        public override int GetHashCode()
        {
            return X.GetHashCode() + Y.GetHashCode() + Z.GetHashCode();
        }
    }
}
