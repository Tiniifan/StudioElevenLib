using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace StudioElevenLibTest.TestNatClass
{
    public class Charaparam
    {
        public int ParamID { get; set; }

        public int BaseID { get; set; }

        public short SkillOffset { get; set; }

        public byte Unk1 { get; set; }

        public byte Invoke { get; set; }

        public byte BaseFP { get; set; }

        public byte BaseTP { get; set; }

        public byte BaseKick { get; set; }

        public byte BaseDribble { get; set; }

        public byte BaseTechnique { get; set; }

        public byte BaseBlock { get; set; }

        public byte BaseSpeed { get; set; }

        public byte BaseStamina { get; set; }

        public byte BaseCatch { get; set; }

        public byte BaseLuck { get; set; }

        public byte Element { get; set; }

        public byte Position { get; set; }

        public byte GrownFP { get; set; }

        public byte GrownTP { get; set; }

        public byte GrownKick { get; set; }

        public byte GrownDribble { get; set; }

        public byte GrownTechnique { get; set; }

        public byte GrownBlock { get; set; }

        public byte GrownSpeed { get; set; }

        public byte GrownStamina { get; set; }

        public byte GrownCatch { get; set; }

        public short AvatarOffset { get; set; }

        public byte ExperienceBar { get; set; }

        public byte SkillCount { get; set; } // >> 0x04

        public short Freedom { get; set; }
    }
}
