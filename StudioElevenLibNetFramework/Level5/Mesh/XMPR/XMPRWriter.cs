using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using StudioElevenLib.Tools;

namespace StudioElevenLib.Level5.Mesh.XMPR
{
    public class XMPRWriter
    {
        private readonly XMPR _xmpr;

        public XMPRWriter(XMPR xmpr)
        {
            _xmpr = xmpr;
        }

        public void Save(string fileName, IProgress<int> progress = null)
        {
            File.WriteAllBytes(fileName, Save(progress));
        }

        public byte[] Save(IProgress<int> progress = null)
        {
            byte[] xpvbData = _xmpr.Vertices.Save(progress);
            byte[] xpviData = _xmpr.Triangles.Save(progress);

            // Material Block
            byte[] material;
            using (var msMat = new MemoryStream())
            using (var matWriter = new BinaryDataWriter(msMat))
            {
                matWriter.Write(Crc32.Compute(Encoding.GetEncoding("Shift-JIS").GetBytes(_xmpr.MeshName)));
                matWriter.Write(Crc32.Compute(Encoding.GetEncoding("Shift-JIS").GetBytes(_xmpr.MaterialName)));

                // A model that only sets SingleBind keeps the previous behavior: #FIX_IMG and the bone as parent node
                uint renderProgramHash = _xmpr.RenderProgramHash != 0 ? _xmpr.RenderProgramHash : (_xmpr.SingleBind.HasValue ? 0x547E69F1u : 0u);
                uint parentNodeHash = _xmpr.ParentNodeHash != 0 ? _xmpr.ParentNodeHash : (_xmpr.SingleBind ?? 0u);

                matWriter.Write(renderProgramHash);
                matWriter.Write(parentNodeHash);

                matWriter.Write(new byte[8]); // Zeros
                matWriter.Write(0.0f); matWriter.Write(0.0f); matWriter.Write(0.0f); // texspace_loc
                matWriter.Write(1.0f); matWriter.Write(1.0f); matWriter.Write(1.0f); // texspace_size
                matWriter.Write(_xmpr.DrawPriority);
                matWriter.Write(_xmpr.MeshType);
                matWriter.Write(_xmpr.Nodes.Count);
                material = msMat.ToArray();
            }

            // Node Block
            byte[] node;
            using (var msNode = new MemoryStream())
            using (var nodeWriter = new BinaryDataWriter(msNode))
            {
                foreach (var name in _xmpr.Nodes)
                {
                    nodeWriter.Write(name);
                }
                node = msNode.ToArray();
            }

            // Name Block
            byte[] xmprName;
            using (var msName = new MemoryStream())
            using (var nameWriter = new BinaryDataWriter(msName))
            {
                nameWriter.Write(Encoding.GetEncoding("shift-jis").GetBytes(_xmpr.MeshName));
                nameWriter.Write((byte)0);
                nameWriter.Write(Encoding.GetEncoding("shift-jis").GetBytes(_xmpr.MaterialName));
                nameWriter.Write((byte)0);
                xmprName = msName.ToArray();
            }

            // Assemble XMPR
            using (MemoryStream finalMs = new MemoryStream())
            using (BinaryDataWriter xmprWriter = new BinaryDataWriter(finalMs))
            {
                xmprWriter.Write(new byte[] { 0x58, 0x4D, 0x50, 0x52 }); // XMPR
                xmprWriter.Write(64);
                xmprWriter.Write(84 + xpvbData.Length + xpviData.Length);
                xmprWriter.Write(0);

                for (int i = 0; i < 3; i++)
                {
                    xmprWriter.Write(84 + xpvbData.Length + xpviData.Length + material.Length);
                    xmprWriter.Write(0);
                }

                xmprWriter.Write(84 + xpvbData.Length + xpviData.Length + material.Length + node.Length);
                xmprWriter.Write(_xmpr.Nodes.Count);
                xmprWriter.Write(84 + xpvbData.Length + xpviData.Length + material.Length + node.Length + xmprName.Length);

                xmprWriter.Write(_xmpr.MeshName.Length + 1);
                xmprWriter.Write(84 + xpvbData.Length + xpviData.Length + material.Length + node.Length + xmprName.Length + 4);
                xmprWriter.Write(_xmpr.MaterialName.Length + 1);

                xmprWriter.Write(new byte[] { 0x58, 0x50, 0x52, 0x4D }); // XPRM
                xmprWriter.Write(20);
                xmprWriter.Write(xpvbData.Length);
                xmprWriter.Write(xpvbData.Length + 20);
                xmprWriter.Write(xpviData.Length);

                xmprWriter.Write(xpvbData);
                xmprWriter.Write(xpviData);
                xmprWriter.Write(material);
                xmprWriter.Write(node);
                xmprWriter.Write(xmprName);

                return finalMs.ToArray();
            }
        }
    }
}
