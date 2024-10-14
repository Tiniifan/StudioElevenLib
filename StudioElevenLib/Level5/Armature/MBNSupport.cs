using System.Numerics;
using System.Runtime.InteropServices;

namespace StudioElevenLib.Level5.Armature
{
    public class MBNSupport
    {
        [StructLayout(LayoutKind.Sequential, Pack = 1)]
        public struct MBNData
        {
            public uint Name;
            public uint Parent;
            public int Unk;
            public MBNVector3 Location;
            public MBNMatrix3x3 MatrixRotation;
            public MBNVector3 Scale;
            public MBNMatrix3x3 LocalMatrixRotation;
            public MBNVector3 LocationXhead;
            public MBNVector3 FirstColumnOfLocalMatrix;
            public MBNVector3 TailSubstractionHead;
            public MBNVector3 LastColumnOfLocalMatrix;
            public MBNVector3 Head;
        }

        public struct MBNVector3
        {
            public float X;
            public float Y;
            public float Z;
        }

        public struct MBNMatrix3x3
        {
            public float M11;
            public float M12;
            public float M13;
            public float M21;
            public float M22;
            public float M23;
            public float M31;
            public float M32;
            public float M33;
        }

        public static Vector3 MBNVector3ToVector3(MBNVector3 mbnVec)
        {
            return new Vector3(mbnVec.X, mbnVec.Y, mbnVec.Z);
        }

        public static Matrix4x4 MBNMatrix3x3ToMatrix4x4(MBNMatrix3x3 mbnMat)
        {
            return new Matrix4x4(
                mbnMat.M11, mbnMat.M12, mbnMat.M13, 0f,
                mbnMat.M21, mbnMat.M22, mbnMat.M23, 0f,
                mbnMat.M31, mbnMat.M32, mbnMat.M33, 0f,
                0f, 0f, 0f, 1f
            );
        }

        public static MBNVector3 Vector3ToMBNVector3(Vector3 vec)
        {
            return new MBNVector3
            {
                X = vec.X,
                Y = vec.Y,
                Z = vec.Z
            };
        }

        public static MBNMatrix3x3 Matrix4x4ToMBNMatrix3x3(Matrix4x4 mat)
        {
            return new MBNMatrix3x3
            {
                M11 = mat.M11,
                M12 = mat.M12,
                M13 = mat.M13,
                M21 = mat.M21,
                M22 = mat.M22,
                M23 = mat.M23,
                M31 = mat.M31,
                M32 = mat.M32,
                M33 = mat.M33
            };
        }
    }
}
