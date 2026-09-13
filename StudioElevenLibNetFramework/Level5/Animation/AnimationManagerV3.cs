using System;
using System.Text;
using System.IO;
using System.Collections.Generic;
using System.Linq;
using StudioElevenLib.Tools;
using StudioElevenLib.Level5.Compression;
using StudioElevenLib.Level5.Animation.Logic;
using StudioElevenLib.Level5.Compression.LZ10;

namespace StudioElevenLib.Level5.Animation
{
    public class AnimationManagerV3 : IAnimationManager
    {
        public string Format { get; set; }

        public string Version => "V3";

        public string AnimationName { get; set; }

        public int FrameCount { get; set; }

        public List<Track> Tracks { get; set; } = new List<Track>();

        public AnimationManagerV3()
        {

        }

        public AnimationManagerV3(Stream stream)
        {
            using (BinaryDataReader reader = new BinaryDataReader(stream))
            {
                // Read header
                AnimationSupport.HeaderV3 header = reader.ReadStruct<AnimationSupport.HeaderV3>();

                // Get format name
                byte[] formatBytes = BitConverter.GetBytes(header.Magic);
                formatBytes = Array.FindAll(formatBytes, b => b != 0);
                Format = Encoding.UTF8.GetString(formatBytes);

                // Get node count for each track
                int[] trackCounts = reader.ReadMultipleValue<int>(AnimationSupport.TrackCountV3[Format]);

                // Bone animation stores how many name hashes are shared between location, rotation and scale
                int sharedHashCount = -1;
                if (Format == "XMTN")
                {
                    sharedHashCount = reader.ReadValue<int>();
                }

                // Get animation name
                long nameOffset = reader.Position + header.NameOffset;
                reader.Seek(nameOffset);
                int animHash = reader.ReadValue<int>();
                AnimationName = reader.ReadString(Encoding.UTF8);

                // Get frame count
                reader.Seek(nameOffset + 0x2C);
                FrameCount = reader.ReadValue<int>();

                // Get track offsets
                reader.Seek(nameOffset + header.TrackOffset);
                short[] trackOffsets = reader.ReadMultipleValue<short>(trackCounts.Length);

                // Get node tables
                reader.Seek(nameOffset + header.TableOffset);
                AnimationSupport.NodeTableV3[] nodeTables = reader.ReadMultipleStruct<AnimationSupport.NodeTableV3>(trackCounts.Sum());

                // Get decomp block
                using (BinaryDataReader decompReader = new BinaryDataReader(Compressor.Decompress(reader.GetSection((int)(reader.Length - reader.Position)))))
                {
                    GetAnimationData(header, decompReader, trackCounts, trackOffsets, nodeTables, sharedHashCount);
                }
            }
        }

        public byte[] Save()
        {
            using (MemoryStream stream = new MemoryStream())
            {
                BinaryDataWriter writer = new BinaryDataWriter(stream);

                int trackCount = AnimationSupport.TrackCountV3[Format];
                int nameOffset = 0x18 + trackCount * 4 + Convert.ToInt32(Format == "XMTN") * 4;

                AnimationSupport.HeaderV3 header = new AnimationSupport.HeaderV3
                {
                    Magic = (int)AnimationSupport.FormatNameToLong(Format),
                    TrackOffset = 0x30,
                    TableOffset = (short)(0x30 + trackCount * 2),
                    DataOffset = 0,
                    NameOffset = 0,
                    DataLength = 0,
                    DecompOffset = 0,
                };

                // Get node count for each track
                int[] trackCounts = Enumerable.Range(0, trackCount).Select(x => GetTrack(x) != null ? GetTrack(x).Nodes.Count() : 0).ToArray();

                // Don't exceed 40 characters
                if (AnimationName.Length > 40)
                {
                    AnimationName = AnimationName.Substring(0, 40);
                }

                // Write animation hash
                writer.Seek(nameOffset);
                writer.Write(unchecked((int)Crc32.Compute(Encoding.GetEncoding("Shift-JIS").GetBytes(AnimationName))));
                writer.Write(Encoding.GetEncoding("Shift-JIS").GetBytes(AnimationName));
                writer.Write(Enumerable.Repeat((byte)0, (int)(nameOffset + 0x2C - writer.Position)).ToArray());
                writer.Write(FrameCount);

                // Write animation data
                int sharedHashCount = SaveAnimationData(ref header, writer, nameOffset, trackCount);

                // Write header
                writer.Seek(0);
                writer.WriteStruct(header);
                writer.WriteMultipleStruct<int>(trackCounts);

                if (Format == "XMTN")
                {
                    writer.Write(sharedHashCount);
                }

                return stream.ToArray();
            }
        }

        private void GetAnimationData(AnimationSupport.HeaderV3 header, BinaryDataReader decompReader, int[] trackCounts, short[] trackOffsets, AnimationSupport.NodeTableV3[] nodeTables, int sharedHashCount)
        {
            // Get name Hashes
            decompReader.Seek(0x0);
            int[] nameHashes = decompReader.ReadMultipleValue<int>(trackOffsets[0] / 4);

            int tableIndex = 0;
            int hashOffset = 0;

            for (int i = 0; i < trackCounts.Length; i++)
            {
                // Read track information
                decompReader.Seek(trackOffsets[i]);
                AnimationSupport.Track track = decompReader.ReadStruct<AnimationSupport.Track>();

                // Each track has its own name hashes except the first three tracks of the bone animation
                int trackHashOffset = hashOffset;
                if (sharedHashCount > -1)
                {
                    trackHashOffset = i < 3 ? 0 : sharedHashCount;
                }

                if (track.Type != 0)
                {
                    Tracks.Add(new Track(AnimationSupport.TrackType[track.Type], i));
                    ReadFrameData(decompReader, nodeTables.Skip(tableIndex).Take(trackCounts[i]).ToArray(), header.DataOffset, nameHashes, trackHashOffset, track, Tracks.Count - 1);
                }

                tableIndex += trackCounts[i];
                hashOffset += trackCounts[i];
            }
        }

        private int SaveAnimationData(ref AnimationSupport.HeaderV3 header, BinaryDataWriter writer, int nameOffset, int trackCount)
        {
            using (MemoryStream memoryStream = new MemoryStream())
            {
                BinaryDataWriter writerDecomp = new BinaryDataWriter(memoryStream);

                List<int> nameHashes = GetNameHashes(trackCount, out int sharedHashCount);

                // Write name hash
                if (nameHashes.Count > 0)
                {
                    writerDecomp.WriteMultipleStruct<int>(nameHashes);
                }

                // Store position
                int trackOffset = (int)writerDecomp.Position;
                int tableOffset = nameOffset + header.TableOffset;
                header.DataOffset = trackOffset + trackCount * 8;

                // Loop in tracks
                for (int i = 0; i < trackCount; i++)
                {
                    // Write track offsets
                    writer.Seek(nameOffset + header.TrackOffset + i * 2);
                    writer.Write((short)(trackOffset + i * 8));

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

                    Track myTrack = GetTrack(i);

                    if (myTrack != null && myTrack.Nodes.Count() > 0)
                    {
                        track.Type = (byte)AnimationSupport.TrackType.FirstOrDefault(x => x.Value == myTrack.Name).Key;
                        track.DataType = (byte)AnimationSupport.TrackDataTypeV3[myTrack.Name];
                        track.Unk = 0;
                        track.DataCount = (byte)AnimationSupport.TrackDataCount[myTrack.Name];
                        track.Start = 0;
                        track.End = (short)FrameCount;
                    }

                    // Write track data
                    writerDecomp.Seek(trackOffset + i * 8);
                    writerDecomp.WriteStruct(track);
                }

                // Loop in tracks
                writerDecomp.Seek(header.DataOffset);
                for (int i = 0; i < trackCount; i++)
                {
                    Track myTrack = GetTrack(i);

                    if (myTrack == null || myTrack.Nodes.Count() == 0) continue;

                    for (int j = 0; j < myTrack.Nodes.Count(); j++)
                    {
                        Node node = myTrack.Nodes[j];

                        // Keep flag offset
                        int flagOffset = (int)writerDecomp.Position - header.DataOffset;

                        // Write name index
                        if (sharedHashCount > -1 && i < 3)
                        {
                            writerDecomp.Write((short)nameHashes.IndexOf(Convert.ToInt32(node.Name, 16)));
                        }
                        else
                        {
                            writerDecomp.Write((short)j);
                        }

                        // Frame count
                        int lowFrameCount = node.Frames.Count() & 0xFF;
                        int hightFrameCount = (node.Frames.Count() >> 8) & 0xFF;
                        writerDecomp.Write((byte)lowFrameCount);

                        if (node.IsInMainTrack || node.Frames.Count() > 0xFF)
                        {
                            writerDecomp.Write((byte)(32 + hightFrameCount));
                        }
                        else
                        {
                            writerDecomp.Write((byte)0x00);
                        }

                        // Keep key frame offset
                        int keyFrameOffset = (int)writerDecomp.Position - header.DataOffset;

                        // Write frames
                        writerDecomp.WriteMultipleStruct<short>(node.Frames.Select(x => Convert.ToInt16(x.Key)).ToArray());
                        writerDecomp.WriteAlignment(4, 0);

                        // Keep value offset
                        int keyDataOffset = (int)writerDecomp.Position - header.DataOffset;

                        // Write value
                        foreach (object value in node.Frames.Select(x => x.Value))
                        {
                            writerDecomp.Write(ValueToByteArray(myTrack.Name, value));
                        }
                        writerDecomp.WriteAlignment(4, 0);

                        AnimationSupport.NodeTableV3 nodeTable = new AnimationSupport.NodeTableV3
                        {
                            FlagOffset = flagOffset,
                            KeyFrameOffset = keyFrameOffset,
                            KeyDataOffset = keyDataOffset,
                            EmptyValue = 0,
                        };

                        // Write node table
                        writer.Seek(tableOffset);
                        writer.WriteStruct(nodeTable);
                        tableOffset += 16;
                    }
                }

                // XIMA swaps the data length and the decomp offset
                if (Format == "XIMA")
                {
                    header.DecompOffset = (int)writerDecomp.Position - header.DataOffset;
                }
                else
                {
                    header.DataLength = (int)writerDecomp.Position - header.DataOffset;
                }

                // Write end padding
                writerDecomp.Write(new byte[0x30]);

                // Write compressed data after the node tables
                writer.Seek(tableOffset);
                writer.Write(new LZ10().Compress(memoryStream.ToArray()));

                return sharedHashCount;
            }
        }

        private void ReadFrameData(BinaryDataReader data, AnimationSupport.NodeTableV3[] nodeTables, int dataOffset, int[] nameHashes, int hashOffset, AnimationSupport.Track track, int trackIndex)
        {
            foreach (AnimationSupport.NodeTableV3 nodeTable in nodeTables)
            {
                bool isMainTrack = true;

                data.Seek(dataOffset + nodeTable.FlagOffset);
                int index = data.ReadValue<short>();
                string nameHash = nameHashes[hashOffset + index].ToString("X8");

                int lowFrameCount = data.ReadValue<byte>();
                int highFrameCount = data.ReadValue<byte>();
                int keyFrameCount = 0;

                if (highFrameCount == 0)
                {
                    isMainTrack = false;
                    keyFrameCount = lowFrameCount;
                }
                else
                {
                    highFrameCount -= 32;
                    keyFrameCount = (highFrameCount << 8) | lowFrameCount;
                }

                data.Seek(dataOffset + nodeTable.KeyDataOffset);
                List<Frame> frames = new List<Frame>();
                for (int k = 0; k < keyFrameCount; k++)
                {
                    long temp = data.Position;
                    data.Seek(dataOffset + nodeTable.KeyFrameOffset + k * 2);
                    int frame = data.ReadValue<short>();
                    data.Seek((int)temp);

                    object[] animData = AnimationSupport.ReadAnimData(data, track.DataType, track.DataCount);

                    frames.Add(new Frame(frame, AnimationSupport.ConvertAnimDataToObject(animData, track.Type)));
                }

                // Create node
                Tracks[trackIndex].Nodes.Add(new Node(nameHash, isMainTrack, frames));
            }
        }

        private byte[] ValueToByteArray(string type, object value)
        {
            // Rotation is stored as short
            if (type == "BoneRotation")
            {
                BoneRotation rotation = (BoneRotation)value;
                return new float[] { rotation.X, rotation.Y, rotation.Z, rotation.W }
                    .SelectMany(x => BitConverter.GetBytes((short)Math.Round(Math.Max(-1, Math.Min(1, x)) * 0x7FFF)))
                    .ToArray();
            }
            else
            {
                return AnimationSupport.ValueToByteArray(type, value);
            }
        }

        private Track GetTrack(int searchIndex)
        {
            return Tracks.Any(x => x.Index == searchIndex)
                ? Tracks.FirstOrDefault(x => x.Index == searchIndex)
                    : (Tracks.Count > searchIndex && Tracks[searchIndex].Index == -1)
                        ? Tracks[searchIndex]
                            : null;
        }

        private List<int> GetNameHashes(int trackCount, out int sharedHashCount)
        {
            List<int> nameHashes = new List<int>();
            sharedHashCount = -1;

            for (int i = 0; i < trackCount; i++)
            {
                Track track = GetTrack(i);

                if (track != null)
                {
                    int lastIndex = -1;

                    foreach (string nameHash in track.Nodes.Select(x => x.Name))
                    {
                        int nameInt = Convert.ToInt32(nameHash, 16);

                        // Bone animation shares the name hashes between location, rotation and scale
                        // Missing hashes are inserted after the previous node to keep the bone order
                        if (Format == "XMTN" && i < 3)
                        {
                            if (!nameHashes.Contains(nameInt))
                            {
                                nameHashes.Insert(lastIndex + 1, nameInt);
                            }

                            lastIndex = nameHashes.IndexOf(nameInt);
                        }
                        else
                        {
                            nameHashes.Add(nameInt);
                        }
                    }
                }

                if (Format == "XMTN" && i == 2)
                {
                    sharedHashCount = nameHashes.Count;
                }
            }

            return nameHashes;
        }
    }
}
