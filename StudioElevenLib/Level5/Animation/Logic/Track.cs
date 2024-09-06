using System;
using System.Collections.Generic;

namespace StudioElevenLib.Level5.Animation.Logic
{
    public class Track
    {
        public string Name { get; set; }
        public Dictionary<Node, Dictionary<int, object>> Value { get; set; }

        public Track()
        {
            Value = new Dictionary<Node, Dictionary<int, object>>();
        }

        public Track(string name)
        {
            Name = name;
            Value = new Dictionary<Node, Dictionary<int, object>>();
        }

        public Track(string name, Dictionary<Node, Dictionary<int, object>> value)
        {
            Name = name;
            Value = value;
        }

        public KeyValuePair<Node, Dictionary<int, object>>? GetNodeByName(string name)
        {
            foreach (var nodeEntry in Value)
            {
                if (nodeEntry.Key.Name == name)
                {
                    return nodeEntry;
                }
            }

            return null;
        }

        public bool NodeExists(string name)
        {
            foreach (var nodeEntry in Value)
            {
                if (nodeEntry.Key.Name == name)
                {
                    return true;
                }
            }

            return false;
        }
    }
}
