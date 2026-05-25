using System;
using System.Collections.Generic;

namespace StudioElevenLib.Level5.Animation.Logic
{
    public class Track
    {
        public string Name { get; set; }

        public int Index { get; set; }

        public List<Node> Nodes { get; set; }

        public Track()
        {
            Index = -1;
            Nodes = new List<Node>();
        }

        public Track(string name)
        {
            Name = name;
            Index = -1;
            Nodes = new List<Node>();
        }

        public Track(string name, int index)
        {
            Name = name;
            Index = index;
            Nodes = new List<Node>();
        }

        public Track(string name, List<Node> nodes)
        {
            Name = name;
            Index = -1;
            Nodes = nodes;
        }

        public Track(string name, int index, List<Node> nodes)
        {
            Name = name;
            Index = index;
            Nodes = nodes;
        }

        public Node GetNodeByName(string name)
        {
            foreach (var nodeEntry in Nodes)
            {
                if (nodeEntry.Name == name)
                {
                    return nodeEntry;
                }
            }

            return null;
        }

        public bool NodeExists(string name)
        {
            foreach (var nodeEntry in Nodes)
            {
                if (nodeEntry.Name == name)
                {
                    return true;
                }
            }

            return false;
        }
    }
}
