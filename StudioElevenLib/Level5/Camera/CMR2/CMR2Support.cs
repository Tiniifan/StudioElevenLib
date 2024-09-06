using System;
using System.Runtime.InteropServices;

namespace StudioElevenLib.Level5.Camera.CMR2
{
    public class CMR2Support
    {
        public struct Header
        {
            public UInt32 Magic;
            public int Offset;
            public int Unk1;
            public int Unk2;
            public int Unk3;
            public int Unk4;
            public int Unk5;
            public int Unk6;
        }

        public struct Header2
        {
            public uint Hash;
            public int Unk1;
            public int FramesCount;
            public int Unk2;
            public float CameraSpeed;
            public int Unk4;
            public int Unk5;
            public int Unk6;
            public int Unk7;
            public int Unk8;
        }

        public struct Header3
        {
            public uint Hash1;
            public uint Hash2;
            public uint Hash3;
            public short Unk1;
            public short Unk2;
            public short Unk3;
            public short Unk4;
            public short Unk5;
            public short Unk6;
            [MarshalAs(UnmanagedType.ByValArray, SizeConst = 4)]
            public Pattern[] UnkPattern;
            [MarshalAs(UnmanagedType.ByValArray, SizeConst = 8)]
            public byte[] Unk7;
        }

        public struct Pattern
        {
            public short Unk1;
            public short Unk2;
            public short Unk3;
            public short FramesCount;
        }

        public struct CameraHeader
        {
            public int CameraDataOffset;
            public int Unk1;
            public int CameraStartDataOffset;
            public int CameraDataLength;
        }
    }
}
