using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace StudioElevenLib.Level5.Animation.Logic
{
    public class Node
    {
        public string Name { get; set; }

        // True when the game interpolates the keys, false when it holds them (V2 node mode, V1 group 3)
        public bool IsInMainTrack { get; set; }

        public List<Frame> Frames { get; set; }

        public Node(string name)
        {
            Name = name;
            Frames = new List<Frame>();
        }

        public Node(string name, bool isInMainTrack)
        {
            Name = name;
            IsInMainTrack = isInMainTrack;
            Frames = new List<Frame>();
        }

        public Node(string name, bool isInMainTrack, List<Frame> frames)
        {
            Name = name;
            IsInMainTrack = isInMainTrack;
            Frames = frames;
        }
    }
}
