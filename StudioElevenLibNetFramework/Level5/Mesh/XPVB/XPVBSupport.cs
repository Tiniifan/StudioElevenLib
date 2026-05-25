using System;
using System.Collections.Generic;
using System.Linq;
using System.Numerics;
using System.Text;
using System.Threading.Tasks;

namespace StudioElevenLib.Level5.Mesh.XPVB
{
    public static class XPVBSupport
    {
        public struct Vertex
        {
            public Vector3 Position;
            public Vector3 Normal;
            public Vector2 UV0;
            public Vector2 UV1;
            public Vector4 Weights;
            public Vector4 BoneIndices;
            public Vector4 Color;
        }
    }
}
