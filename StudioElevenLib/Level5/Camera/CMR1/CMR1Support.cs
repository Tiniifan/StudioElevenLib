namespace StudioElevenLib.Level5.Camera.CMR1
{
    public class CMR1Support
    {
        public struct Header
        {
            public uint Magic;
            public int DataOffset;
            public int DataSkipOffset;
            public int Unk1;
            public int Unk2;
            public int Unk3;
            public int Unk4;
            public int Unk5;
            public uint AnimationHash;
            public int EmptyBlock1;
            public int FrameCount;
            public int Unk6;
            public float CamSpeed;
        }

        public struct CameraHeader
        {
            public int CameraOffset;
            public int GhostFrameOffset;
            public int FrameOffset;
            public int DataOffset;
            public int BlockLength;
        }

        public struct CameraDataHeader
        {
            public uint Unk1;
            public byte Unk2;
            public byte Unk3;
            public short Unk4;
            public int Unk5;
            public int FrameCount;
            public int DataCount;
            public int GhostFrameCount;
            public int DataSize;
            public int DataByteLength;
            public int DataBlockSize;
            public int GhostFrameLength;
            public int FrameLength;
            public int DataLength;
        }
    }
}
