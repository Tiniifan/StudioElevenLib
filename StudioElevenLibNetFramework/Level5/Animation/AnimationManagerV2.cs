using System;
using System.IO;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using StudioElevenLib.Tools;
using StudioElevenLib.Level5.Compression;
using StudioElevenLib.Level5.Animation.Logic;
using StudioElevenLib.Level5.Compression.LZ10;

namespace StudioElevenLib.Level5.Animation
{
    public class AnimationManagerV2 : IAnimationManager
    {
        public string Format { get; set; }

        public string Version => "V2";

        public string AnimationName { get; set; }

        public int FrameCount { get; set; }

        public List<Track> Tracks { get; set; } = new List<Track>();

        public AnimationManagerV2()
        {

        }

        public AnimationManagerV2(Stream stream)
        {
            using (BinaryDataReader reader = new BinaryDataReader(stream))
            {
                // Read header
                AnimationSupport.Header header = AnimationSupport.ReadHeader(reader, out string format);
                Format = format;

                // Track 9 case
                int maxNodeBeforeTrack4 = 0;
                if (header.NameOffset == 0x28 && header.CompDataOffset == 0x58)
                {
                    reader.Seek(0x24);
                    maxNodeBeforeTrack4 = reader.ReadValue<int>();
                }

                // Get animation name and frame count
                AnimationSupport.ReadAnimationInfo(reader, header, out string animationName, out int frameCount);
                AnimationName = animationName;
                FrameCount = frameCount;

                // Get decomp block
                using (BinaryDataReader decompReader = new BinaryDataReader(Compressor.Decompress(reader.GetSection((int)(reader.Length - reader.Position)))))
                {
                    GetAnimationData(header, decompReader, maxNodeBeforeTrack4);
                }
            }
        }

        public byte[] Save()
        {
            using (MemoryStream stream = new MemoryStream())
            {
                BinaryDataWriter writer = new BinaryDataWriter(stream);

                AnimationSupport.Header header = AnimationSupport.CreateHeader(Format, Tracks);

                // Don't exceed 40 characters
                if (AnimationName.Length > 40)
                {
                    AnimationName = AnimationName.Substring(0, 40);
                }

                // Write animation hash, name and frame count
                if (Format == "XMTN")
                {
                    // XMTN stores the number of names of the location, rotation and scale nodes at 0x24, the name and the data are 4 bytes further
                    header.NameOffset = 0x28;
                    header.CompDataOffset = 0x58;

                    writer.Seek(0x24);
                    writer.Write(GetNameHashes().Count);
                    writer.Write(unchecked((int)Crc32.Compute(Encoding.GetEncoding("Shift-JIS").GetBytes(AnimationName))));
                    writer.Write(Encoding.GetEncoding("Shift-JIS").GetBytes(AnimationName));
                    writer.Write(Enumerable.Repeat((byte)0, (int)(0x54 - writer.Position)).ToArray());
                    writer.Write(FrameCount);
                }
                else
                {
                    AnimationSupport.WriteAnimationInfo(writer, AnimationName, FrameCount);
                }

                // Write animation data
                SaveAnimationData(ref header, writer);

                // Write header
                AnimationSupport.WriteHeader(writer, header, Format, Tracks);

                return stream.ToArray();
            }
        }

        private void GetAnimationData(AnimationSupport.Header header, BinaryDataReader decompReader, int maxNodeBeforeTrack4)
        {
            AnimationSupport.DataHeader dataHeader = decompReader.ReadStruct<AnimationSupport.DataHeader>();

            // Get name Hashes
            decompReader.Seek(dataHeader.HashOffset);
            int elementCount = (dataHeader.TrackOffset - dataHeader.HashOffset) / 4;
            int[] nameHashes = decompReader.ReadMultipleValue<int>(elementCount);

            // Name information
            int pos = 0;
            Dictionary<int, int[]> nameDict = new Dictionary<int, int[]>();
            nameDict.Add(0, nameHashes);
            nameDict.Add(1, nameHashes);
            nameDict.Add(2, nameHashes);
            nameDict.Add(3, nameHashes);

            // Track Information
            int[] trackCountList = new int[] { header.Track1Count, header.Track2Count, header.Track3Count, header.Track4Count };
            int trackCount = Convert.ToInt32(header.Track1Count != -1) + Convert.ToInt32(header.Track2Count != -1) + Convert.ToInt32(header.Track3Count != -1) + Convert.ToInt32(header.Track4Count != -1);

            List<AnimationSupport.Track> tracks = new List<AnimationSupport.Track>();
            for (int i = 0; i < trackCount; i++)
            {
                decompReader.Seek(dataHeader.TrackOffset + 2 * i);
                decompReader.Seek(decompReader.ReadValue<short>());
                tracks.Add(decompReader.ReadStruct<AnimationSupport.Track>());

                if (tracks[i].Type != 0)
                {
                    Tracks.Add(new Track(AnimationSupport.TrackType[tracks[i].Type], i));

                    if (i < 3 && trackCountList[i+1] == 0 && i+1 != 3)
                    {
                        if (maxNodeBeforeTrack4 < 1)
                        {
                            pos += trackCountList[i] * 4;

                            for (int j = i + 1; j < trackCount; j++)
                            {
                                nameDict[j] = null;
                            }
                        }
                    } else if (tracks[i].Type == 9 && maxNodeBeforeTrack4 > 0)
                    {
                        nameDict[i] = null;
                        pos += maxNodeBeforeTrack4 * 4;
                    }
                }

                if (nameDict[i] == null)
                {
                    decompReader.Seek(dataHeader.HashOffset + pos);
                    nameDict[i] = decompReader.ReadMultipleValue<int>(trackCountList[i]);
                }
            }

            int offset = 0;
            int index = 0;
            int trackIndex = 0;

            if (header.Track1Count > 0)
            {
                ReadFrameData(decompReader, offset, header.Track1Count, dataHeader.DataOffset, nameDict[0], tracks[0], trackIndex);
                trackIndex++;
            }
            offset += header.Track1Count;
            index++;

            if (header.Track2Count > 0)
            {
                ReadFrameData(decompReader, offset, header.Track2Count, dataHeader.DataOffset, nameDict[1], tracks[1], trackIndex);
                trackIndex++;
            }
            offset += header.Track2Count;
            index++;

            if (header.Track3Count > 0)
            {
                ReadFrameData(decompReader, offset, header.Track3Count, dataHeader.DataOffset, nameDict[2], tracks[2], trackIndex);
                trackIndex++;
            }
            offset += header.Track3Count;
            index++;

            if (header.Track4Count > 0)
            {
                ReadFrameData(decompReader, offset, header.Track4Count, dataHeader.DataOffset, nameDict[3], tracks[3], trackIndex);
            }
        }

        private void SaveAnimationData(ref AnimationSupport.Header header, BinaryDataWriter writer)
        {
            using (MemoryStream memoryStream = new MemoryStream())
            {
                BinaryDataWriter writerDecomp = new BinaryDataWriter(memoryStream);

                int hashCount = AnimationSupport.CountHashes(Tracks);

                // The BoneBool nodes index a second list of names, after the names of the other tracks
                List<int> nameHashes = GetNameHashes();
                List<int> boolHashes = GetBoolHashes();
                int hashCountDistinct = nameHashes.Count + boolHashes.Count;

                // Write data header
                writerDecomp.Write(0x0C);
                writerDecomp.Write(0x0C + hashCountDistinct * 4);
                writerDecomp.Write((0x0C + hashCountDistinct * 4) + 4 * 10);

                // Write name hash
                if (hashCountDistinct > 0)
                {
                    writerDecomp.WriteMultipleStruct<int>(nameHashes.Concat(boolHashes).ToList());
                }

                // Store position
                int trackOffset = (int)writerDecomp.Position + 4 * 2;
                int trackDataOffset = (int)writerDecomp.Position + 4 * 2;
                int tableOffset = (0x0C + hashCountDistinct * 4) + 4 * 10;
                int dataOffset = tableOffset + hashCount * 16;

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
                        Track myTrack = Tracks.ElementAt(i);

                        if (myTrack.Nodes.Count() > 0)
                        {
                            track.Type = (byte)AnimationSupport.TrackType.FirstOrDefault(x => x.Value == myTrack.Name).Key;
                            track.DataType = (byte)AnimationSupport.TrackDataType[myTrack.Name];
                            track.Unk = 0;
                            track.DataCount = (byte)AnimationSupport.TrackDataCount[myTrack.Name];
                            track.Start = 0;
                            track.End = (short)FrameCount;

                            foreach (Node node in myTrack.Nodes)
                            {
                                // Write table header
                                writerDecomp.Seek(tableOffset);
                                writerDecomp.Write(dataOffset);
                                writerDecomp.Write(dataOffset + 4);
                                tableOffset += 8;

                                // Write data
                                writerDecomp.Seek(dataOffset);
                                if (myTrack.Name == "BoneBool")
                                {
                                    writerDecomp.Write((short)boolHashes.IndexOf(Convert.ToInt32(node.Name, 16)));
                                }
                                else
                                {
                                    writerDecomp.Write((short)nameHashes.IndexOf(Convert.ToInt32(node.Name, 16)));
                                }

                                // Key count on 13 bits, mode on the 3 high bits: 0 holds the keys, 1 interpolates
                                int mode = node.IsInMainTrack ? 1 : 0;
                                writerDecomp.Write((ushort)((node.Frames.Count() & 0x1FFF) | (mode << 13)));

                                // Write frames
                                writerDecomp.WriteMultipleStruct<short>(node.Frames.Select(x => Convert.ToInt16(x.Key)).ToArray());
                                writerDecomp.WriteAlignment(4, 0);

                                // Keep value offset
                                int valueOffset = (int)writerDecomp.Position;

                                // Write value
                                foreach (object value in node.Frames.Select(x => x.Value))
                                {
                                    writerDecomp.Write(AnimationSupport.ValueToByteArray(myTrack.Name, value));
                                }

                                // The next node starts on 4 bytes (the bool values are 1 byte)
                                writerDecomp.WriteAlignment(4, 0);

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

                // Memory the game reserves: the decompressed data and 84 bytes per node
                header.DecompSize = (int)memoryStream.Length + 84 * hashCount;
                writer.Write(new LZ10().Compress(memoryStream.ToArray()));
            }
        }

        private List<int> GetNameHashes()
        {
            List<int> nameHashes = new List<int>();

            foreach (Track track in Tracks.Where(x => x.Name != "BoneBool"))
            {
                foreach (Node node in track.Nodes)
                {
                    int name = Convert.ToInt32(node.Name, 16);

                    if (!nameHashes.Contains(name))
                    {
                        nameHashes.Add(name);
                    }
                }
            }

            return nameHashes;
        }

        private List<int> GetBoolHashes()
        {
            List<int> boolHashes = new List<int>();

            foreach (Track track in Tracks.Where(x => x.Name == "BoneBool"))
            {
                foreach (Node node in track.Nodes)
                {
                    boolHashes.Add(Convert.ToInt32(node.Name, 16));
                }
            }

            return boolHashes;
        }

        private void ReadFrameData(BinaryDataReader data, int offset, int count, int dataOffset, int[] nameHashes, AnimationSupport.Track track, int trackIndex)
        {
            for (int i = offset; i < offset + count; i++)
            {
                data.Seek(dataOffset + 4 * 4 * i);

                int flagOffset = data.ReadValue<int>();
                int keyFrameOffset = data.ReadValue<int>();
                int keyDataOffset = data.ReadValue<int>();

                data.Seek(flagOffset);
                int index = data.ReadValue<short>();
                string nameHash = nameHashes[index].ToString("X8");

                // Key count on 13 bits, mode on the 3 high bits: 0 holds the keys, any other mode interpolates
                int keyFrameCountFlag = data.ReadValue<ushort>();
                int keyFrameCount = keyFrameCountFlag & 0x1FFF;
                bool isMainTrack = (keyFrameCountFlag >> 13) != 0;

                data.Seek(keyDataOffset);
                List<Frame> frames = new List<Frame>();
                for (int k = 0; k < keyFrameCount; k++)
                {
                    long temp = data.Position;
                    data.Seek(keyFrameOffset + k * 2);
                    int frame = data.ReadValue<short>();
                    data.Seek((int)temp);

                    object[] animData = AnimationSupport.ReadAnimData(data, track.DataType, track.DataCount);

                    frames.Add(new Frame(frame, AnimationSupport.ConvertAnimDataToObject(animData, track.Type)));
                }

                // Create node
                Tracks[trackIndex].Nodes.Add(new Node(nameHash, isMainTrack, frames));
            }
        }
    }
}
