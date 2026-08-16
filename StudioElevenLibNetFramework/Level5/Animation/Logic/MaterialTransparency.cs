using System;

namespace StudioElevenLib.Level5.Animation.Logic
{
    public class MaterialTransparency
    {
        public float Transparency { get; set; }

        public MaterialTransparency()
        {

        }

        public MaterialTransparency(float transparency)
        {
            Transparency = transparency;
        }

        public byte[] ToByte()
        {
            return BitConverter.GetBytes(Transparency);
        }

        public override bool Equals(object obj)
        {
            if (obj is MaterialTransparency other)
            {
                return Transparency == other.Transparency;
            }
            return false;
        }

        public override int GetHashCode()
        {
            return Transparency.GetHashCode();
        }
    }
}
