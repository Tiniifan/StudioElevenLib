using System;
using System.IO;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using StudioElevenLib.Tools;
using StudioElevenLib.Level5.Compression;
using StudioElevenLib.Level5.Compression.NoCompression;

namespace StudioElevenLib.Level5.Material
{
    public class MTRC
    {
        public byte[] MTRCData { get; set; }

        public byte[] LUTCData { get; set; }

        public MTRC(byte[] data)
        {
            MTRCData = new byte[] { };
            LUTCData = new byte[] { };

            using (BinaryDataReader reader = new BinaryDataReader(data))
            {
                MTRCSupport.MTRCHeader mtrcHeader = reader.ReadStruct<MTRCSupport.MTRCHeader>();

                reader.Seek(mtrcHeader.DataStartOffset);

                if (mtrcHeader.DataEndOffset == 0x0)
                {
                    MTRCData = Compressor.Decompress(reader.GetSection((int)(0x3C)));
                } else
                {
                    MTRCData = Compressor.Decompress(reader.GetSection((int)(mtrcHeader.DataEndOffset - mtrcHeader.DataStartOffset)));

                    MTRCSupport.LUTCHeader lutcHeader = reader.ReadStruct<MTRCSupport.LUTCHeader>();
                    LUTCData = Compressor.Decompress(reader.GetSection((int)(reader.Length - reader.Position)));
                }
            }
        }

        public byte[] Save()
        {
            using (MemoryStream fileStream = new MemoryStream())
            {
                BinaryDataWriter writer = new BinaryDataWriter(fileStream);

                byte[] mtrcDataCompressed = new NoCompression().Compress(MTRCData.ToArray());
                byte[] lutcDataCompressed = new NoCompression().Compress(LUTCData.ToArray());
            
                if (LUTCData.Length > 0)
                {
                    // MTRC
                    writer.Write(0x000030304352544D);
                    writer.Write((short)0x18);
                    writer.Write((short)(0x18 + mtrcDataCompressed.Length));
                    writer.Write(0);
                    writer.Write((short)(0x18 + mtrcDataCompressed.Length));
                    writer.Write((short)(0x18 + mtrcDataCompressed.Length));
                    writer.Write(0);
                    writer.Write(mtrcDataCompressed);

                    // LUTC
                    writer.Write(0x000030304354554C);
                    writer.Write((short)0);
                    writer.Write((short)0x10);
                    writer.Write(0);
                    writer.Write(lutcDataCompressed);
                } else
                {
                    writer.Write(0x000030304352544D);
                    writer.Write((short)0x18);
                    writer.Write((short)(0x00));
                    writer.Write(0);
                    writer.Write((short)(0x00));
                    writer.Write((short)(0x00));
                    writer.Write(0);
                    writer.Write(mtrcDataCompressed);
                }


                return fileStream.ToArray();
            }
        }
    }
}
