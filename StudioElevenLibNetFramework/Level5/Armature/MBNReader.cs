#nullable enable

using System;
using System.IO;
using System.Numerics;
using StudioElevenLib.Tools;
using StudioElevenLib.Level5.Armature.Logic;

namespace StudioElevenLib.Level5.Armature
{
    /// <summary>
    /// Handles the reading and decoding of MBN binary data.
    /// </summary>
    public class MBNReader
    {
        private readonly Stream _baseStream;

        public MBNReader(Stream stream)
        {
            _baseStream = stream;
        }

        /// <summary>
        /// Reads the stream and reconstructs a Bone object.
        /// </summary>
        public Bone Read()
        {
            using (BinaryDataReader reader = new BinaryDataReader(_baseStream))
            {
                MBNSupport.MBNData header = reader.ReadStruct<MBNSupport.MBNData>();

                // Convert header data to readable Bone properties
                // CRC32 hashes are represented as Hex strings since the original name is lost
                string name = header.Name.ToString("X8");
                Bone? parent = header.Parent != 0 ? new Bone(header.Parent.ToString("X8")) : null;

                Vector3 location = MBNSupport.MBNVector3ToVector3(header.Location);
                Matrix4x4 rotation = MBNSupport.MBNMatrix3x3ToMatrix4x4(header.MatrixRotation);
                Vector3 scale = MBNSupport.MBNVector3ToVector3(header.Scale);

                // Initialize the bone. (Bone constructor automatically handles Quaternion inversion)
                Bone bone = new Bone(name, location, rotation, scale, false, parent);

                // Manually overwrite Head and Tail
                bone.Head = MBNSupport.MBNVector3ToVector3(header.Head);

                Vector3 tailSubHead = MBNSupport.MBNVector3ToVector3(header.TailSubstractionHead);
                bone.Tail = new Vector3(
                    tailSubHead.X + bone.Head.X,
                    tailSubHead.Y + bone.Head.Y,
                    tailSubHead.Z + bone.Head.Z
                );

                return bone;
            }
        }
    }
}