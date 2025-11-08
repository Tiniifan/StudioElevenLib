using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using StudioElevenLib.Level5.Binary.Collections;
using StudioElevenLib.Level5.Binary.Logic;

namespace StudioElevenLibTest.TestClass
{
    public class Position3D
    {
        public float X { get; set; }

        public float Y { get; set; }

        public float Z { get; set; }

        public float? W { get; set; }

        public Position3D(PtreeNode ptreeNode, string nodeName)
        {
            InitializeFromNode(ptreeNode, nodeName);
        }

        private void InitializeFromNode(PtreeNode ptreeNode, string nodeName)
        {
            if (ptreeNode == null) return;

            X = ptreeNode.GetValueFromChildByHeader<float>(nodeName, 0);
            Z = ptreeNode.GetValueFromChildByHeader<float>(nodeName, 1);
            Y = ptreeNode.GetValueFromChildByHeader<float>(nodeName, 2);
            W = ptreeNode.GetValueFromChildByHeader<float?>(nodeName, 3);
        }

        public PtreeNode ToPtreeNode(string nodeName)
        {
            var ptvals = new Entry("PTVALS");

            ptvals.Variables.Add(new Variable(CfgValueType.Float, X));
            ptvals.Variables.Add(new Variable(CfgValueType.Float, Z));
            ptvals.Variables.Add(new Variable(CfgValueType.Float, Y));

            if (W != null)
            {
                ptvals.Variables.Add(new Variable(CfgValueType.Float, W));
            }

            var ptvalsNode = new PtreeNode(ptvals);

            var entry = new Entry("PTREE");
            entry.Variables.Add(new Variable(CfgValueType.String, nodeName));

            var node = new PtreeNode(entry);
            node.AddChild(ptvalsNode);

            return node;
        }
    }
}
