using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace StudioElevenLib.Level5.Mesh.XPVB
{
    public class XPVB
    {
        public List<XPVBSupport.Vertex> Vertices { get; set; }

        public XPVB()
        {
            Vertices = new List<XPVBSupport.Vertex>();
        }

        public XPVB(Stream stream, List<uint> nodeTable = null)
        {
            var reader = new XPVBReader(stream, nodeTable);
            Vertices = reader.Read();
        }

        public byte[] Save(IProgress<int> progress = null)
        {
            var writer = new XPVBWriter(this);
            return writer.Save(progress);
        }
    }
}
