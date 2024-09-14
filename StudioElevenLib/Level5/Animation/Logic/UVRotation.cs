using System;

namespace StudioElevenLib.Level5.Animation.Logic
{
    public class UVRotation
    {
        public float Degree { get; set; }

        public UVRotation()
        {

        }

        public UVRotation(float degree)
        {
            Degree = degree;
        }

        public byte[] ToByte()
        {
            return BitConverter.GetBytes(Degree);
        }

        public override bool Equals(object obj)
        {
            if (obj is UVRotation other)
            {
                return Degree == other.Degree;
            }
            return false;
        }

        public override int GetHashCode()
        {
            return Degree.GetHashCode();
        }
    }
}
