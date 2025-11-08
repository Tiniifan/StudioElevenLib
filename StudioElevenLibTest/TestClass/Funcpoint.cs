using System;
using System.Collections.Generic;
using System.Linq;
using System.Runtime.Remoting.Metadata.W3cXsd2001;
using System.Text;
using System.Text.RegularExpressions;
using System.Threading.Tasks;
using StudioElevenLib.Level5.Binary.Collections;

namespace StudioElevenLibTest.TestClass
{
    public interface IEventObject
    {

    }

    public class EventTrigger : IEventObject
    {
        public string PtreType { get; set; }

        public string EventType { get; set; }

        public int? EventID { get; set; }

        public bool? Stop { get; set; }

        public int? ItemID { get; set; }

        public int? GetBit { get; set; }

        public int? EV_T_BIT { get; set; }

        public int? EV_F_BIT { get; set; }

        public EventTrigger(PtreeNode ptreeNode)
        {
            InitializeFromNode(ptreeNode);
        }

        private void InitializeFromNode(PtreeNode ptreeNode)
        {
            if (ptreeNode == null) return;

            PtreeNode basePtreeNode = ptreeNode.FindByHeader("EVENT");

            if (basePtreeNode != null)
            {
                // Ptval children
                EV_T_BIT = basePtreeNode.GetValueFromChild<int?>("EV_T_BIT");
                EV_F_BIT = basePtreeNode.GetValueFromChild<int?>("EV_F_BIT");

                if (basePtreeNode.Children.Count > 0)
                {
                    PtreeNode evPtreeNode = basePtreeNode.FindByHeader("EV_TYPE");

                    if (evPtreeNode != null)
                    {
                        // Value
                        PtreType = evPtreeNode.GetValue<string>(0);
                        EventType = evPtreeNode.GetValue<string>(1);

                        // Ptval children
                        EventID = evPtreeNode.GetValueFromChild<int?>("ID");
                        Stop = evPtreeNode.GetValueFromChild<bool?>("Stop");
                        ItemID = evPtreeNode.GetValueFromChild<int?>("ItemID");
                        GetBit = evPtreeNode.GetValueFromChild<int?>("GetBit");
                    }
                }
            }
        }
    }

    public class EventSE : IEventObject
    {
        public string PtreType { get; set; }

        public int? SoundType { get; set; }

        public string SeName { get; set; } // nullable

        public int? Frame { get; set; }

        public bool? IsFade { get; set; }

        public int? FadeRadius { get; set; }

        public EventSE(PtreeNode ptreeNode)
        {
            InitializeFromNode(ptreeNode);
        }

        private void InitializeFromNode(PtreeNode ptreeNode)
        {
            if (ptreeNode == null) return;

            PtreeNode basePtreeNode = ptreeNode.FindByHeader("SE_POINT");

            if (basePtreeNode != null)
            {
                // Value
                PtreType = basePtreeNode.GetValue<string>(0);

                // Ptval children
                SoundType = basePtreeNode.GetValueFromChild<int?>("SoundType");
                SeName = basePtreeNode.GetValueFromChild<string>("SeName");
                Frame = basePtreeNode.GetValueFromChild<int?>("Frame");
                IsFade = basePtreeNode.GetValueFromChild<bool?>("IsFade");
                FadeRadius = basePtreeNode.GetValueFromChild<int?>("FadeRadius");
            }
        }
    }

    public class EventMapJump : IEventObject
    {
        public string PtreType { get; set; }

        public JumpTo JumpTo { get; set; }

        public Position3D STDPos { get; set; }

        public EventMapJump(PtreeNode ptreeNode)
        {
            InitializeFromNode(ptreeNode);
        }

        private void InitializeFromNode(PtreeNode ptreeNode)
        {
            if (ptreeNode == null) return;

            PtreeNode basePtreeNode = ptreeNode.FindByHeader("MAP_JUMP");

            if (basePtreeNode != null)
            {
                PtreeNode jumpToPtreeNode = ptreeNode.FindByHeader("JUMP_TO");

                if (jumpToPtreeNode != null)
                {
                    JumpTo = new JumpTo(jumpToPtreeNode);
                }

                PtreeNode stdPosPtreeNode = ptreeNode.FindByHeader("STD_POS");

                if (stdPosPtreeNode != null)
                {
                    if (stdPosPtreeNode.FindByHeader("POS") != null)
                    {
                        STDPos = new Position3D(stdPosPtreeNode, "POS");
                    }
                }
            }
        }
    }

    public class JumpTo
    {
        public string MapID { get; set; }

        public int? MoveRot { get; set; }

        public string MapMotion { get; set; } // nullable

        public int? FadeFrame { get; set; }

        public string SeName { get; set; } // nullable

        public int? SeFrame { get; set; }

        public JumpTo(PtreeNode ptreeNode)
        {
            InitializeFromNode(ptreeNode);
        }

        private void InitializeFromNode(PtreeNode ptreeNode)
        {
            if (ptreeNode == null) return;

            PtreeNode basePtreeNode = ptreeNode.FindByHeader("JUMP_TO");

            if (basePtreeNode != null)
            {
                // Ptval children
                MapID = basePtreeNode.GetValueFromChild<string>("MapID");
                MoveRot = basePtreeNode.GetValueFromChild<int?>("MoveRot");
                MapMotion = basePtreeNode.GetValueFromChild<string>("MapMotion");
                FadeFrame = basePtreeNode.GetValueFromChild<int?>("FadeFrame");
                SeName = basePtreeNode.GetValueFromChild<string>("SeName");
                SeFrame = basePtreeNode.GetValueFromChild<int?>("SeFrame");
            }
        }
    }

    public class EventDef
    {
        public string PtreType { get; set; }

        public string EventName { get; set; }

        public IEventObject EventObject { get; set; }

        public int? TBoxBitCheck { get; set; }

        public bool? BtnCheck { get; set; }

        public EventDef(PtreeNode ptreeNode)
        {
            InitializeFromNode(ptreeNode);
        }

        private void InitializeFromNode(PtreeNode ptreeNode)
        {
            if (ptreeNode == null) return;

            PtreeNode basePtreeNode = ptreeNode.FindByHeader("FP_DEF");

            if (basePtreeNode != null)
            {
                // Value
                PtreType = basePtreeNode.GetValue<string>(0);
                EventName = basePtreeNode.GetValue<string>(1);

                // Ptval children
                TBoxBitCheck = basePtreeNode.GetValueFromChild<int?>("TBoxBitCheck");
                BtnCheck = basePtreeNode.GetValueFromChild<bool?>("BtnCheck");

                if (EventName.StartsWith("KO") || EventName.StartsWith("EV"))
                {
                    PtreeNode childPtreeNode = ptreeNode.FindByHeader("EVENT");
                    EventObject = new EventTrigger(childPtreeNode);
                }
                else if (EventName.StartsWith("MS"))
                {
                    PtreeNode childPtreeNode = ptreeNode.FindByHeader("SE_POINT");
                    EventObject = new EventSE(childPtreeNode);
                }
                else if (EventName.StartsWith("MJ"))
                {
                    PtreeNode childPtreeNode = ptreeNode.FindByHeader("MAP_JUMP");
                    EventObject = new EventMapJump(childPtreeNode);
                }
            }
        }
    }

    public class Event
    {
        public string PtreType { get; set; }

        public string EventName { get; set; }

        public Position3D Position { get; set; }

        public IArea Area { get; set; }

        public EventDef EventDef { get; set; }

        public Event(PtreeNode ptreeNode)
        {
            InitializeFromNode(ptreeNode);
        }

        private void InitializeFromNode(PtreeNode ptreeNode)
        {
            if (ptreeNode == null) return;

            PtreeNode basePtreeNode = ptreeNode.FindByHeader("FP");

            if (basePtreeNode != null)
            {
                // Value
                PtreType = basePtreeNode.GetValue<string>(0);
                EventName = basePtreeNode.GetValue<string>(1);

                if (basePtreeNode.FindByHeader("POS") != null)
                {
                    Position = new Position3D(basePtreeNode, "POS");
                }

                if (basePtreeNode.FindByHeader("BOX") != null)
                {
                    Area = new BoxLimiter(basePtreeNode);
                }
                else if (basePtreeNode.FindByHeader("CIRCLE") != null)
                {
                    Area = new CircleLimiter(basePtreeNode);
                }

                if (basePtreeNode.FindByHeader("FP_DEF") != null)
                {
                    EventDef = new EventDef(basePtreeNode.FindByHeader("FP_DEF"));
                }
            }
        }
    }

    public class Funcpoint
    {
        public string FuncpointName { get; set; }

        public string MapID { get; set; }

        public List<Event> Events { get; set; }

        public Funcpoint(PtreeNode ptreeNode)
        {
            InitializeFromNode(ptreeNode);
        }

        private void InitializeFromNode(PtreeNode ptreeNode)
        {
            if (ptreeNode == null) return;

            PtreeNode basePtreeNode = ptreeNode.FindByHeader("FUNCPOINT");

            if (basePtreeNode != null)
            {
                // Value
                FuncpointName = basePtreeNode.GetValue<string>(0);
                MapID = basePtreeNode.GetValue<string>(1);

                Events = new List<Event>();

                for (int i = 0; i < basePtreeNode.Children.Count; i++)
                {
                    PtreeNode childrenPtreeNode = (PtreeNode)basePtreeNode.Children[i];
                    Event newEvent = new Event(childrenPtreeNode);
                    Events.Add(newEvent);
                }
            }
        }

    }
}
