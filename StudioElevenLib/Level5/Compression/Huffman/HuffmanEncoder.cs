using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using StudioElevenLib.Tools;

// HuffmanEncoder From https://github.com/FanTranslatorsInternational/Kuriimu2

namespace StudioElevenLib.Level5.Compression.Huffman
{
    internal class HuffmanNode
    {
        public int Frequency;
        public int Code;
        public bool IsLeaf;
        public byte Value;
        public HuffmanNode[] Children;

        public IEnumerable<(int Symbol, string Bits)> GetHuffCodes(string prefix = "")
        {
            if (IsLeaf)
            {
                yield return (Value, prefix.Length == 0 ? "0" : prefix);
            }
            else
            {
                if (Children == null) yield break;
                foreach (var c in Children[0].GetHuffCodes(prefix + "0")) yield return c;
                foreach (var c in Children[1].GetHuffCodes(prefix + "1")) yield return c;
            }
        }
    }
    internal class HuffmanEncoder
    {
        private readonly int _bitDepth;

        public HuffmanEncoder(int bitDepth) => _bitDepth = bitDepth;

        public void Encode(byte[] data, Stream output)
        {
            int symbolCount = 1 << _bitDepth;

            var freq = new int[symbolCount];
            if (_bitDepth == 8)
            {
                foreach (var b in data) freq[b]++;
            }
            else
            {
                foreach (var b in data) { freq[b & 0xF]++; freq[b >> 4]++; }
            }

            var root = BuildTree(freq, symbolCount);

            var labelList = LabelTreeNodes(root);

            var bitCodes = labelList[0]
                .GetHuffCodes()
                .ToDictionary(x => x.Symbol, x => x.Bits);

            using (var bw = new BinaryWriter(output, System.Text.Encoding.Default, leaveOpen: true))
            {
                bw.Write((byte)labelList.Count);

                var nodesToWrite = labelList
                    .Take(1)
                    .Concat(labelList.SelectMany(n => n.Children ?? Array.Empty<HuffmanNode>()));

                foreach (var node in nodesToWrite)
                {
                    byte code = (byte)node.Code;
                    if (node.Children != null)
                    {
                        if (node.Children[0].IsLeaf) code |= 0x80;
                        if (node.Children[1].IsLeaf) code |= 0x40;
                    }
                    bw.Write(code);
                }

                while (output.Position % 4 != 0) bw.Write((byte)0);

                var bitWriter = new MsbBitWriter(output);

                if (_bitDepth == 4)
                {
                    foreach (var b in data)
                    {
                        foreach (var ch in bitCodes[b & 0xF]) bitWriter.WriteBit(ch == '1');
                        foreach (var ch in bitCodes[b >> 4]) bitWriter.WriteBit(ch == '1');
                    }
                }
                else
                {
                    foreach (var b in data)
                        foreach (var ch in bitCodes[b]) bitWriter.WriteBit(ch == '1');
                }

                bitWriter.Flush();
            }
        }

        private static HuffmanNode BuildTree(int[] freq, int symbolCount)
        {
            var queue = new List<HuffmanNode>();
            for (int i = 0; i < symbolCount; i++)
                if (freq[i] > 0)
                    queue.Add(new HuffmanNode { Frequency = freq[i], IsLeaf = true, Value = (byte)i });

            if (queue.Count == 0) queue.Add(new HuffmanNode { Frequency = 1, IsLeaf = true, Value = 0 });
            if (queue.Count == 1) queue.Add(new HuffmanNode { Frequency = 1, IsLeaf = true, Value = 0 });

            while (queue.Count > 1)
            {
                queue.Sort((a, b) => a.Frequency.CompareTo(b.Frequency));
                var left = queue[0];
                var right = queue[1];
                queue.RemoveRange(0, 2);
                queue.Add(new HuffmanNode
                {
                    Frequency = left.Frequency + right.Frequency,
                    IsLeaf = false,
                    Children = new[] { left, right }
                });
            }

            return queue[0];
        }

        private static List<HuffmanNode> LabelTreeNodes(HuffmanNode rootNode)
        {
            var labelList = new List<HuffmanNode>();
            var frequencies = new List<HuffmanNode> { rootNode };
            rootNode.Code = 0;

            while (frequencies.Count > 0)
            {
                var node = frequencies
                    .Select((n, i) => new { Node = n, Score = n.Code - i })
                    .OrderBy(x => x.Score)
                    .First().Node;

                frequencies.Remove(node);
                node.Code = labelList.Count - node.Code;
                labelList.Add(node);

                if (node.Children == null) continue;

                foreach (var child in node.Children.Reverse().Where(c => !c.IsLeaf))
                {
                    child.Code = labelList.Count;
                    frequencies.Add(child);
                }
            }

            return labelList;
        }
    }
}
