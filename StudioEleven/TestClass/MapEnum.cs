using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace StudioElevenLibTest.TestClass
{
    public enum NPCType
    {
        Unknown = 0,
        Player = 1,
        Unused = 2,
        NPC = 3,
        NPCOther = 4
    }

    public enum IconType
    {
        Point = 0,
        PalpackShop = 1,
        ConsumableShop = 2,
        EquipmentShop = 3,
        SpecialMoveShop = 4,
        CompetitionRoute = 5,
        FightingSpirit = 6,
        PlayCoin = 7,
        None = 8
    }

    public enum EventType
    {
        None = 0,
        Text = 1,
        UnkScript = 2,
        Script = 3,
        Talk1 = 4,
        Talk2 = 5,
        UnkEvent = 6,
        CompetitiveRoute = 7,
        Shop = 8
    }

    public enum TBoxType
    {
        Normal = 0,
        Gold = 1,
        Silver = 2
    }
}
