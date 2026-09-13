using System;
using System.IO;
using System.Collections.Generic;
using StudioElevenLib.Level5.Animation.Logic;

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
            throw new NotImplementedException("Animation V3 is not supported yet.");
        }

        public byte[] Save()
        {
            throw new NotImplementedException("Animation V3 is not supported yet.");
        }
    }
}
