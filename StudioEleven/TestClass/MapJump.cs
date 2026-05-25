using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using StudioElevenLib.Level5.Binary.Logic;

namespace StudioElevenLibTest.TestClass
{
    public class MapJumpEventSTDPos
    {
        public float[] Pos { get; set; } = new float[4];
    }

    public class MapJumpEventJumpTo
    {
        public string MapID { get; set; }

        public string MapMotion { get; set; }

        public int FadeFrame { get; set; }

        public string SeName { get; set; }

        public int SeFrame { get; set; }
    }

    public class MapJumpEvent
    {
        public MapJumpEventJumpTo MapJumpEventJumpTo { get; set; }

        public MapJumpEventSTDPos MapJumpEventSTDPos { get; set; }
    }

    public class MapJumpBtnCheck
    {
        public bool Enable { get; set; }
        public string BtnCheckText { get; set; } = "BtnCheck";
    }

    public class MapJumpTBoxBitCheck
    {
        public int Value { get; set; }
        public string TBoxBitCheckText { get; set; } = "TBoxBitCheck";
    }

    public class MapJumpGMGuide
    {
        public int Value { get; set; }
        public string GMGuideText { get; set; } = "GMGuide";
    }

    public class MapJumpDefinition
    {
        public string FuncName { get; set; }

        public string ID { get; set; }

        public MapJumpGMGuide GMGuide { get; set; }

        public MapJumpTBoxBitCheck TBoxBitCheck { get; set; }

        public MapJumpBtnCheck BtnCheck { get; set; }

        public MapJumpEvent Event { get; set; }
    }

    public class MapJump
    {
        public string FuncName { get; set; }

        public string ID { get; set; }

        public float[] Pos { get; set; } = new float[4];

        //public AreaLimiter Area { get; set; }

        public MapJumpDefinition Definition { get; set; }
    }
}
