using System;

namespace StudioElevenLib.Level5.Animation.Logic
{
    public class Unk
    {
        public int Enable { get; set; }

        public Unk()
        {

        }

        public Unk(int enable)
        {
            Enable = enable;
        }

        public byte[] ToByte()
        {
            byte[] bytes = new byte[1];
            bytes[0] = Convert.ToByte(Enable);
            return bytes;
        }
    }
}
