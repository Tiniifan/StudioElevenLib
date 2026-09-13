using System;
using System.Text;
using System.Linq;
using System.Collections.Generic;
using System.Runtime.InteropServices;
using StudioElevenLib.Tools;
using StudioElevenLib.Level5.Animation.Logic;

namespace StudioElevenLib.Level5.Animation
{
    public class AnimationSupport
    {
        [StructLayout(LayoutKind.Sequential, Pack = 1)]
        public struct Header
        {
            public long Magic;
            public int DecompSize;
            public int NameOffset;
            public int CompDataOffset;
            public int Track1Count;
            public int Track2Count;
            public int Track3Count;
            public int Track4Count;
        }

        [StructLayout(LayoutKind.Sequential, Pack = 1)]
        public struct Header2
        {
            public long Magic;
            public long EmptyBlock;
            public int DecompSize;
            public int NameOffset;
            public int CompDataOffset;
            public int Track1Count;
            public int Track2Count;
        }

        [StructLayout(LayoutKind.Sequential, Pack = 1)]
        public struct DataHeader
        {
            public int HashOffset;
            public int TrackOffset;
            public int DataOffset;
        }

        [StructLayout(LayoutKind.Sequential, Pack = 1)]
        public struct Track
        {
            public byte Type;
            public byte DataType;
            public byte Unk;
            public byte DataCount;
            public short Start;
            public short End;
        }

        public struct TableHeader
        {
            public int NodeOffset;
            public int KeyFrameOffset;
            public int DifferentKeyFrameOffset;
            public int DataOffset;
            public int EmptyValue;
        }

        [StructLayout(LayoutKind.Sequential, Pack = 1)]
        public struct Node
        {
            public int BoneNameHash;
            public byte NodeType;
            public byte DataType;
            public byte IsInMainTrack;
            public byte Unk2;
            public int FrameStart;
            public int FrameEnd;
            public int DataCount;
            public int DifferentFrameCount;
            public int DataByteSize;
            public int DataVectorSize;
            public int DataVectorLength;
            public int DifferentFrameLength;
            public int FrameLength;
            public int DataLength;
        }

        public static Dictionary<int, string> TrackType = new Dictionary<int, string>
        {
            {0, "None" },
            {1, "BoneLocation" },
            {2, "BoneRotation" },
            {3, "BoneScale" },
            {4, "UVMove" },
            {5, "UVScale" },
            {6, "UVRotation" },
            {7, "MaterialTransparency" },
            {8, "MaterialAttribute" },
            {9, "BoneBool" },
        };

        public static Dictionary<string, int> TrackDataCount = new Dictionary<string, int>
        {
            {"BoneLocation", 3 },
            {"BoneRotation", 4 },
            {"BoneScale", 3 },
            {"UVMove", 2 },
            {"UVScale", 2 },
            {"UVRotation", 1 },
            {"MaterialTransparency", 1 },
            {"MaterialAttribute", 3 },
            {"BoneBool", 1 },
        };

        public static Dictionary<string, int> TrackDataType = new Dictionary<string, int>
        {
            {"BoneLocation", 2 },
            {"BoneRotation", 2 },
            {"BoneScale", 2 },
            {"UVMove", 2 },
            {"UVScale", 2 },
            {"UVRotation", 3 },
            {"MaterialTransparency", 2 },
            {"MaterialAttribute", 2 },
            {"BoneBool", 4 },
        };

        public static Dictionary<string, int> TrackDataSize = new Dictionary<string, int>
        {
            {"BoneLocation", 4 },
            {"BoneRotation", 4 },
            {"BoneScale", 4 },
            {"UVMove", 4 },
            {"UVScale", 4 },
            {"UVRotation", 4 },
            {"MaterialTransparency", 4 },
            {"MaterialAttribute", 4 },
            {"BoneBool", 1 },
        };

        public static Header ReadHeader(BinaryDataReader reader, out string format)
        {
            // Read header
            Header header = reader.ReadStruct<Header>();

            // Get format name
            byte[] formatBytes = BitConverter.GetBytes(header.Magic);
            formatBytes = Array.FindAll(formatBytes, b => b != 0);
            format = Encoding.UTF8.GetString(formatBytes);

            // Wrong Header? Try the second header patern
            if (header.DecompSize == 0)
            {
                reader.Seek(0x0);
                Header2 header2 = reader.ReadStruct<Header2>();
                header.DecompSize = header2.DecompSize;
                header.NameOffset = header2.NameOffset;
                header.CompDataOffset = header2.CompDataOffset;
                header.Track1Count = header2.Track1Count;
                header.Track2Count = header2.Track2Count;

                // Track3 and Track4 doesn't exist in this header
                header.Track3Count = -1;
                header.Track4Count = -1;
            }

            return header;
        }

        public static void ReadAnimationInfo(BinaryDataReader reader, Header header, out string animationName, out int frameCount)
        {
            // Get animation name
            reader.Seek(header.NameOffset);
            int animHash = reader.ReadValue<int>();
            animationName = reader.ReadString(Encoding.UTF8);

            // Get frame count
            reader.Seek(header.CompDataOffset - 4);
            frameCount = reader.ReadValue<int>();
        }

        public static Header CreateHeader(string format, List<Logic.Track> tracks)
        {
            return new Header
            {
                Magic = FormatNameToLong(format),
                DecompSize = 0x00,
                NameOffset = 0x24,
                CompDataOffset = 0x54,
                Track1Count = CountInTrack(tracks, 0),
                Track2Count = CountInTrack(tracks, 1),
                Track3Count = CountInTrack(tracks, 2),
                Track4Count = CountInTrack(tracks, 3),
            };
        }

        public static void WriteAnimationInfo(BinaryDataWriter writer, string animationName, int frameCount)
        {
            // Write animation hash
            writer.Seek(0x24);
            writer.Write(unchecked((int)Crc32.Compute(Encoding.GetEncoding("Shift-JIS").GetBytes(animationName))));
            writer.Write(Encoding.GetEncoding("Shift-JIS").GetBytes(animationName));
            writer.Write(Enumerable.Repeat((byte)0, (int)(0x50 - writer.Position)).ToArray());
            writer.Write(frameCount);
        }

        public static void WriteHeader(BinaryDataWriter writer, Header header, string format, List<Logic.Track> tracks)
        {
            writer.Seek(0);

            if (format == "XMTM")
            {
                Header2 header2 = new Header2
                {
                    Magic = FormatNameToLong(format),
                    EmptyBlock = 0x0,
                    DecompSize = header.DecompSize,
                    NameOffset = 0x24,
                    CompDataOffset = 0x54,
                    Track1Count = CountInTrack(tracks, 0),
                    Track2Count = CountInTrack(tracks, 1),
                };

                writer.WriteStruct(header2);
            }
            else
            {
                writer.WriteStruct(header);
            }
        }

        public static object[] ReadAnimData(BinaryDataReader reader, int dataType, int dataCount)
        {
            object[] animData = Enumerable.Range(0, dataCount).Select(c => new object()).ToArray();

            for (int i = 0; i < dataCount; i++)
            {
                if (dataType == 1)
                {
                    animData[i] = reader.ReadValue<short>() / (float)0x7FFF;
                }
                else if (dataType == 2)
                {
                    animData[i] = reader.ReadValue<float>();
                }
                else if (dataType == 3)
                {
                    animData[i] = reader.ReadValue<float>();
                }
                else if (dataType == 4)
                {
                    animData[i] = reader.ReadValue<byte>();
                }
                else
                {
                    throw new NotImplementedException($"Data Type {dataType} not implemented");
                }
            }

            return animData;
        }

        public static object ConvertAnimDataToObject(object[] animData, int type)
        {
            if (type == 1)
            {
                return new BoneLocation((float)animData[0], (float)animData[1], (float)animData[2]);
            }
            else if (type == 2)
            {
                return new BoneRotation((float)animData[0], (float)animData[1], (float)animData[2], (float)animData[3]);
            }
            else if (type == 3)
            {
                return new BoneScale((float)animData[0], (float)animData[1], (float)animData[2]);
            }
            else if (type == 4)
            {
               return new UVMove((float)animData[0], (float)animData[1]);
            }
            else if (type == 5)
            {
                return new UVScale((float)animData[0], (float)animData[1]);
            }
            else if (type == 6)
            {
                return new UVRotation((float)animData[0]);
            }
            else if (type == 7)
            {
                return new MaterialTransparency((float)animData[0]);
            }
            else if (type == 8)
            {
                return new MaterialAttribute((float)animData[0], (float)animData[1], (float)animData[2]);
            }
            else if (type == 9)
            {
                return new BoneBool(Convert.ToInt32(animData[0]));
            } else
            {
                throw new NotImplementedException($"Data Type {type} not implemented");
            }
        }

        public static byte[] ValueToByteArray(string type, object value)
        {
            if (type == "BoneLocation")
            {
                BoneLocation location = (BoneLocation)value;
                return location.ToByte();
            }
            else if (type == "BoneRotation")
            {
                BoneRotation rotation = (BoneRotation)value;
                return rotation.ToByte();
            }
            else if (type == "BoneScale")
            {
                BoneScale scale = (BoneScale)value;
                return scale.ToByte();
            }
            else if (type == "UVMove")
            {
                UVMove uvMove = (UVMove)value;
                return uvMove.ToByte();
            }
            else if (type == "UVRotation")
            {
                UVRotation uvRotation = (UVRotation)value;
                return uvRotation.ToByte();
            }
            else if (type == "UVScale")
            {
                UVScale uvScale = (UVScale)value;
                return uvScale.ToByte();
            }
            else if (type == "MaterialTransparency")
            {
                MaterialTransparency textureBrightness = (MaterialTransparency)value;
                return textureBrightness.ToByte();
            }
            else if (type == "MaterialAttribute")
            {
                MaterialAttribute textureUnk = (MaterialAttribute)value;
                return textureUnk.ToByte();
            }
            else if (type == "BoneBool")
            {
                BoneBool enableUnk = (BoneBool)value;
                return enableUnk.ToByte();
            }
            else
            {
                return new byte[] { };
            }
        }

        public static long FormatNameToLong(string str)
        {
            byte[] bytes = Encoding.UTF8.GetBytes(str);

            long result = 0;
            for (int i = 0; i < bytes.Length && i < sizeof(long); i++)
            {
                result |= (long)bytes[i] << (i * 8);
            }

            return result;
        }

        public static int CountInTrack(List<Logic.Track> tracks, int searchIndex)
        {
            return tracks.Any(x => x.Index == searchIndex)
                ? tracks.FirstOrDefault(x => x.Index == searchIndex).Nodes.Count()
                    : (tracks.Count > searchIndex && tracks[searchIndex].Index == -1)
                        ? tracks[searchIndex].Nodes.Count()
                            : 0;
        }

        public static int GetDistincHashes(List<Logic.Track> tracks)
        {
            List<string> hashes = new List<string>();

            for (int i = 0; i < tracks.Count; i++)
            {
                hashes.AddRange(tracks.ElementAt(i).Nodes.Select(x => x.Name));
            }

            return hashes.Distinct().Count();
        }

        public static int CountHashes(List<Logic.Track> tracks)
        {
            int hashes = 0;

            for (int i = 0; i < tracks.Count; i++)
            {
                hashes += tracks.ElementAt(i).Nodes.Count();
            }

            return hashes;
        }

        public static List<int> GetNameHashes(List<Logic.Track> tracks)
        {
            List<int> nameHashes = new List<int>();

            for (int i = 0; i < tracks.Count; i++)
            {
                foreach (string nameHash in tracks.ElementAt(i).Nodes.Select(x => x.Name))
                {
                    int nameInt = Convert.ToInt32(nameHash, 16);

                    if (!nameHashes.Contains(nameInt))
                    {
                        nameHashes.Add(nameInt);
                    }
                }
            }

            return nameHashes.ToList();
        }
    }
}
