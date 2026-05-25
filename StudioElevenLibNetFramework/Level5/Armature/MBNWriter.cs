#nullable enable

using System;
using System.IO;
using System.Text;
using System.Numerics;
using StudioElevenLib.Tools;
using StudioElevenLib.Level5.Armature.Logic;

namespace StudioElevenLib.Level5.Armature
{
    /// <summary>
    /// Handles the encoding and writing of MBN binary data.
    /// </summary>
    public class MBNWriter
    {
        private readonly Bone _bone;

        public MBNWriter(MBN mbn)
        {
            _bone = mbn.Bone!;
        }

        public void Save(string fileName)
        {
            using (var stream = new FileStream(fileName, FileMode.Create, FileAccess.Write))
            {
                WriteToStream(stream);
            }
        }

        public byte[] Save()
        {
            using (var ms = new MemoryStream())
            {
                WriteToStream(ms);
                return ms.ToArray();
            }
        }

        private void WriteToStream(Stream stream)
        {
            using (BinaryDataWriter writer = new BinaryDataWriter(stream))
            {
                Bone? parent = _bone.Parent;
                while (parent != null)
                {
                    // Stop when we find a deforming parent bone
                    if (parent.UseDeform)
                        break;

                    parent = parent.Parent;
                }

                Matrix4x4 poseMatrix = _bone.Matrix;
                Matrix4x4 localMatrix = poseMatrix;

                // Adjust the pose matrix to be relative to the parent's inverted matrix
                if (parent != null)
                {
                    Matrix4x4 parentMatrix = parent.Matrix;
                    Matrix4x4.Invert(parentMatrix, out parentMatrix);
                    poseMatrix = parentMatrix * poseMatrix;
                }

                // Determine "Unk" value based on (specific bone names)
                int unkValue = (_bone.Name == "billboard" || _bone.Name == "cam_rot") ? 5 : 4;

                // Extract rotation from the matrices (normalized via quaternions)
                Matrix4x4 poseMatrixRotation = Matrix4x4.CreateFromQuaternion(Quaternion.CreateFromRotationMatrix(poseMatrix));
                Matrix4x4 localMatrixRotation = Matrix4x4.CreateFromQuaternion(Quaternion.CreateFromRotationMatrix(localMatrix));

                // Calculate rotated head
                Vector3 rotatedHead = Vector3.Transform(_bone.Head, Matrix4x4.Transpose(localMatrixRotation));
                rotatedHead = -rotatedHead; // Multiply by -1

                // Structure the data to write
                var mbnData = new MBNSupport.MBNData
                {
                    Name = Crc32.Compute(Encoding.GetEncoding("Shift-JIS").GetBytes(_bone.Name)),
                    Parent = parent != null ? Crc32.Compute(Encoding.GetEncoding("Shift-JIS").GetBytes(parent.Name)) : 0,
                    Unk = unkValue,

                    // Location is rounded to 4 decimals
                    Location = MBNSupport.Vector3ToMBNVector3(MBNSupport.Round4(poseMatrix.Translation)),

                    // Write the main rotation matrix transposed (column-major)
                    MatrixRotation = MBNSupport.Matrix4x4ToMBNMatrix3x3Transposed(poseMatrixRotation),

                    Scale = MBNSupport.Vector3ToMBNVector3(new Vector3(
                        new Vector3(poseMatrix.M11, poseMatrix.M12, poseMatrix.M13).Length(),
                        new Vector3(poseMatrix.M21, poseMatrix.M22, poseMatrix.M23).Length(),
                        new Vector3(poseMatrix.M31, poseMatrix.M32, poseMatrix.M33).Length()
                    )),

                    // Local rotation is written row-major, rounded to 4 decimals
                    LocalMatrixRotation = MBNSupport.Matrix4x4ToMBNMatrix3x3Rounded(localMatrixRotation),

                    LocationXhead = MBNSupport.Vector3ToMBNVector3(rotatedHead),

                    FirstColumnOfLocalMatrix = MBNSupport.Vector3ToMBNVector3(new Vector3(
                        localMatrixRotation.M11, localMatrixRotation.M21, localMatrixRotation.M31)),

                    TailSubstractionHead = MBNSupport.Vector3ToMBNVector3(new Vector3(
                        _bone.Tail.X - _bone.Head.X, _bone.Tail.Y - _bone.Head.Y, _bone.Tail.Z - _bone.Head.Z)),

                    LastColumnOfLocalMatrix = MBNSupport.Vector3ToMBNVector3(new Vector3(
                        localMatrixRotation.M13, localMatrixRotation.M23, localMatrixRotation.M33)),

                    Head = MBNSupport.Vector3ToMBNVector3(_bone.Head)
                };

                writer.WriteStruct(mbnData);
            }
        }
    }
}