using System;
using System.IO;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using StudioElevenLib.Tools;
using StudioElevenLib.Level5.Animation.Logic;
using StudioElevenLib.Level5.Compression;
using StudioElevenLib.Level5.Compression.LZ10;
using StudioElevenLib.Level5.Compression.NoCompression;

namespace StudioElevenLib.Level5.Camera.CMR1
{
    public class CMR1
    {
        private static int[] FillArray(int[] inputArray, int size)
        {
            int[] result = new int[size];
            int lastIndex = 0;

            for (int i = 0; i < inputArray.Length; i++)
            {
                int nextValue = 0;
                int lastValue = inputArray[i];

                if (i != inputArray.Length - 1)
                {
                    nextValue = inputArray[i + 1];
                }
                else
                {
                    nextValue = size;
                }

                for (int j = lastValue; j < nextValue; j++)
                {
                    result[j] = lastIndex;
                }

                lastIndex++;
            }

            return result;
        }

        public byte[] Save(uint hashName, Dictionary<int, Dictionary<int, float[]>> CamValues, int frameCount, float camSpeed)
        {
            using (MemoryStream fileStream = new MemoryStream())
            {
                BinaryDataWriter writer = new BinaryDataWriter(fileStream);

                CMR1Support.Header header = new CMR1Support.Header
                {
                    Magic = 0x414D4358,
                    DataOffset = 0x20,
                    DataSkipOffset = 0x14,
                    Unk1 = Convert.ToInt32(CamValues[0].Values.Count() > 0),
                    Unk2 = Convert.ToInt32(CamValues[1].Values.Count() > 0),
                    Unk3 = Convert.ToInt32(CamValues[2].Values.Count() > 0),
                    Unk4 = Convert.ToInt32(CamValues[3].Values.Count() > 0),
                    Unk5 = 0x00,
                    AnimationHash = hashName,
                    EmptyBlock1 = 0x00,
                    FrameCount = frameCount,
                    Unk6 = 0x02,
                    CamSpeed = camSpeed,
                };

                writer.WriteStruct(header);

                for (int i = 0; i < 4; i++)
                {
                    CMR1Support.CameraHeader cameraHeader = new CMR1Support.CameraHeader
                    {
                        CameraOffset = 0x14,
                        GhostFrameOffset = 0x30,
                        FrameOffset = 0x00,
                        DataOffset = 0x00,
                        BlockLength = 0x00,
                    };

                    // Write camera motion data
                    using (MemoryStream memoryStream = new MemoryStream())
                    {
                        BinaryDataWriter writerCameraMotion = new BinaryDataWriter(memoryStream);

                        if (CamValues[i].Values.Count() > 0)
                        {

                            CMR1Support.CameraDataHeader cameraDataHeader = new CMR1Support.CameraDataHeader
                            {
                                Unk1 = 0xC55BEBD1,
                                Unk2 = 0x01,
                                Unk3 = 0x02,
                                Unk4 = 0x01,
                                Unk5 = 0x00,
                                FrameCount = frameCount,
                                DataCount = CamValues[i].Values.Count(),
                                GhostFrameCount = frameCount + 1,
                                DataSize = 0x04,
                                DataByteLength = CamValues[i].ElementAt(0).Value.Length,
                                DataBlockSize = CamValues[i].ElementAt(0).Value.Length * 4,
                                GhostFrameLength = (frameCount + 1) * 2,
                                FrameLength = (frameCount) * 2,
                                DataLength = CamValues[i].Values.Count() * CamValues[i].ElementAt(0).Value.Length,
                            };

                            writerCameraMotion.WriteStruct(cameraDataHeader);
                            writerCameraMotion.Write(FillArray(CamValues[i].Keys.Select(x => x).ToArray(), frameCount + 1).SelectMany(x => BitConverter.GetBytes((short)x)).ToArray());
                            writerCameraMotion.WriteAlignment();
                            cameraHeader.FrameOffset = (int)writerCameraMotion.Position;
                            writerCameraMotion.Write(CamValues[i].Select(x => x.Key).SelectMany(x => BitConverter.GetBytes((short)x)).ToArray());
                            writerCameraMotion.WriteAlignment();
                            cameraHeader.DataOffset = (int)writerCameraMotion.Position;

                            foreach (float[] camDataValues in CamValues[i].Select(x => x.Value))
                            {
                                foreach (float camDataValue in camDataValues)
                                {
                                    writerCameraMotion.Write(camDataValue);
                                }
                            }

                            byte[] compressedCameraMotion = new LZ10().Compress(memoryStream.ToArray());
                            cameraHeader.BlockLength = 0x14 + compressedCameraMotion.Length;

                            writer.WriteStruct(cameraHeader);
                            writer.Write(compressedCameraMotion);
                        }
                    }
                }

                return fileStream.ToArray();
            }
        }
    }
}
