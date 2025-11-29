using System;
using System.IO;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using StudioElevenLib.Tools;
using StudioElevenLib.Level5.Compression;
using StudioElevenLib.Level5.Compression.NoCompression;
using StudioElevenLib.Level5.Resource;
using StudioElevenLib.Level5.Resource.Types;

namespace StudioElevenLib.Level5.Resource.XRES
{
    public class XRES : IResource
    {
        public string Name => "XRES";

        private Dictionary<string, uint> StringTable { get; set; }

        public Dictionary<RESType, List<RESElement>> Items { get; set; }

        public XRES(Stream stream)
        {
            //StringTable = new List<string>();
            //Items = new Dictionary<RESType, List<RESElement>>();

            //using (BinaryDataReader reader = new BinaryDataReader(Compressor.Decompress(stream)))
            //{
            //    XRESSupport.Header header = reader.ReadStruct<XRESSupport.Header>();

            //    reader.Seek(header.StringOffset);
            //    using (BinaryDataReader textReader = new BinaryDataReader(reader.GetSection((int)(reader.Length - reader.Position))))
            //    {
            //        while (textReader.Position < textReader.Length)
            //        {
            //            string name = textReader.ReadString(Encoding.GetEncoding(932));

            //            if (name != "" && name != " ")
            //            {
            //                StringTable.Add(name);
            //            }
            //        }
            //    }

            //    //Items.Add(RESType.Bone, ReadType(reader, header.Bone, RESType.Bone));
            //    //Items.Add(RESType.Textproj, ReadType(reader, header.Textproj, RESType.Textproj));
            //    //Items.Add(RESType.Properties, ReadType(reader, header.Properties, RESType.Properties));
            //    //Items.Add(RESType.Shading, ReadType(reader, header.Shading, RESType.Shading));
            //    //Items.Add(RESType.Material1, ReadType(reader, header.Material1, RESType.Material1));
            //    //Items.Add(RESType.Material2, ReadType(reader, header.Material2, RESType.Material2));
            //    //Items.Add(RESType.MeshName, ReadType(reader, header.MeshName, RESType.MeshName));
            //    //Items.Add(RESType.MaterialData, ReadType(reader, header.MaterialData, RESType.MaterialData));
            //    //Items.Add(RESType.TextureData, ReadType(reader, header.TextureData, RESType.TextureData));
            //    //Items.Add(RESType.AnimationMTN2, ReadType(reader, header.AnimationMTN2, RESType.AnimationMTN2));
            //    //Items.Add(RESType.AnimationIMN2, ReadType(reader, header.AnimationIMN2, RESType.AnimationIMN2));
            //    //Items.Add(RESType.AnimationMTM2, ReadType(reader, header.AnimationMTM2, RESType.AnimationMTM2));
            //    //Items.Add(RESType.MTNINF, ReadType(reader, header.MTNINF, RESType.MTNINF));
            //    //Items.Add(RESType.IMMINF, ReadType(reader, header.IMMINF, RESType.IMMINF));
            //    //Items.Add(RESType.IMMINF, ReadType(reader, header.IMMINF, RESType.IMMINF));
            //}
        }

        public XRES(List<string> stringTable, Dictionary<RESType, List<byte[]>> items)
        {
            //StringTable = stringTable;
            //Items = items;
        }

        public void Save(string magic, string filepath)
        {
            using (FileStream fileStream = new FileStream(fileName, FileMode.Create, FileAccess.Write))
            {
                using (MemoryStream memoryStream = new MemoryStream())
                {
                    Save(memoryStream);

                    memoryStream.Position = 0;
                    memoryStream.CopyTo(fileStream);
                }
            }
        }

        public byte[] Save()
        {
            using (MemoryStream fileStream = new MemoryStream())
            {
                Save(fileStream);
                return fileStream.ToArray();
            }
        }

        private void Save(MemoryStream fileStream)
        {
            BinaryDataWriter writerComp = new BinaryDataWriter(fileStream);

            using (MemoryStream memoryStream = new MemoryStream())
            {
                BinaryDataWriter writerDecomp = new BinaryDataWriter(memoryStream);

                // Fix items size
                for (int i = 0; i < XRESSupport.TypeOrder.Count; i++)
                {
                    RESType resType = XRESSupport.TypeOrder[i];

                    if (Items.ContainsKey(resType))
                    {
                        for (int j = 0; j < Items[resType].Count; j++)
                        {
                            //if (XRESSupport.TypeLength[resType] != Items[resType][j].Length)
                            //{
                            //    byte[] resizedArray = new byte[XRESSupport.TypeLength[resType]];
                            //    Array.Copy(Items[resType][j], resizedArray, Math.Min(XRESSupport.TypeLength[resType], Items[resType][j].Length));
                            //    Items[resType][j] = resizedArray;
                            //}
                        }
                    }
                }

                // int stringOffset = 0x64 + Items.Values.SelectMany(itemData => itemData).Sum(byteArray => byteArray.Length);
                int stringOffset = 0;

                // Header
                writerDecomp.Write(0x53455258);
                writerDecomp.Write((short)stringOffset);
                writerDecomp.Write((short)1);
                writerDecomp.Write(new byte[0x0C]);

                // Create dataOffset dict
                int dataOffset = 0x64;
                int stringPos = 0;
                Dictionary<RESType, int> dataOffsetDict = new Dictionary<RESType, int>();
                Dictionary<string, int> newStringTable = new Dictionary<string, int>();
                for (int i = 0; i < XRESSupport.DataOrder.Count; i++)
                {
                    RESType resType = XRESSupport.DataOrder[i];
                    dataOffsetDict.Add(resType, dataOffset);

                    if (Items.ContainsKey(resType))
                    {
                        //for (int j = 0; j < Items[resType].Count; j++)
                        //{
                        //    byte[] itemContent = Items[resType][j];

                        //    // Reorder string
                        //    using (BinaryDataReader readResItemm = new BinaryDataReader(itemContent))
                        //    {
                        //        int hash = readResItemm.ReadValue<int>();

                        //        if (hash == unchecked((int)0xBA3CEDA6))
                        //        {
                        //            // Remove unused cmn
                        //            Items[resType].RemoveAt(j);
                        //        }
                        //        else
                        //        {
                        //            foreach (string myStr in StringTable)
                        //            {
                        //                if (hash == unchecked((int)Crc32.Compute(Encoding.GetEncoding("Shift-JIS").GetBytes(myStr))))
                        //                {
                        //                    if (!newStringTable.ContainsKey(myStr))
                        //                    {
                        //                        newStringTable.Add(myStr, stringPos);
                        //                        stringPos += Encoding.GetEncoding(932).GetByteCount(myStr) + 1;
                        //                    }

                        //                    if (resType == RESType.TextureData || resType == RESType.Material1 || resType == RESType.Material2 || resType == RESType.MaterialData)
                        //                    {
                        //                        using (BinaryDataWriter writeResItemm = new BinaryDataWriter(itemContent))
                        //                        {
                        //                            writeResItemm.Seek(4);
                        //                            writeResItemm.Write(newStringTable[myStr]);
                        //                        }
                        //                    }
                        //                }
                        //            }
                        //        }
                        //    }
                        //}
                        //dataOffset += Items[resType].Select(itemData => itemData).Sum(byteArray => byteArray.Length);
                    }
                }

                // Header table                 
                for (int i = 0; i < XRESSupport.TypeOrder.Count; i++)
                {
                    RESType resType = XRESSupport.TypeOrder[i];

                    if (Items.ContainsKey(resType))
                    {
                        writerDecomp.Write((short)dataOffsetDict[resType]);
                        writerDecomp.Write((short)Items[resType].Count);
                    }
                    else
                    {
                        writerDecomp.Write((short)dataOffsetDict[resType]);
                        writerDecomp.Write((short)0);
                    }

                    if (i == 4 || i == 5)
                    {
                        writerDecomp.Write(0);
                    }
                }

                // Data
                for (int i = 0; i < XRESSupport.TypeOrder.Count; i++)
                {
                    RESType resType = XRESSupport.TypeOrder[i];

                    if (Items.ContainsKey(resType))
                    {
                        //writerDecomp.Seek(dataOffsetDict[resType]);
                        //writerDecomp.Write(Items[resType].SelectMany(bytes => bytes).ToArray());
                    }
                }

                // String table
                writerDecomp.Seek(stringOffset);
                for (int i = 0; i < newStringTable.Count; i++)
                {
                    writerDecomp.Write(Encoding.GetEncoding(932).GetBytes(newStringTable.ElementAt(i).Key));
                    writerDecomp.Write((byte)0);
                }

                writerDecomp.WriteAlignment(4);

                // Compress
                writerComp.Write(new NoCompression().Compress(memoryStream.ToArray()));
            }
        }

        private List<byte[]> ReadType(BinaryDataReader reader, XRESSupport.HeaderTable headerTable, RESType type)
        {
            List<byte[]> output = new List<byte[]>();

            for (int i = 0; i < headerTable.Count; i++)
            {
                reader.Seek((uint)(headerTable.DataOffset + i * XRESSupport.TypeLength[type]));
                output.Add(reader.GetSection(XRESSupport.TypeLength[type]));
            }

            return output;
        }
    }
}
