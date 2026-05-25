using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace StudioElevenLib.Level5.Mesh.XPVI
{
    public class XPVI
    {
        public List<ushort> Triangles { get; set; }

        public XPVI()
        {
            Triangles = new List<ushort>();
        }

        public XPVI(Stream stream)
        {
            var reader = new XPVIReader(stream);
            Triangles = reader.Read();
        }

        public byte[] Save(IProgress<int> progress = null)
        {
            var writer = new XPVIWriter(this);
            return writer.Save(progress);
        }
    }
}
