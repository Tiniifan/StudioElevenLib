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

        public bool IsInMainTrack { get; set; }

        public Node(string name)
        {
            Name = name;
        }

        public Node(string name, bool isInMainTrack)
        {
            Name = name;
            IsInMainTrack = isInMainTrack;
        }
    }
}
