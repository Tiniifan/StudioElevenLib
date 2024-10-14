using System.Numerics;

namespace StudioElevenLib.Level5.Armature.Logic
{
    public class Bone
    {
        public string Name { get; set; }
        public bool UseDeform { get; set; }
        public Bone Parent { get; set; }
        public Vector3 Location { get; set; }
        public Quaternion Rotation { get; set; }
        public Vector3 Scale { get; set; }
        public Vector3 Head { get; set; }
        public Vector3 Tail { get; set; }
        public Matrix4x4 Matrix { get; set; }

        public Bone(string name, Vector3 location, Quaternion rotation, Vector3 scale, bool useDeform = false, Bone parent = null)
        {
            Name = name;
            Location = location;
            Rotation = rotation;
            Scale = scale;
            UseDeform = useDeform;
            Parent = parent;

            // Calculate the matrix
            Matrix = Matrix4x4.CreateScale(Scale) * Matrix4x4.CreateFromQuaternion(Rotation) * Matrix4x4.CreateTranslation(Location);

            // Calculate Head and Tail based on the matrix
            Head = Vector3.Transform(Vector3.Zero, Matrix);
            Tail = Vector3.Transform(Vector3.UnitZ, Matrix);
        }

        public Bone(string name, Vector3 location, Matrix4x4 rotation, Vector3 scale, bool useDeform = false, Bone parent = null)
        {
            Name = name;
            Location = location;
            Rotation = Quaternion.Inverse(new Quaternion(rotation.M11, rotation.M12, rotation.M13, rotation.M44));
            Scale = scale;
            UseDeform = useDeform;
            Parent = parent;

            // Calculate the matrix
            Matrix = Matrix4x4.CreateScale(Scale) * Matrix4x4.CreateFromQuaternion(Rotation) * Matrix4x4.CreateTranslation(Location);

            // Calculate Head and Tail based on the matrix
            Head = Vector3.Transform(Vector3.Zero, Matrix);
            Tail = Vector3.Transform(Vector3.UnitZ, Matrix);
        }

        public Bone(string name, Vector3 location, Vector3 eulerRotation, Vector3 scale, bool useDeform = false, Bone parent = null)
        {
            Name = name;
            Location = location;
            Scale = scale;
            UseDeform = useDeform;
            Parent = parent;

            // Convert Euler rotations to quaternion
            Rotation = Quaternion.CreateFromYawPitchRoll(eulerRotation.Y, eulerRotation.X, eulerRotation.Z);

            // Calculate the matrix
            Matrix = Matrix4x4.CreateScale(Scale) * Matrix4x4.CreateFromQuaternion(Rotation) * Matrix4x4.CreateTranslation(Location);

            // Calculate Head and Tail based on the matrix
            Head = Vector3.Transform(Vector3.Zero, Matrix);
            Tail = Vector3.Transform(Vector3.UnitZ, Matrix);
        }

        public Bone(string name, Vector3 location, Vector3 rotationAxis, float angle, Vector3 scale, bool useDeform = false, Bone parent = null)
        {
            Name = name;
            Location = location;
            Scale = scale;
            UseDeform = useDeform;
            Parent = parent;

            // Create a quaternion from a rotation axis and an angle
            Rotation = Quaternion.CreateFromAxisAngle(rotationAxis, angle);

            // Calculate the matrix
            Matrix = Matrix4x4.CreateScale(Scale) * Matrix4x4.CreateFromQuaternion(Rotation) * Matrix4x4.CreateTranslation(Location);

            // Calculate Head and Tail based on the matrix
            Head = Vector3.Transform(Vector3.Zero, Matrix);
            Tail = Vector3.Transform(Vector3.UnitZ, Matrix);
        }

        public Bone()
        {
        }

        public Bone(string name)
        {
            Name = name;
        }
    }
}
