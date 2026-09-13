using System.Collections.Generic;
using StudioElevenLib.Level5.Animation.Logic;

namespace StudioElevenLib.Level5.Animation
{
    public interface IAnimationManager
    {
        string Format { get; set; }

        string Version { get; }

        string AnimationName { get; set; }

        int FrameCount { get; set; }

        List<Track> Tracks { get; set; }

        byte[] Save();
    }
}
