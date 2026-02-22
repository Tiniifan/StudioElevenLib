using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace StudioElevenLib.Level5.Mesh.Logic
{
    public class Face
    {
        public int Vertex { get; set; }
        public int Normal { get; set; }
        public int Texture { get; set; }
        public int Color { get; set; }

        public Face(int vertex, int normal, int texture, int color)
        {
            Vertex = vertex;
            Normal = normal;
            Texture = texture;
            Color = color;
        }

        public Face() 
        { 

        }
    }
}
