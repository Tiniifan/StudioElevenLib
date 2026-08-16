using System;

namespace StudioElevenLib.Level5.Animation.Logic
{
    public class MaterialAttribute
    {
        public float Hue { get; set; }
        public float Saturation { get; set; }
        public float Value { get; set; }

        public MaterialAttribute()
        {

        }

        public MaterialAttribute(float x, float y, float z)
        {
            Hue = x;
            Saturation = y;
            Value = z;
        }

        public byte[] ToByte()
        {
            byte[] bytes = new byte[12];

            byte[] xBytes = BitConverter.GetBytes(Hue);
            byte[] yBytes = BitConverter.GetBytes(Saturation);
            byte[] zBytes = BitConverter.GetBytes(Value);

            Array.Copy(xBytes, 0, bytes, 0, 4);
            Array.Copy(yBytes, 0, bytes, 4, 4);
            Array.Copy(zBytes, 0, bytes, 8, 4);

            return bytes;
        }

        public override bool Equals(object obj)
        {
            if (obj is MaterialAttribute other)
            {
                return Hue == other.Hue && Saturation == other.Saturation && Value == other.Value;
            }
            return false;
        }

        public override int GetHashCode()
        {
            return Hue.GetHashCode() + Saturation.GetHashCode() + Value.GetHashCode();
        }
    }
}
