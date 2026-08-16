using System;

namespace StudioElevenLib.Level5.Animation.Logic
{
    public class BoneBool
    {
        public int Enable { get; set; }

        public BoneBool()
        {

        }

        public BoneBool(int enable)
        {
            Enable = enable;
        }

        public byte[] ToByte()
        {
            byte[] bytes = new byte[1];
            bytes[0] = Convert.ToByte(Enable);
            return bytes;
        }

        public override bool Equals(object obj)
        {
            if (obj is BoneBool other)
            {
                return Enable == other.Enable;
            }
            return false;
        }

        public override int GetHashCode()
        {
            return Enable.GetHashCode();
        }
    }
}
