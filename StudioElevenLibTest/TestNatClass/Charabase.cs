using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace StudioElevenLibTest.TestNatClass
{
    public class Charabase
    {
        public int BaseID { get; set; }

        public int NameID { get; set; }

        public int NicknameID { get; set; }

        public int DescriptionID { get; set; }

        public int Unk1 { get; set; }

        public short HeadID { get; set; }

        public byte Unk2 { get; set; }

        public byte Unk3 { get; set; }

        public byte Style { get; set; }

        public byte Unk4 { get; set; }

        public byte Size { get; set; } // First bit = body type, second byte = chara type

        public byte Identity { get; set; } // First bit = age, second byte = gender

        public int Unk5 { get; set; }
    }
}
