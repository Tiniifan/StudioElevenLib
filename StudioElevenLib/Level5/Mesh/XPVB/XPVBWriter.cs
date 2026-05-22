using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using StudioElevenLib.Level5.Compression;
using StudioElevenLib.Tools;

namespace StudioElevenLib.Level5.Mesh.XPVB
{
    public class XPVBWriter
    {
        private readonly XPVB _xpvb;

        public XPVBWriter(XPVB xpvb)
        {
            _xpvb = xpvb;
        }

        public byte[] Save(IProgress<int> progress = null)
        {
            const byte TYPE_FLOAT = 2;
            int vertexCount = _xpvb.Vertices.Count;

            // Detect which attributes are actually used by the vertices
            bool hasNormal = _xpvb.Vertices.Any(v => v.Normal != default);
            bool hasUV0 = _xpvb.Vertices.Any(v => v.UV0 != default);
            bool hasUV1 = _xpvb.Vertices.Any(v => v.UV1 != default);
            bool hasWeights = _xpvb.Vertices.Any(v => v.Weights != default);
            bool hasBoneIndices = _xpvb.Vertices.Any(v => v.BoneIndices != default);
            bool hasColor = _xpvb.Vertices.Any(v => v.Color != default);

            // Initialize attribute table: 10 slots * 4 bytes [count, offset, size, type]
            var attTable = new byte[10 * 4];
            int currentOffset = 0;

            // Helper to populate the attribute table and advance the local byte offset
            void AddAttribute(int slot, int count, int byteSize)
            {
                attTable[slot * 4 + 0] = (byte)count;
                attTable[slot * 4 + 1] = (byte)currentOffset;
                attTable[slot * 4 + 2] = (byte)byteSize;
                attTable[slot * 4 + 3] = TYPE_FLOAT;
                currentOffset += byteSize;
            }

            // Register present attributes into their specific slots
            AddAttribute(0, 3, 12);                                  // Slot 0: Position
            if (hasNormal) AddAttribute(2, 3, 12);                   // Slot 2: Normal
            if (hasUV0) AddAttribute(4, 2, 8);                       // Slot 4: UV0
            if (hasUV1) AddAttribute(5, 2, 8);                       // Slot 5: UV1
            if (hasWeights) AddAttribute(7, 4, 16);                  // Slot 7: Weights
            if (hasBoneIndices) AddAttribute(8, 4, 16);              // Slot 8: Bone Indices
            if (hasColor) AddAttribute(9, 4, 16);                    // Slot 9: Color

            // Total size (in bytes) of a single vertex
            int stride = currentOffset;

            // Compress the generated attribute table
            byte[] attCompressed = Compressor.Compress(attTable);

            // Write vertex data dynamically based on available attributes
            byte[] dataGeometrie;
            using (var msGeom = new MemoryStream())
            using (var geomWriter = new BinaryDataWriter(msGeom))
            {
                foreach (var v in _xpvb.Vertices)
                {
                    if (attTable[0 * 4] > 0) { geomWriter.Write(v.Position.X); geomWriter.Write(v.Position.Y); geomWriter.Write(v.Position.Z); }
                    if (attTable[2 * 4] > 0) { geomWriter.Write(v.Normal.X); geomWriter.Write(v.Normal.Y); geomWriter.Write(v.Normal.Z); }
                    // Note: UV Y-axis is inverted (1.0 - Y)
                    if (attTable[4 * 4] > 0) { geomWriter.Write(v.UV0.X); geomWriter.Write(1.0f - v.UV0.Y); }
                    if (attTable[5 * 4] > 0) { geomWriter.Write(v.UV1.X); geomWriter.Write(1.0f - v.UV1.Y); }
                    if (attTable[7 * 4] > 0) { geomWriter.Write(v.Weights.X); geomWriter.Write(v.Weights.Y); geomWriter.Write(v.Weights.Z); geomWriter.Write(v.Weights.W); }
                    if (attTable[8 * 4] > 0) { geomWriter.Write(v.BoneIndices.X); geomWriter.Write(v.BoneIndices.Y); geomWriter.Write(v.BoneIndices.Z); geomWriter.Write(v.BoneIndices.W); }
                    if (attTable[9 * 4] > 0) { geomWriter.Write(v.Color.X); geomWriter.Write(v.Color.Y); geomWriter.Write(v.Color.Z); geomWriter.Write(v.Color.W); }
                }
                dataGeometrie = msGeom.ToArray();
            }

            // Compress the full vertex geometry buffer
            byte[] compressGeometrie = Compressor.Compress(dataGeometrie);

            // Fixed metadata block required by the format
            byte[] unkBytes = { 0x81, 0x00, 0x00, 0x00, 0x08, 0x00, 0x00, 0x80, 0x3F, 0x90, 0x03, 0x00 };

            // Calculate file offsets for the header
            ushort attBufferOffset = 16;
            ushort unkOffset = (ushort)(attBufferOffset + attCompressed.Length);
            ushort vertexBufferOffset = (ushort)(unkOffset + unkBytes.Length);

            // Build the final binary file
            using (var finalMs = new MemoryStream())
            using (var xpvbWriter = new BinaryDataWriter(finalMs))
            {
                // Write Header (16 bytes)
                xpvbWriter.Write(new byte[] { 0x58, 0x50, 0x56, 0x42 }); // Magic: "XPVB"
                xpvbWriter.Write(attBufferOffset);
                xpvbWriter.Write(unkOffset);
                xpvbWriter.Write(vertexBufferOffset);
                xpvbWriter.Write((ushort)stride);
                xpvbWriter.Write((uint)vertexCount);

                // Write Data blocks
                xpvbWriter.Write(attCompressed);
                xpvbWriter.Write(unkBytes);
                xpvbWriter.Write(compressGeometrie);

                return finalMs.ToArray();
            }
        }
    }
}
