using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using StudioElevenLib.Level5.Binary.Logic;

namespace StudioElevenLibTest.TestClass
{
    public class NPCBase
    {
        public int ID { get; set; }
        public int ModelHead { get; set; }
        public int Type { get; set; } // NPCType
        public int Unk1 { get; set; }
        public int UniformNumber { get; set; }
        public int BootsNumber { get; set; }
        public int GloveNumber { get; set; }
        public int Icon { get; set; } // IconType
    }

    public class NPCPreset
    {
        public int NPCBaseID { get; set; }
        public int NPCAppearStartIndex { get; set; }
        public int NPCAppearCount { get; set; }
    }

    public class NPCAppear
    {
        public float LocationX { get; set; }
        public float LocationZ { get; set; }
        public float LocationY { get; set; }
        public int Unk1 { get; set; }
        public object Unk2 { get; set; }
        public float Rotation { get; set; }
        public string StandAnimation { get; set; }
        public int LookAtThePlayer { get; set; } // 2 = true, other = false
        public string TalkAnimation { get; set; }
        public string UnkAnimation { get; set; }
        public int Unk7 { get; set; }
        public string PhaseAppear { get; set; }
        public int Unk8 { get; set; }
    }

    public class NPCTalkInfo
    {
        public int NPCBaseID { get; set; }
        public int TalkConfigStartIndex { get; set; }
        public int TalkConfigCount { get; set; }
    }

    public class NPCTalkConfig
    {
        public int EventType { get; set; } // EventType
        public int EventValue { get; set; }
        public string EventCondition { get; set; }
        public int AutoTurn { get; set; } // 2 = true, other = false
    }
}
