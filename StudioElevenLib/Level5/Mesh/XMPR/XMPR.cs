using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using System.Xml;
using StudioElevenLib.Level5.Mesh.XPVB;
using StudioElevenLib.Level5.Mesh.XPVI;

namespace StudioElevenLib.Level5.Mesh.XMPR
{
    public class XMPR
    {
        public string Name => "XMPR";

        public XPVB.XPVB Vertices { get; set; }
        public XPVI.XPVI Triangles { get; set; }

        public List<uint> Nodes { get; set; }
        public string MeshName { get; set; }
        public string MaterialName { get; set; }
        public uint? SingleBind { get; set; }
        public uint DrawPriority { get; set; }
        public ushort MeshType { get; set; }

        public XMPR()
        {
            Vertices = new XPVB.XPVB();
            Triangles = new XPVI.XPVI();
            Nodes = new List<uint>();
        }

        public XMPR(Stream stream)
        {
            var reader = new XMPRReader(stream);
            var result = reader.Read();

            Vertices = result.Vertices;
            Triangles = result.Triangles;
            Nodes = result.Nodes;
            MeshName = result.MeshName;
            MaterialName = result.MaterialName;
            SingleBind = result.SingleBind;
            DrawPriority = result.DrawPriority;
            MeshType = result.MeshType;
        }

        public XMPR(byte[] fileByteArray)
        {
            using (var ms = new MemoryStream(fileByteArray))
            {
                var reader = new XMPRReader(ms);
                var result = reader.Read();

                Vertices = result.Vertices;
                Triangles = result.Triangles;
                Nodes = result.Nodes;
                MeshName = result.MeshName;
                MaterialName = result.MaterialName;
                SingleBind = result.SingleBind;
                DrawPriority = result.DrawPriority;
                MeshType = result.MeshType;
            }
        }

        public void Save(string fileName, IProgress<int> progress = null)
        {
            var writer = new XMPRWriter(this);
            writer.Save(fileName, progress);
        }

        public byte[] Save(IProgress<int> progress = null)
        {
            var writer = new XMPRWriter(this);
            return writer.Save(progress);
        }
    }
}
