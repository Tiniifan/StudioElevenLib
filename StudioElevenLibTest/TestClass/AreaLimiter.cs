using StudioElevenLib.Level5.Binary.Collections;
using StudioElevenLib.Level5.Binary.Logic;

namespace StudioElevenLibTest.TestClass
{
    public interface IArea
    {
        PtreeNode ToPtreeNode();
    }

    public class BoxLimiter : IArea
    {
        public float Width { get; set; }
        public float Height { get; set; }
        public float Angle { get; set; }

        public BoxLimiter(PtreeNode ptreeNode)
        {
            InitializeFromNode(ptreeNode);
        }

        private void InitializeFromNode(PtreeNode ptreeNode)
        {
            if (ptreeNode == null) return;

            Width = ptreeNode.GetValueFromChildByHeader<float>("BOX", 0);
            Height = ptreeNode.GetValueFromChildByHeader<float>("BOX", 1);
            Angle = ptreeNode.GetValueFromChildByHeader<float>("BOX", 2);
        }

        public PtreeNode ToPtreeNode()
        {
            var ptvals = new Entry("PTVALS");

            ptvals.Variables.Add(new Variable(CfgValueType.Float, Width));
            ptvals.Variables.Add(new Variable(CfgValueType.Float, Height));
            ptvals.Variables.Add(new Variable(CfgValueType.Float, Angle));

            var ptvalsNode = new PtreeNode(ptvals);

            var entry = new Entry("PTREE");
            entry.Variables.Add(new Variable(CfgValueType.String, "BOX"));

            var node = new PtreeNode(entry);
            node.AddChild(ptvalsNode);

            return node;
        }
    }

    public class CircleLimiter : IArea
    {
        public float Radius { get; set; }
        public float Angle { get; set; }

        public CircleLimiter(PtreeNode ptreeNode)
        {
            InitializeFromNode(ptreeNode);
        }

        private void InitializeFromNode(PtreeNode ptreeNode)
        {
            if (ptreeNode == null) return;

            Radius = ptreeNode.GetValueFromChildByHeader<float>("CIRCLE", 0);
            Angle = ptreeNode.GetValueFromChildByHeader<float>("CIRCLE", 1);
        }

        public PtreeNode ToPtreeNode()
        {
            var ptvals = new Entry("PTVALS");

            ptvals.Variables.Add(new Variable(CfgValueType.Float, Radius));
            ptvals.Variables.Add(new Variable(CfgValueType.Float, Angle));

            var ptvalsNode = new PtreeNode(ptvals);

            var entry = new Entry("PTREE");
            entry.Variables.Add(new Variable(CfgValueType.String, "CIRCLE"));

            var node = new PtreeNode(entry);
            node.AddChild(ptvalsNode);

            return node;
        }
    }
}
