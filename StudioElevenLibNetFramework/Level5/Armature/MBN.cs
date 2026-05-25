using System.IO;
using System.Text;
using System.Numerics;
using StudioElevenLib.Tools;
using StudioElevenLib.Level5.Armature.Logic;

namespace StudioElevenLib.Level5.Armature
{
    public class MBN
    {
        public static Bone Open(byte[] data)
        {
            if (data.Length == 0)
                return null;

            using (BinaryDataReader reader = new BinaryDataReader(data))
            {
                MBNSupport.MBNData header = reader.ReadStruct<MBNSupport.MBNData>();

                // Convert header data to readable Bone properties
                string name = header.Name.ToString("X8");
                Bone parent = header.Parent != 0 ? new Bone(header.Parent.ToString("X8")) : null;
                Vector3 location = MBNSupport.MBNVector3ToVector3(header.Location);
                Matrix4x4 rotation = MBNSupport.MBNMatrix3x3ToMatrix4x4(header.MatrixRotation);
                Vector3 scale = MBNSupport.MBNVector3ToVector3(header.Scale);

                return new Bone(name, location, rotation, scale, false, parent);
            }
        }

        public static byte[] Write(Bone bone)
        {
            using (MemoryStream stream = new MemoryStream())
            using (BinaryDataWriter writer = new BinaryDataWriter(stream))
            {
                Bone parent = bone.Parent;
                while (parent != null)
                {
                    // Stop when we find a deforming parent bone
                    if (parent.UseDeform)
                        break;

                    parent = parent.Parent;
                }

                Matrix4x4 poseMatrix = bone.Matrix;
                Matrix4x4 localMatrix = poseMatrix;

                // Adjust the pose matrix with the parent's inverted matrix
                if (parent != null)
                {
                    Matrix4x4 parentMatrix = parent.Matrix;
                    Matrix4x4.Invert(parentMatrix, out parentMatrix);
                    poseMatrix = parentMatrix * poseMatrix;
                }

                // Extract the rotation from the local matrix
                Matrix4x4 localMatrixRotation = Matrix4x4.CreateFromQuaternion(Quaternion.CreateFromRotationMatrix(localMatrix));

                // Organize the local matrix rotation into a 2D array
                float[,] localMatrixRotationOrdered = new float[3, 3];
                localMatrixRotationOrdered[0, 0] = localMatrixRotation.M11;
                localMatrixRotationOrdered[0, 1] = localMatrixRotation.M12;
                localMatrixRotationOrdered[0, 2] = localMatrixRotation.M13;
                localMatrixRotationOrdered[1, 0] = localMatrixRotation.M21;
                localMatrixRotationOrdered[1, 1] = localMatrixRotation.M22;
                localMatrixRotationOrdered[1, 2] = localMatrixRotation.M23;
                localMatrixRotationOrdered[2, 0] = localMatrixRotation.M31;
                localMatrixRotationOrdered[2, 1] = localMatrixRotation.M32;
                localMatrixRotationOrdered[2, 2] = localMatrixRotation.M33;

                // Write the structured data
                writer.WriteStruct(new MBNSupport.MBNData
                {
                    // Name and parent are encoded using Crc32
                    Name = Crc32.Compute(Encoding.GetEncoding("Shift-JIS").GetBytes(bone.Name)),
                    Parent = parent != null ? Crc32.Compute(Encoding.GetEncoding("Shift-JIS").GetBytes(parent.Name)) : 0,
                    Unk = 4,

                    // Convert location, rotation, and scale
                    Location = MBNSupport.Vector3ToMBNVector3(poseMatrix.Translation),
                    MatrixRotation = MBNSupport.Matrix4x4ToMBNMatrix3x3(
                        Matrix4x4.CreateFromQuaternion(Quaternion.CreateFromRotationMatrix(poseMatrix))),
                    Scale = MBNSupport.Vector3ToMBNVector3(new Vector3(
                        new Vector3(poseMatrix.M11, poseMatrix.M12, poseMatrix.M13).Length(),
                        new Vector3(poseMatrix.M21, poseMatrix.M22, poseMatrix.M23).Length(),
                        new Vector3(poseMatrix.M31, poseMatrix.M32, poseMatrix.M33).Length()
                    )),

                    // Handle local rotation, location, and matrix columns
                    LocalMatrixRotation = MBNSupport.Matrix4x4ToMBNMatrix3x3(localMatrixRotation),
                    LocationXhead = MBNSupport.Vector3ToMBNVector3(MatrixVectorMultiply(localMatrixRotationOrdered, bone.Head)),
                    FirstColumnOfLocalMatrix = MBNSupport.Vector3ToMBNVector3(new Vector3(
                        localMatrixRotation.M11, localMatrixRotation.M21, localMatrixRotation.M31)),
                    TailSubstractionHead = MBNSupport.Vector3ToMBNVector3(new Vector3(
                        bone.Tail.X - bone.Head.X, bone.Tail.Y - bone.Head.Y, bone.Tail.Z - bone.Head.Z)),
                    LastColumnOfLocalMatrix = MBNSupport.Vector3ToMBNVector3(new Vector3(
                        localMatrixRotation.M13, localMatrixRotation.M23, localMatrixRotation.M33)),
                    Head = MBNSupport.Vector3ToMBNVector3(bone.Head),
                });

                return stream.ToArray();
            }
        }

        // Helper function to multiply a 3x3 matrix with a vector
        private static Vector3 MatrixVectorMultiply(float[,] matrix, Vector3 vector)
        {
            Vector3 result = new Vector3();
            for (int i = 0; i < 3; i++)
            {
                for (int j = 0; j < 3; j++)
                {
                    if (i == 0) result.X += matrix[i, j] * (j == 0 ? vector.X : j == 1 ? vector.Y : vector.Z);
                    if (i == 1) result.Y += matrix[i, j] * (j == 0 ? vector.X : j == 1 ? vector.Y : vector.Z);
                    if (i == 2) result.Z += matrix[i, j] * (j == 0 ? vector.X : j == 1 ? vector.Y : vector.Z);
                }
            }
            return result;
        }
    }
}