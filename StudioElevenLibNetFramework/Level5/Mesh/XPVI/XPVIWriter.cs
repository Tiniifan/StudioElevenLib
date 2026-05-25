using System;
using System.IO;
using System.Text;
using SharpTriStrip;
using StudioElevenLib.Tools;
using StudioElevenLib.Level5.Compression;

namespace StudioElevenLib.Level5.Mesh.XPVI
{
    public class XPVIWriter
    {
        private readonly XPVI _xpvi;
        private readonly bool _useTriStrip;

        public XPVIWriter(XPVI xpvi, bool useTriStrip = true)
        {
            _xpvi = xpvi;
            _useTriStrip = useTriStrip;
        }

        public byte[] Save(IProgress<int> progress = null)
        {
            ushort primitiveType = _useTriStrip ? (ushort)2 : (ushort)0;
            ushort[] finalIndices;

            if (_useTriStrip)
            {
                ushort[] inputIndices = _xpvi.Triangles.ToArray();

                TriStrip stripifier = new TriStrip();
                stripifier.SetStitchStrips(true);

                bool success = stripifier.GenerateStrips(inputIndices, out TriStrip.PrimitiveGroup[] groups);

                if (success && groups.Length > 0)
                {
                    finalIndices = groups[0].Indices;
                }
                else
                {
                    finalIndices = inputIndices;
                    primitiveType = 0;
                }
            }
            else
            {
                finalIndices = _xpvi.Triangles.ToArray();
            }

            uint faceCount = (uint)finalIndices.Length;

            using (MemoryStream msIndices = new MemoryStream())
            using (BinaryDataWriter writer = new BinaryDataWriter(msIndices))
            {
                foreach (ushort indice in finalIndices)
                {
                    writer.Write(indice);
                }

                byte[] dataTriangle = msIndices.ToArray();
                byte[] compressTriangle = Compressor.Compress(dataTriangle);

                using (MemoryStream finalMs = new MemoryStream())
                using (BinaryDataWriter xpviWriter = new BinaryDataWriter(finalMs))
                {
                    xpviWriter.Write(Encoding.UTF8.GetBytes("XPVI"));
                    xpviWriter.Write(primitiveType);
                    xpviWriter.Write((ushort)12);
                    xpviWriter.Write(faceCount);

                    xpviWriter.Write(compressTriangle);

                    return finalMs.ToArray();
                }
            }
        }
    }
}