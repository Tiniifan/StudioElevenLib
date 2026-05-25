using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using StudioElevenLib.Tools;

namespace StudioElevenLib.Level5.Mesh.XMPR
{
    public class XMPRReader
    {
        private readonly Stream _baseStream;

        public XMPRReader(Stream stream)
        {
            _baseStream = stream;
        }

        public XMPR Read()
        {
            var xmpr = new XMPR();

            using (BinaryDataReader reader = new BinaryDataReader(_baseStream))
            {
                var header = reader.ReadStruct<XMPRSupport.XMPRHeader>();

                long xprmOffset = reader.Position;
                var xprm = reader.ReadStruct<XMPRSupport.XPRMHeader>();

                // Properties
                reader.Seek(header.PropertiesOffset);
                uint meshNameHash = reader.ReadValue<uint>();
                uint matNameHash = reader.ReadValue<uint>();
                uint unkHash = reader.ReadValue<uint>();
                uint meshNameSplitHash = reader.ReadValue<uint>();
                reader.Seek(reader.Position + 32); // Skip 32 unk bytes
                xmpr.DrawPriority = reader.ReadValue<uint>();
                xmpr.MeshType = reader.ReadValue<ushort>();
                ushort meshUnk = reader.ReadValue<ushort>();
                uint nodesCount = reader.ReadValue<uint>();

                // Nodes
                if (header.NodesLength != 0)
                {
                    reader.Seek(header.NodesOffset);
                    for (int i = 0; i < nodesCount; i++)
                    {
                        xmpr.Nodes.Add(reader.ReadValue<uint>());
                    }
                }

                if (header.NodesLength == 0)
                {
                    xmpr.SingleBind = meshNameSplitHash;
                }

                // Names
                reader.Seek(header.MeshNameOffset);
                xmpr.MeshName = reader.ReadString(Encoding.GetEncoding("shift-jis"));

                reader.Seek(header.MaterialNameOffset);
                xmpr.MaterialName = reader.ReadString(Encoding.GetEncoding("shift-jis"));

                // Read Buffers
                reader.Seek(xprmOffset + xprm.XpvbOffset);
                byte[] xpvbData = reader.ReadMultipleValue<byte>((int)xprm.XpvbLength);
                using (var ms = new MemoryStream(xpvbData))
                {
                    xmpr.Vertices = new XPVB.XPVB(ms, xmpr.Nodes);
                }

                reader.Seek(xprmOffset + xprm.XpviOffset);
                byte[] xpviData = reader.ReadMultipleValue<byte>((int)xprm.XpviLength);
                using (var ms = new MemoryStream(xpviData))
                {
                    xmpr.Triangles = new XPVI.XPVI(ms);
                }
            }

            return xmpr;
        }
    }
}
