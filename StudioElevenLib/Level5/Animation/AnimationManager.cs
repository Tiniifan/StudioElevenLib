using System;
using System.Text;
using System.IO;
using System.Collections.Generic;
using System.Linq;
using System.Threading.Tasks;
using StudioElevenLib.Tools;
using StudioElevenLib.Level5.Compression;
using StudioElevenLib.Level5.Animation.Logic;
using StudioElevenLib.Level5.Compression.LZ10;

namespace StudioElevenLib.Level5.Animation
{
    public class AnimationManager
    {
        public string Format;

        public string Version;

        public string AnimationName;

        public int FrameCount;

        public List<Track> Tracks = new List<Track>();

        public AnimationManager()
        {

        }

        public AnimationManager(Stream stream)
        {
            using (BinaryDataReader reader = new BinaryDataReader(stream))
            {
                // Read header
                AnimationSupport.Header header = reader.ReadStruct<AnimationSupport.Header>();

                // Get format name
                byte[] formatBytes = BitConverter.GetBytes(header.Magic);
                formatBytes = Array.FindAll(formatBytes, b => b != 0);
                Format = Encoding.UTF8.GetString(formatBytes);

                // Wrong Header? Try the second header patern
                if (header.DecompSize == 0)
                {
                    reader.Seek(0x0);
                    AnimationSupport.Header2 header2 = reader.ReadStruct<AnimationSupport.Header2>();
                    header.DecompSize = header2.DecompSize;
                    header.NameOffset = header2.NameOffset;
                    header.CompDataOffset = header2.CompDataOffset;
                    header.Track1Count = header2.Track1Count;
                    header.Track2Count = header2.Track2Count;

                    // Track3 and Track4 doesn't exist in this header
                    header.Track3Count = -1;
                    header.Track4Count = -1;
                }

                // Get animation name
                reader.Seek(header.NameOffset);
                int animHash = reader.ReadValue<int>();
                AnimationName = reader.ReadString(Encoding.UTF8);

                // Get frame count
                reader.Seek(header.CompDataOffset - 4);
                FrameCount = reader.ReadValue<int>();

                // Get decomp block
                using (BinaryDataReader decompReader = new BinaryDataReader(Compressor.Decompress(reader.GetSection((int)(reader.Length - reader.Position)))))
                {
                    int hashOffset = decompReader.ReadValue<int>();
                    int trackoffset = decompReader.ReadValue<int>();
                    decompReader.Seek(0x0);

                    if (hashOffset == 0x0C)
                    {
                        Version = "V2";
                        GetAnimationDataV2(header, decompReader);
                    }
                    else
                    {
                        Version = "V1";
                        GetAnimationDataV1(header, decompReader);
                    }
                }
            }
        }

        public byte[] Save()
        {
            using (MemoryStream stream = new MemoryStream())
            {
                BinaryDataWriter writer = new BinaryDataWriter(stream);

                AnimationSupport.Header header = new AnimationSupport.Header
                {
                    Magic = FormatNameToLong(Format),
                    DecompSize = 0x00,
                    NameOffset = 0x24,
                    CompDataOffset = 0x54,
                    Track1Count = 0,
                    Track2Count = 0,
                    Track3Count = 0,
                    Track4Count = 0,
                };

                if (Tracks.Count(x => x.Name == "UVMove") == 2)
                {
                    header.Track1Count = Tracks[0].Value.Count();
                    header.Track4Count = Tracks[1].Value.Count();
                }
                else if (Tracks.Any(x => x.Name == "UVMove") && Tracks.Any(x => x.Name == "UVScale"))
                {
                    header.Track1Count = Tracks[0].Value.Count();
                    header.Track3Count = Tracks[1].Value.Count();
                }
                else
                {
                    header.Track1Count = Tracks.Count() >= 1 ? Tracks.ElementAt(0).Value.Count() : 0;
                    header.Track2Count = Tracks.Count() >= 2 ? Tracks.ElementAt(1).Value.Count() : 0;
                    header.Track3Count = Tracks.Count() >= 3 ? Tracks.ElementAt(2).Value.Count() : 0;
                    header.Track4Count = Tracks.Count() >= 4 ? Tracks.ElementAt(3).Value.Count() : 0;
                }

                // Don't exceed 40 characters
                if (AnimationName.Length > 40)
                {
                    AnimationName = AnimationName.Substring(0, 40);
                }

                // Write animation hash
                writer.Seek(0x24);
                writer.Write(unchecked((int)Crc32.Compute(Encoding.GetEncoding("Shift-JIS").GetBytes(AnimationName))));
                writer.Write(Encoding.GetEncoding("Shift-JIS").GetBytes(AnimationName));
                writer.Write(Enumerable.Repeat((byte)0, (int)(0x50 - writer.Position)).ToArray());
                writer.Write(FrameCount);

                // Write animation data
                if (Version == "V1")
                {
                    SaveAnimationDataV1(ref header, writer);
                }
                else
                {
                    SaveAnimationDataV2(ref header, writer);
                }

                writer.Seek(0);

                if (Format == "XMTM")
                {
                    AnimationSupport.Header2 header2 = new AnimationSupport.Header2
                    {
                        Magic = FormatNameToLong(Format),
                        EmptyBlock = 0x0,
                        DecompSize = header.DecompSize,
                        NameOffset = 0x24,
                        CompDataOffset = 0x54,
                        Track1Count = Tracks.Count() >= 1 ? Tracks.ElementAt(0).Value.Count() : 0,
                        Track2Count = Tracks.Count() >= 2 ? Tracks.ElementAt(1).Value.Count() : 0,
                    };

                    writer.WriteStruct(header2);
                }
                else
                {
                    writer.WriteStruct(header);
                }

                return stream.ToArray();
            }
        }

        private void GetAnimationDataV1(AnimationSupport.Header header, BinaryDataReader decompReader)
        {
            int trackIndex = 0;
            long tableOffset = 0;

            if (header.Track1Count > 0)
            {
                for (int i = 0; i < header.Track1Count; i++)
                {
                    ReadFrameDataV1(decompReader, tableOffset, trackIndex);
                }

                trackIndex++;
            }

            if (header.Track2Count > 0)
            {
                for (int i = 0; i < header.Track2Count; i++)
                {
                    ReadFrameDataV1(decompReader, tableOffset, trackIndex);
                }

                trackIndex++;
            }

            if (header.Track3Count > 0)
            {
                for (int i = 0; i < header.Track3Count; i++)
                {
                    ReadFrameDataV1(decompReader, tableOffset, trackIndex);
                }

                trackIndex++;
            }

            if (header.Track4Count > 0)
            {
                for (int i = 0; i < header.Track4Count; i++)
                {
                    ReadFrameDataV1(decompReader, tableOffset, trackIndex);
                }

                trackIndex++;
            }
        }

        private void GetAnimationDataV2(AnimationSupport.Header header, BinaryDataReader decompReader)
        {
            AnimationSupport.DataHeader dataHeader = decompReader.ReadStruct<AnimationSupport.DataHeader>();

            // Get name Hashes
            decompReader.Seek(dataHeader.HashOffset);
            int elementCount = (dataHeader.TrackOffset - dataHeader.HashOffset) / 4;
            int[] nameHashes = decompReader.ReadMultipleValue<int>(elementCount);
            int[] followNameHashes = nameHashes.Skip(header.Track1Count).Take(nameHashes.Length- header.Track1Count).ToArray();

            // Track Information
            int trackCount = Convert.ToInt32(header.Track1Count != -1) + Convert.ToInt32(header.Track2Count != -1) + Convert.ToInt32(header.Track3Count != -1) + Convert.ToInt32(header.Track4Count != -1);
            List<AnimationSupport.Track> tracks = new List<AnimationSupport.Track>();
            for (int i = 0; i < trackCount; i++)
            {
                decompReader.Seek(dataHeader.TrackOffset + 2 * i);
                decompReader.Seek(decompReader.ReadValue<short>());
                tracks.Add(decompReader.ReadStruct<AnimationSupport.Track>());

                if (tracks[i].Type != 0)
                {
                    Tracks.Add(new Track(AnimationSupport.TrackType[tracks[i].Type]));
                }
            }

            int offset = 0;
            int index = 0;
            int trackIndex = 0;

            if (header.Track1Count > 0)
            {
                ReadFrameDataV2(decompReader, offset, header.Track1Count, dataHeader.DataOffset, nameHashes, followNameHashes, tracks[0], trackIndex);
                trackIndex++;
            }
            offset += header.Track1Count;
            index++;

            if (header.Track2Count > 0)
            {
                ReadFrameDataV2(decompReader, offset, header.Track2Count, dataHeader.DataOffset, nameHashes, followNameHashes, tracks[1], trackIndex);
                trackIndex++;
            }
            offset += header.Track2Count;
            index++;

            if (header.Track3Count > 0)
            {
                ReadFrameDataV2(decompReader, offset, header.Track3Count, dataHeader.DataOffset, nameHashes, followNameHashes, tracks[2], trackIndex);
                trackIndex++;
            }
            offset += header.Track3Count;
            index++;

            if (header.Track4Count > 0)
            {
                ReadFrameDataV2(decompReader, offset, header.Track4Count, dataHeader.DataOffset, nameHashes, followNameHashes, tracks[3], trackIndex);
            }
        }

        private void SaveAnimationDataV1(ref AnimationSupport.Header header, BinaryDataWriter writer)
        {
            using (MemoryStream memoryStream = new MemoryStream())
            {
                BinaryDataWriter writerDecomp = new BinaryDataWriter(memoryStream);

                int hashCount = CountHashes();
                int hashCountDistinct = GetDistincHashes();

                long headerPos = 0;
                long nodeOffset = hashCount * 20;

                for (int i = 0; i < 4; i++)
                {
                    if (i < Tracks.Count)
                    {
                        var node = Tracks.ElementAt(i);
                        FixNode(node.Value, FrameCount);

                        if (node.Value.Count() > 0)
                        {
                            foreach (var keyValuePair in node.Value)
                            {
                                int nameInt = Convert.ToInt32(keyValuePair.Key.Name, 16);
                                int dataVectorSize = AnimationSupport.TrackDataCount[node.Name];

                                AnimationSupport.Node nodeHeader = new AnimationSupport.Node
                                {
                                    BoneNameHash = nameInt,
                                    NodeType = (byte)AnimationSupport.TrackType.FirstOrDefault(x => x.Value == node.Name).Key,
                                    DataType = 2,
                                    IsInMainTrack = (byte)Convert.ToInt32(keyValuePair.Key.IsInMainTrack),
                                    Unk2 = 0,
                                    FrameStart = 0,
                                    FrameEnd = FrameCount,
                                    DataCount = keyValuePair.Value.Count,
                                    DifferentFrameCount = FrameCount + 1,
                                    DataByteSize = 4,
                                    DataVectorSize = dataVectorSize,
                                    DataVectorLength = dataVectorSize * 4,
                                    DifferentFrameLength = (FrameCount + 1) * 2,
                                    FrameLength = keyValuePair.Value.Count * 2,
                                    DataLength = keyValuePair.Value.Count * dataVectorSize * 4
                                };

                                // Write node table
                                writerDecomp.Seek(nodeOffset);
                                writerDecomp.WriteStruct(nodeHeader);

                                // write key frame table
                                long keyFrameOffset = writerDecomp.Position;
                                writerDecomp.Write(FillArray(keyValuePair.Value.Select(x => x.Key).ToArray(), FrameCount + 1).SelectMany(x => BitConverter.GetBytes((short)x)).ToArray());
                                writerDecomp.WriteAlignment2(4, 0);

                                // Write different key frame table
                                long differentKeyFrameOffset = writerDecomp.Position;
                                writerDecomp.Write(keyValuePair.Value.SelectMany(x => BitConverter.GetBytes((short)x.Key)).ToArray());
                                writerDecomp.WriteAlignment2(4, 0);

                                // writer animation data
                                long dataOffset = writerDecomp.Position;
                                writerDecomp.Write(keyValuePair.Value.SelectMany(x => ValueToByteArray(node.Name, x.Value)).ToArray());

                                AnimationSupport.TableHeader tableHeader = new AnimationSupport.TableHeader
                                {
                                    NodeOffset = (int)nodeOffset,
                                    KeyFrameOffset = (int)keyFrameOffset,
                                    DifferentKeyFrameOffset = (int)differentKeyFrameOffset,
                                    DataOffset = (int)dataOffset,
                                    EmptyValue = 0,
                                };

                                // Update offset
                                nodeOffset = writerDecomp.Position;

                                // Write header table
                                writerDecomp.Seek(headerPos);
                                writerDecomp.WriteStruct(tableHeader);
                                headerPos = writerDecomp.Position;
                            }
                        }
                    }
                }

                header.DecompSize = (int)memoryStream.Length * 2;
                writer.Write(new LZ10().Compress(memoryStream.ToArray()));
            }
        }

        private void SaveAnimationDataV2(ref AnimationSupport.Header header, BinaryDataWriter writer)
        {
            using (MemoryStream memoryStream = new MemoryStream())
            {
                BinaryDataWriter writerDecomp = new BinaryDataWriter(memoryStream);

                int hashCount = CountHashes();
                int hashCountDistinct = GetDistincHashes();
                List<int> nameHashes = GetNameHashes();

                // Write data header
                writerDecomp.Write(0x0C);
                writerDecomp.Write(0x0C + hashCountDistinct * 4);
                writerDecomp.Write((0x0C + hashCountDistinct * 4) + 4 * 10);

                // Write name hash
                if (hashCountDistinct > 0)
                {
                    writerDecomp.WriteMultipleStruct<int>(nameHashes);
                }

                // Store position
                int trackOffset = (int)writerDecomp.Position + 4 * 2;
                int trackDataOffset = (int)writerDecomp.Position + 4 * 2;
                int tableOffset = (0x0C + hashCountDistinct * 4) + 4 * 10;
                int dataOffset = (int)(0x0C + hashCount * 4) + 4 * 10 + hashCount * 16;

                // Loop in tracks
                for (int i = 0; i < 4; i++)
                {
                    // Write track offsets
                    writerDecomp.Seek(0x0C + hashCountDistinct * 4 + i * 2);
                    writerDecomp.Write((short)trackOffset);
                    trackOffset += 8;

                    // Set track struct
                    AnimationSupport.Track track = new AnimationSupport.Track
                    {
                        Type = 0,
                        DataType = 0,
                        Unk = 0,
                        DataCount = 0,
                        Start = 0,
                        End = 0
                    };

                    if (i < Tracks.Count)
                    {
                        var node = Tracks.ElementAt(i);

                        if (node.Value.Count() > 0)
                        {
                            track.Type = (byte)AnimationSupport.TrackType.FirstOrDefault(x => x.Value == node.Name).Key;
                            track.DataType = 2;
                            track.Unk = 0;
                            track.DataCount = (byte)AnimationSupport.TrackDataCount[node.Name];
                            track.Start = 0;
                            track.End = (short)FrameCount;

                            foreach (var keyValuePair in node.Value)
                            {
                                // Write table header
                                writerDecomp.Seek(tableOffset);
                                writerDecomp.Write(dataOffset);
                                writerDecomp.Write(dataOffset + 4);
                                tableOffset += 8;

                                // Write data
                                writerDecomp.Seek(dataOffset);
                                writerDecomp.Write((short)nameHashes.IndexOf(Convert.ToInt32(keyValuePair.Key.Name, 16)));

                                // Frame count
                                if (keyValuePair.Value.Count() < 255)
                                {
                                    writerDecomp.Write((byte)keyValuePair.Value.Count());
                                    writerDecomp.Write((byte)0x00);
                                }
                                else
                                {
                                    int lowFrameCount = (short)keyValuePair.Value.Count() & 0xFF;
                                    int hightFrameCount = 32 + ((short)keyValuePair.Value.Count() >> 8) & 0xFF;
                                    writerDecomp.Write((byte)lowFrameCount);
                                    writerDecomp.Write((byte)hightFrameCount);
                                }

                                // Write frames
                                writerDecomp.WriteMultipleStruct<short>(keyValuePair.Value.Keys.Select(x => Convert.ToInt16(x)).ToArray());
                                writerDecomp.WriteAlignment(4, 0);

                                // Keep value offset
                                int valueOffset = (int)writerDecomp.Position;

                                // Write value
                                foreach (object value in keyValuePair.Value.Values)
                                {
                                    writerDecomp.Write(ValueToByteArray(node.Name, value));
                                }

                                // Update dataOffset
                                dataOffset = (int)writerDecomp.Position;

                                // Finish to write table header
                                writerDecomp.Seek(tableOffset);
                                writerDecomp.Write(valueOffset);
                                writerDecomp.Write(0);
                                tableOffset += 8;
                            }
                        }
                    }

                    // Write track data
                    writerDecomp.Seek(trackDataOffset);
                    writerDecomp.WriteStruct(track);
                    trackDataOffset += 8;
                }

                header.DecompSize = (int)memoryStream.Length * 2;
                writer.Write(new LZ10().Compress(memoryStream.ToArray()));
            }
        }

        public void ReadFrameDataV2(BinaryDataReader data, int offset, int count, int dataOffset, int[] nameHashes, int[] followNameHashes, AnimationSupport.Track track, int trackIndex)
        {
            for (int i = offset; i < offset + count; i++)
            {
                bool isMainTrack = true;
                data.Seek(dataOffset + 4 * 4 * i);

                int flagOffset = data.ReadValue<int>();
                int keyFrameOffset = data.ReadValue<int>();
                int keyDataOffset = data.ReadValue<int>();

                data.Seek(flagOffset);
                int index = data.ReadValue<short>();
                string nameHash = nameHashes[index].ToString("X8");

                int lowFrameCount = data.ReadValue<byte>();
                int highFrameCount = data.ReadValue<byte>();
                int keyFrameCount = 0;

                if (highFrameCount == 0)
                {
                    isMainTrack = false;
                    nameHash = followNameHashes[index].ToString("X8");
                    keyFrameCount = lowFrameCount;
                }
                else
                {
                    highFrameCount -= 32;
                    keyFrameCount = (highFrameCount << 8) | lowFrameCount;
                }

                data.Seek(keyDataOffset);
                for (int k = 0; k < keyFrameCount; k++)
                {
                    long temp = data.Position;
                    data.Seek(keyFrameOffset + k * 2);
                    int frame = data.ReadValue<short>();
                    data.Seek((int)temp);

                    object[] animData = Enumerable.Range(0, track.DataCount).Select(c => new object()).ToArray();
                    for (int j = 0; j < track.DataCount; j++)
                    {
                        if (track.DataType == 1)
                        {
                            animData[j] = data.ReadValue<short>() / (float)0x7FFF;
                        }
                        else if (track.DataType == 2)
                        {
                            animData[j] = data.ReadValue<float>();
                        }
                        else if (track.DataType == 3)
                        {
                            animData[j] = data.ReadValue<short>();
                        }
                        else if (track.DataType == 4)
                        {
                            animData[j] = data.ReadValue<byte>();
                        }
                        else
                        {
                            throw new NotImplementedException($"Data Type {track.DataType} not implemented");
                        }
                    }

                    AddNode(nameHash, animData, track.Type, frame, trackIndex, isMainTrack);
                }
            }
        }

        public void ReadFrameDataV1(BinaryDataReader decompReader, long tableOffset, int trackIndex)
        {
            // Read offset table
            decompReader.Seek(tableOffset);
            AnimationSupport.TableHeader tableHeader = decompReader.ReadStruct<AnimationSupport.TableHeader>();
            tableOffset = decompReader.Position;

            // Read node
            decompReader.Seek(tableHeader.NodeOffset);
            AnimationSupport.Node node = decompReader.ReadStruct<AnimationSupport.Node>();

            // Create node key based on track type
            if (node.NodeType != 0)
            {
                Tracks.Add(new Track(AnimationSupport.TrackType[node.NodeType]));
            }

            // Get data index for frame
            decompReader.Seek(tableHeader.KeyFrameOffset);
            int[] dataIndexes = decompReader.ReadMultipleValue<short>(node.DifferentFrameLength / 2).Select(x => Convert.ToInt32(x)).Distinct().ToArray();

            // Get different frame index
            decompReader.Seek(tableHeader.DifferentKeyFrameOffset);
            int[] differentFrames = decompReader.ReadMultipleValue<short>(node.FrameLength / 2).Select(x => Convert.ToInt32(x)).ToArray();

            for (int j = 0; j < differentFrames.Length; j++)
            {
                // Get frame
                int frame = differentFrames[j];
                int dataIndex = dataIndexes[j];

                // Seek data offset
                decompReader.Seek(tableHeader.DataOffset + j * node.DataVectorSize * node.DataByteSize);
                object[] animData = Enumerable.Range(0, node.DataVectorSize).Select(c => new object()).ToArray();

                // Decode animation data
                for (int k = 0; k < node.DataVectorSize; k++)
                {
                    if (node.DataType == 1)
                    {
                        animData[k] = decompReader.ReadValue<short>() / (float)0x7FFF;
                    }
                    else if (node.DataType == 2)
                    {
                        animData[k] = decompReader.ReadValue<float>();
                    }
                    else if (node.DataType == 3)
                    {
                        animData[k] = decompReader.ReadValue<short>();
                    }
                    else
                    {
                        throw new NotImplementedException($"Data Type {node.DataType} not implemented");
                    }
                }

                // Create node
                AddNode(node.BoneNameHash.ToString("X8"), animData, node.NodeType, frame, trackIndex, node.IsInMainTrack == 1);
            }
        }

        public void AddNode(string nameHash, object[] animData, int type, int frame, int index, bool isMainTrack)
        {
            if (!Tracks[index].NodeExists(nameHash))
            {
                Tracks[index].Value.Add(new Node(nameHash, isMainTrack), new Dictionary<int, object>());
            }

            KeyValuePair<Node, Dictionary<int, object>>? node;
            node = Tracks[index].GetNodeByName(nameHash);

            if (node != null && !node.Value.Value.ContainsKey(frame))
            {
                if (type == 1)
                {
                    node.Value.Value.Add(frame, new BoneLocation((float)animData[0], (float)animData[1], (float)animData[2]));
                }
                else if (type == 2)
                {
                    node.Value.Value.Add(frame, new BoneRotation((float)animData[0], (float)animData[1], (float)animData[2], (float)animData[3]));
                }
                else if (type == 3)
                {
                    node.Value.Value.Add(frame, new BoneScale((float)animData[0], (float)animData[1], (float)animData[2]));
                }
                else if (type == 4)
                {
                    node.Value.Value.Add(frame, new UVMove((float)animData[0], (float)animData[1]));
                }
                else if (type == 5)
                {
                    node.Value.Value.Add(frame, new UVScale((float)animData[0], (float)animData[1]));
                }
                else if (type == 7)
                {
                    node.Value.Value.Add(frame, new TextureBrightness((float)animData[0]));
                }
                else if (type == 8)
                {
                    node.Value.Value.Add(frame, new TextureUnk((float)animData[0], (float)animData[1], (float)animData[2]));
                }
                else if (type == 9)
                {
                    node.Value.Value.Add(frame, new Unk(Convert.ToInt32(animData[0])));
                }
            }
        }

        public byte[] ValueToByteArray(string type, object value)
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
            else if (type == "UVScale")
            {
                UVScale uvScale = (UVScale)value;
                return uvScale.ToByte();
            }
            else if (type == "TextureBrightness")
            {
                TextureBrightness textureBrightness = (TextureBrightness)value;
                return textureBrightness.ToByte();
            }
            else if (type == "TextureUnk")
            {
                TextureUnk textureUnk = (TextureUnk)value;
                return textureUnk.ToByte();
            }
            else if (type == "Unk")
            {
                Unk enableUnk = (Unk)value;
                return enableUnk.ToByte();
            }
            else
            {
                return new byte[] { };
            }
        }

        private long FormatNameToLong(string str)
        {
            byte[] bytes = Encoding.UTF8.GetBytes(str);

            long result = 0;
            for (int i = 0; i < bytes.Length && i < sizeof(long); i++)
            {
                result |= (long)bytes[i] << (i * 8);
            }

            return result;
        }

        public int GetDistincHashes()
        {
            List<string> hashes = new List<string>();

            for (int i = 0; i < Tracks.Count; i++)
            {
                hashes.AddRange(Tracks.ElementAt(i).Value.Select(x => x.Key.Name));
            }

            return hashes.Distinct().Count();
        }

        public int CountHashes()
        {
            int hashes = 0;

            for (int i = 0; i < Tracks.Count; i++)
            {
                hashes += Tracks.ElementAt(i).Value.Count();
            }

            return hashes;
        }

        public List<int> GetNameHashes()
        {
            List<int> nameHashes = new List<int>();

            for (int i = 0; i < Tracks.Count; i++)
            {
                foreach (string nameHash in Tracks.ElementAt(i).Value.Keys.Select(x => x.Name))
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

        private void FixNode(Dictionary<Node, Dictionary<int, object>> node, int frameCount)
        {
            foreach (KeyValuePair<Node, Dictionary<int, object>> keyValuePair in node)
            {
                if (keyValuePair.Value.ElementAt(keyValuePair.Value.Count - 1).Key != frameCount)
                {
                    keyValuePair.Value.Add(frameCount, keyValuePair.Value.ElementAt(keyValuePair.Value.Count - 1).Value);
                }
            }
        }
    }
}
