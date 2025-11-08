using System;
using System.Collections.Generic;
using System.Linq;
using System.Runtime.Remoting.Metadata.W3cXsd2001;
using System.Text;
using System.Text.RegularExpressions;
using System.Threading.Tasks;
using StudioElevenLib.Level5.Binary.Collections;
using StudioElevenLib.Level5.Binary.Logic;

namespace StudioElevenLibTest.TestClass
{
    public class HealArea
    {
        public string PtreType { get; set; }

        public string HealAreaName { get; set; }

        public Position3D Position { get; set; }

        public HealArea(PtreeNode ptreeNode)
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
                HealAreaName = basePtreeNode.GetValue<string>(1);

                if (basePtreeNode.FindByHeader("POS") != null)
                {
                    Position = new Position3D(basePtreeNode, "POS");
                }
            }
        }

        public PtreeNode ToPtreeNode()
        {
            var entry = new Entry("PTREE");
            entry.Variables.Add(new Variable(CfgValueType.String, "FP"));
            entry.Variables.Add(new Variable(CfgValueType.String, HealAreaName));

            var node = new PtreeNode(entry);

            if (Position != null)
                node.AddChild(Position.ToPtreeNode("POS"));

            return node;
        }
    }

    public class Healpoint
    {
        public string HealpointName { get; set; }

        public string MapID { get; set; }

        public List<HealArea> HealAreas;

        public Healpoint(PtreeNode ptreeNode)
        {
            InitializeFromNode(ptreeNode);
        }

        private void InitializeFromNode(PtreeNode ptreeNode)
        {
            if (ptreeNode == null) return;

            PtreeNode basePtreeNode = ptreeNode.FindByHeader("HEALPOINT");

            if (basePtreeNode != null)
            {
                // Value
                HealpointName = basePtreeNode.GetValue<string>(0);
                MapID = basePtreeNode.GetValue<string>(1);

                HealAreas = new List<HealArea>();

                foreach (PtreeNode childPtreeNode in ptreeNode.Children)
                {
                    if (childPtreeNode.Header == "FP")
                    {
                        HealAreas.Add(new HealArea(childPtreeNode));
                    }
                }
            }
        }

        public PtreeNode ToPtreeNode()
        {
            var entry = new Entry("PTREE");
            entry.Variables.Add(new Variable(CfgValueType.String, "HEALPOINT"));
            entry.Variables.Add(new Variable(CfgValueType.String, MapID));

            var node = new PtreeNode(entry);

            if (HealAreas != null && HealAreas.Count > 0)
            {
                foreach (var healArea in HealAreas)
                {
                    node.AddChild(healArea.ToPtreeNode());
                }
            }

            return node;
        }
    }
}
