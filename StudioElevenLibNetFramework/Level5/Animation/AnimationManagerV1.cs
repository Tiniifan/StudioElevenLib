using System;
using System.IO;
using System.Collections.Generic;
using System.Linq;
using StudioElevenLib.Tools;
using StudioElevenLib.Level5.Compression;
using StudioElevenLib.Level5.Animation.Logic;
using StudioElevenLib.Level5.Compression.LZ10;

namespace StudioElevenLib.Level5.Animation
{
    public class AnimationManagerV1 : IAnimationManager
    {
        public string Format { get; set; }

        public string Version => "V1";

        public string AnimationName { get; set; }

        public int FrameCount { get; set; }

        public List<Track> Tracks { get; set; } = new List<Track>();

        public AnimationManagerV1()
        {

        }

        public AnimationManagerV1(Stream stream)
        {
            using (BinaryDataReader reader = new BinaryDataReader(stream))
            {
                // Read header
                AnimationSupport.Header header = AnimationSupport.ReadHeader(reader, out string format);
                Format = format;

                // Get animation name and frame count
                AnimationSupport.ReadAnimationInfo(reader, header, out string animationName, out int frameCount);
                AnimationName = animationName;
                FrameCount = frameCount;

                // Get decomp block
                using (BinaryDataReader decompReader = new BinaryDataReader(Compressor.Decompress(reader.GetSection((int)(reader.Length - reader.Position)))))
                {
                    GetAnimationData(header, decompReader);
                }
            }
        }

        public byte[] Save()
        {
            using (MemoryStream stream = new MemoryStream())
            {
                BinaryDataWriter writer = new BinaryDataWriter(stream);

                // V1 writes its nodes by group, the group decides the interpolation
                List<Track> groups = GetGroups();

                AnimationSupport.Header header = AnimationSupport.CreateHeader(Format, groups);

                // Don't exceed 40 characters
                if (AnimationName.Length > 40)
                {
                    AnimationName = AnimationName.Substring(0, 40);
                }

                // Write animation hash, name and frame count
                AnimationSupport.WriteAnimationInfo(writer, AnimationName, FrameCount);

                // Write animation data
                //Tracks.RemoveAll(x => x.Name == "Unk");
                SaveAnimationData(ref header, writer, groups);

                // Write header
                AnimationSupport.WriteHeader(writer, header, Format, groups);

                return stream.ToArray();
            }
        }

        private void GetAnimationData(AnimationSupport.Header header, BinaryDataReader decompReader)
        {
            int trackIndex = 0;
            long tableOffset = 0;

            if (header.Track1Count > 0)
            {
                for (int i = 0; i < header.Track1Count; i++)
                {
                    ReadFrameData(decompReader, ref tableOffset, 0, trackIndex);
                }

                trackIndex++;
            }

            if (header.Track2Count > 0)
            {
                for (int i = 0; i < header.Track2Count; i++)
                {
                    ReadFrameData(decompReader, ref tableOffset, 1, trackIndex);
                }

                trackIndex++;
            }

            if (header.Track3Count > 0)
            {
                for (int i = 0; i < header.Track3Count; i++)
                {
                    ReadFrameData(decompReader, ref tableOffset, 2, trackIndex);
                }

                trackIndex++;
            }

            if (header.Track4Count > 0)
            {
                for (int i = 0; i < header.Track4Count; i++)
                {
                    ReadFrameData(decompReader, ref tableOffset, 3, trackIndex);
                }

                trackIndex++;
            }
        }

        private List<Track> GetGroups()
        {
            List<Track> groups = new List<Track>();

            for (int i = 0; i < 4; i++)
            {
                groups.Add(new Track(null, i));
            }

            foreach (Track track in Tracks)
            {
                foreach (Node node in track.Nodes)
                {
                    int group = TrackGroup[track.Name];
                    Node groupNode = node;

                    if (!node.IsInMainTrack)
                    {
                        if (track.Name == "UVMove" || track.Name == "BoneBool")
                        {
                            group = 3;
                        }
                        else
                        {
                            // V1 only holds the keys of group 3, the other nodes jump from a key to the next one in one frame
                            groupNode = new Node(node.Name, true, GetStepFrames(node.Frames));
                        }
                    }

                    groups[group].Name = track.Name;
                    groups[group].Nodes.Add(groupNode);
                }
            }

            return groups;
        }

        private static List<Frame> GetStepFrames(List<Frame> frames)
        {
            List<Frame> stepFrames = new List<Frame>();

            for (int i = 0; i < frames.Count; i++)
            {
                if (i > 0 && frames[i].Key - frames[i - 1].Key > 1)
                {
                    stepFrames.Add(new Frame(frames[i].Key - 1, frames[i - 1].Value));
                }

                stepFrames.Add(frames[i]);
            }

            return stepFrames;
        }

        // V1 has 4 node groups, group 3 holds its keys (UV move and bool only)
        private static readonly Dictionary<string, int> TrackGroup = new Dictionary<string, int>
        {
            { "BoneLocation", 0 },
            { "BoneRotation", 1 },
            { "BoneScale", 2 },
            { "BoneBool", 3 },
            { "UVMove", 0 },
            { "UVRotate", 1 },
            { "UVScale", 2 },
            { "MaterialTransparency", 0 },
            { "MaterialAttribute", 1 },
        };

        private void SaveAnimationData(ref AnimationSupport.Header header, BinaryDataWriter writer, List<Track> groups)
        {
            if (Tracks == null || Tracks.Count() == 0) return;

            using (MemoryStream memoryStream = new MemoryStream())
            {
                BinaryDataWriter writerDecomp = new BinaryDataWriter(memoryStream);

                int hashCount = AnimationSupport.CountHashes(Tracks);

                long headerPos = 0;
                long nodeOffset = hashCount * 20;

                for (int i = 0; i < 4; i++)
                {
                    if (i < groups.Count)
                    {
                        Track track = groups.ElementAt(i);

                        // After the last key the game interpolates toward the first key, a key on the last frame stops it
                        FixNode(track.Nodes.Where(x => x.IsInMainTrack).ToList(), FrameCount);

                        if (track.Nodes.Count() > 0)
                        {
                            foreach (Node node in track.Nodes)
                            {
                                int nameInt = Convert.ToInt32(node.Name, 16);
                                int dataVectorSize = AnimationSupport.TrackDataCount[track.Name];

                                AnimationSupport.Node nodeHeader = new AnimationSupport.Node
                                {
                                    BoneNameHash = nameInt,
                                    NodeType = (byte)AnimationSupport.TrackType.FirstOrDefault(x => x.Value == track.Name).Key,
                                    DataType = (byte)AnimationSupport.TrackDataType[track.Name],
                                    IsInMainTrack = (byte)Convert.ToInt32(node.IsInMainTrack),
                                    Unk2 = 0,
                                    FrameStart = 0,
                                    FrameEnd = FrameCount,
                                    DataCount = node.Frames.Count,
                                    DifferentFrameCount = FrameCount + 1,
                                    DataByteSize = AnimationSupport.TrackDataSize[track.Name],
                                    DataVectorSize = dataVectorSize,
                                    DataVectorLength = dataVectorSize * AnimationSupport.TrackDataSize[track.Name],
                                    DifferentFrameLength = (FrameCount + 1) * 2,
                                    FrameLength = node.Frames.Count * 2,
                                    DataLength = node.Frames.Count * dataVectorSize * AnimationSupport.TrackDataSize[track.Name]
                                };

                                // Write node table
                                writerDecomp.Seek(nodeOffset);
                                writerDecomp.WriteStruct(nodeHeader);

                                // write key frame table
                                long keyFrameOffset = writerDecomp.Position;
                                writerDecomp.Write(FillArray(node.Frames.Select(x => x.Key).ToArray(), FrameCount + 1).SelectMany(x => BitConverter.GetBytes((short)x)).ToArray());
                                writerDecomp.WriteAlignment2(4, 0);

                                // Write different key frame table
                                long differentKeyFrameOffset = writerDecomp.Position;
                                writerDecomp.Write(node.Frames.SelectMany(x => BitConverter.GetBytes((short)x.Key)).ToArray());
                                writerDecomp.WriteAlignment2(4, 0);

                                // writer animation data
                                long dataOffset = writerDecomp.Position;
                                writerDecomp.Write(node.Frames.SelectMany(x => AnimationSupport.ValueToByteArray(track.Name, x.Value)).ToArray());
                                if (AnimationSupport.TrackDataSize[track.Name] != 4)
                                {
                                    writerDecomp.WriteAlignment2(4, 0);
                                }

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

        private void ReadFrameData(BinaryDataReader decompReader, ref long tableOffset, int trackNum, int trackIndex)
        {
            // Read offset table
            decompReader.Seek(tableOffset);
            AnimationSupport.TableHeader tableHeader = decompReader.ReadStruct<AnimationSupport.TableHeader>();
            tableOffset = decompReader.Position;

            // Read node
            decompReader.Seek(tableHeader.NodeOffset);
            AnimationSupport.Node node = decompReader.ReadStruct<AnimationSupport.Node>();

            // Add the track if it doesn't exist
            if (Tracks.All(t => t.Index != trackNum) && node.NodeType != 0)
            {
                Tracks.Add(new Track(AnimationSupport.TrackType[node.NodeType], trackNum));
            }

            // Get data index for frame
            decompReader.Seek(tableHeader.KeyFrameOffset);
            int[] dataIndexes = decompReader.ReadMultipleValue<short>(node.DifferentFrameLength / 2).Select(x => Convert.ToInt32(x)).Distinct().ToArray();

            // Get different frame index
            decompReader.Seek(tableHeader.DifferentKeyFrameOffset);
            int[] differentFrames = decompReader.ReadMultipleValue<short>(node.FrameLength / 2).Select(x => Convert.ToInt32(x)).ToArray();

            List<Frame> frames = new List<Frame>();

            for (int j = 0; j < differentFrames.Length; j++)
            {
                // Get frame
                int frame = differentFrames[j];
                int dataIndex = dataIndexes[j];

                // Seek data offset
                decompReader.Seek(tableHeader.DataOffset + j * node.DataVectorSize * node.DataByteSize);

                // Decode animation data
                object[] animData = AnimationSupport.ReadAnimData(decompReader, node.DataType, node.DataVectorSize, node.DataByteSize);

                frames.Add(new Frame(frame, AnimationSupport.ConvertAnimDataToObject(animData, node.NodeType)));
            }

            // Create node
            // The group decides the interpolation, the game never reads the byte of the node
            Tracks[trackIndex].Nodes.Add(new Node(node.BoneNameHash.ToString("X8"), trackNum != 3, frames));
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
                    if (j < size)
                    {
                        result[j] = lastIndex;
                    }
                }

                lastIndex++;
            }

            return result;
        }

        private void FixNode(List<Node> nodes, int frameCount)
        {
            foreach (Node node in nodes)
            {
                if (node.Frames.ElementAt(node.Frames.Count - 1).Key != frameCount)
                {
                    node.Frames.Add(new Frame(frameCount, node.Frames.ElementAt(node.Frames.Count - 1).Value));
                }
            }
        }
    }
}
