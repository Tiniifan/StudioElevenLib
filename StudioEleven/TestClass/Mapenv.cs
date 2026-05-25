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
    public class MapenvXYZ
    {
        public float X { get; set; }

        public float Y { get; set; }

        public float Z { get; set; }

        public MapenvXYZ(PtreeNode ptreeNode, string nodeName)
        {
            InitializeFromNode(ptreeNode, nodeName);
        }

        private void InitializeFromNode(PtreeNode ptreeNode, string nodeName)
        {
            if (ptreeNode == null) return;

            X = ptreeNode.GetValueFromChildByHeader<float>(nodeName, 0);
            Y = ptreeNode.GetValueFromChildByHeader<float>(nodeName, 1);
            Z = ptreeNode.GetValueFromChildByHeader<float>(nodeName, 2);
        }

        public PtreeNode ToPtreeNode(string nodeName)
        {
            var ptvals = new Entry("PTVALS");
            ptvals.Variables.Add(new Variable(CfgValueType.Float, X));
            ptvals.Variables.Add(new Variable(CfgValueType.Float, Y));
            ptvals.Variables.Add(new Variable(CfgValueType.Float, Z));
            var ptvalsNode = new PtreeNode(ptvals);

            var entry = new Entry("PTREE");
            entry.Variables.Add(new Variable(CfgValueType.String, nodeName));

            var node = new PtreeNode(entry);
            node.AddChild(ptvalsNode);

            return node;
        }
    }

    public class MapenvRGB
    {
        public float R { get; set; }

        public float G { get; set; }

        public float B { get; set; }

        public MapenvRGB(PtreeNode ptreeNode, string nodeName)
        {
            InitializeFromNode(ptreeNode, nodeName);
        }

        private void InitializeFromNode(PtreeNode ptreeNode, string nodeName)
        {
            if (ptreeNode == null) return;

            R = ptreeNode.GetValueFromChildByHeader<float>(nodeName, 0);
            G = ptreeNode.GetValueFromChildByHeader<float>(nodeName, 1);
            B = ptreeNode.GetValueFromChildByHeader<float>(nodeName, 2);
        }

        public PtreeNode ToPtreeNode(string nodeName)
        {
            var ptvals = new Entry("PTVALS");
            ptvals.Variables.Add(new Variable(CfgValueType.Float, R));
            ptvals.Variables.Add(new Variable(CfgValueType.Float, G));
            ptvals.Variables.Add(new Variable(CfgValueType.Float, B));
            var ptvalsNode = new PtreeNode(ptvals);

            var entry = new Entry("PTREE");
            entry.Variables.Add(new Variable(CfgValueType.String, nodeName));

            var node = new PtreeNode(entry);
            node.AddChild(ptvalsNode);

            return node;
        }
    }

    public class MapenvShadowCol
    {
        public string ID { get; set; }

        public MapenvXYZ Collision { get; set; }

        public MapenvShadowCol(PtreeNode ptreeNode)
        {
            InitializeFromNode(ptreeNode);
        }

        private void InitializeFromNode(PtreeNode ptreeNode)
        {
            if (ptreeNode == null) return;

            ID = ptreeNode.GetValue<string>(0);
            Collision = new MapenvXYZ(ptreeNode, ID);
        }

        public PtreeNode ToPtreeNode()
        {
            return Collision.ToPtreeNode(ID);
        }
    }

    public class MMModelPos
    {
        public float MinX { get; set; }

        public float MinY { get; set; }

        public float MaxX { get; set; }

        public float MaxY { get; set; }

        public MMModelPos(PtreeNode ptreeNode)
        {
            InitializeFromNode(ptreeNode);
        }

        private void InitializeFromNode(PtreeNode ptreeNode)
        {
            if (ptreeNode == null) return;

            PtreeNode basePtreeNode = ptreeNode.FindByHeader("MMModelPos");

            if (basePtreeNode == null) return;

            for (int i = 0; i < 4; i++)
            {
                if (i < basePtreeNode.Children.Count())
                {
                    PtreeNode child = (PtreeNode)basePtreeNode.Children[i];
                    float value = child.GetValue<int>(0);

                    switch (i)
                    {
                        case 0: MinX = value; break;
                        case 1: MinY = value; break;
                        case 2: MaxX = value; break;
                        case 3: MaxY = value; break;
                    }
                }
            }
        }

        public PtreeNode ToPtreeNode()
        {
            var entry = new Entry("PTREE");
            entry.Variables.Add(new Variable(CfgValueType.String, "MMModelPos"));

            var node = new PtreeNode(entry);

            var values = new[] { MinX, MinY, MaxX, MaxY };
            for (int i = 0; i < values.Length; i++)
            {
                var childEntry = new Entry("PTVALS");
                childEntry.Variables.Add(new Variable(CfgValueType.Float, values[i]));
                node.AddChild(new PtreeNode(childEntry));
            }

            return node;
        }
    }

    public class MapenvCamera
    {
        public float? Fov { get; set; }

        public float? Dist { get; set; }

        public float? RotH { get; set; }

        public float? RotV { get; set; }

        public float? DptRngRate { get; set; }

        public float? DptLvl { get; set; }

        public MapenvCamera(PtreeNode ptreeNode)
        {
            InitializeFromNode(ptreeNode);
        }

        private void InitializeFromNode(PtreeNode ptreeNode)
        {
            if (ptreeNode == null) return;

            PtreeNode basePtreeNode = ptreeNode.FindByHeader("CAMERA");

            if (basePtreeNode == null) return;

            Fov = basePtreeNode.GetValueFromChildByHeader<float?>("Fov");
            Dist = basePtreeNode.GetValueFromChildByHeader<float?>("Dist");
            RotH = basePtreeNode.GetValueFromChildByHeader<float?>("RotH");
            RotV = basePtreeNode.GetValueFromChildByHeader<float?>("RotV");
            DptRngRate = basePtreeNode.GetValueFromChildByHeader<float?>("DptRngRate");
            DptLvl = basePtreeNode.GetValueFromChildByHeader<float?>("DptLvl");
        }

        public PtreeNode ToPtreeNode()
        {
            var entry = new Entry("PTREE");
            entry.Variables.Add(new Variable(CfgValueType.String, "CAMERA"));

            var node = new PtreeNode(entry);

            var properties = new Dictionary<string, float?>
            {
                { "Fov", Fov },
                { "Dist", Dist },
                { "RotH", RotH },
                { "RotV", RotV },
                { "DptRngRate", DptRngRate },
                { "DptLvl", DptLvl }
            };

            foreach (var prop in properties)
            {
                if (prop.Value != null)
                {
                    var childEntry = new Entry("PTREE");
                    childEntry.Variables.Add(new Variable(CfgValueType.String, prop.Key));

                    var childNode = new PtreeNode(childEntry);

                    var valueEntry = new Entry("PTVALS");
                    valueEntry.Variables.Add(new Variable(CfgValueType.Float, prop.Value));
                    childNode.AddChild(new PtreeNode(valueEntry));

                    node.AddChild(childNode);
                }
            }

            return node;
        }
    }

    public class MapenvLight
    {
        public string MapLight { get; set; }

        public MapenvRGB BackgroundColor { get; set; }

        public MapenvXYZ LightCollision { get; set; }

        public List<MapenvShadowCol> ShadowCols { get; set; }

        public MapenvLight(PtreeNode ptreeNode)
        {
            InitializeFromNode(ptreeNode);
        }

        private void InitializeFromNode(PtreeNode ptreeNode)
        {
            if (ptreeNode == null) return;

            PtreeNode basePtreeNode = ptreeNode.FindByHeader("LIGHT");

            if (basePtreeNode == null) return;

            MapLight = basePtreeNode.GetValueFromChildByHeader<string>("Fov");

            if (basePtreeNode.FindByHeader("BGColor") != null)
            {
                BackgroundColor = new MapenvRGB(basePtreeNode, "BGColor");
            }

            if (basePtreeNode.FindByHeader("LightCol") != null)
            {
                LightCollision = new MapenvXYZ(basePtreeNode, "LightCol");
            }

            PtreeNode shadowColPtreeNode = ptreeNode.FindByHeader("ShadowCol");
            if (shadowColPtreeNode != null)
            {
                ShadowCols = new List<MapenvShadowCol>();

                foreach (PtreeNode shadowColChildNode in shadowColPtreeNode.Children)
                {
                    ShadowCols.Add(new MapenvShadowCol(shadowColChildNode));
                }
            }
        }

        public PtreeNode ToPtreeNode()
        {
            var entry = new Entry("PTREE");
            entry.Variables.Add(new Variable(CfgValueType.String, "LIGHT"));

            var node = new PtreeNode(entry);

            if (BackgroundColor != null)
            {
                node.AddChild(BackgroundColor.ToPtreeNode("BGColor"));
            }

            if (LightCollision != null)
            {
                node.AddChild(LightCollision.ToPtreeNode("LightCol"));
            }

            if (ShadowCols != null && ShadowCols.Count > 0)
            {
                var shadowColEntry = new Entry("PTREE");
                shadowColEntry.Variables.Add(new Variable(CfgValueType.String, "ShadowCol"));

                var shadowColNode = new PtreeNode(shadowColEntry);

                foreach (var shadowCol in ShadowCols)
                {
                    shadowColNode.AddChild(shadowCol.ToPtreeNode());
                }

                node.AddChild(shadowColNode);
            }

            return node;
        }
    }

    public class Mapenv
    {
        public string MapenvName { get; set; }

        public string MapID { get; set; }

        public string Default { get; set; }

        public string ParentID { get; set; }

        public int? GroupID { get; set; }

        public string BtlMapID { get; set; }

        public string MapEffect { get; set; }

        public int? MMScroll { get; set; }

        public int? AntiMode { get; set; }

        public MMModelPos MMModelPos { get; set; }

        public MapenvCamera Camera { get; set; }

        public string SndBGM { get; set; }

        public string SndEnv { get; set; }

        public MapenvLight Light { get; set; }

        public Mapenv (PtreeNode ptreeNode)
        {
            InitializeFromNode(ptreeNode);
        }

        private void InitializeFromNode(PtreeNode ptreeNode)
        {
            if (ptreeNode == null) return;

            PtreeNode basePtreeNode = ptreeNode.FindByHeader("MAP_ENV");

            if (basePtreeNode != null)
            {
                // Value
                MapenvName = basePtreeNode.GetValue<string>(0);
                MapID = basePtreeNode.GetValue<string>(1);

                // Ptval children
                Default = basePtreeNode.GetValueFromChild<string>("Default");
                ParentID = basePtreeNode.GetValueFromChild<string>("ParentID");
                GroupID = basePtreeNode.GetValueFromChild<int?>("GroupID");
                BtlMapID = basePtreeNode.GetValueFromChild<string>("BtlMapID");
                MapEffect = basePtreeNode.GetValueFromChild<string>("MapEffect");
                MMScroll = basePtreeNode.GetValueFromChild<int?>("MMScroll");
                AntiMode = basePtreeNode.GetValueFromChild<int?>("AntiMode");
                SndBGM = basePtreeNode.GetValueFromChild<string>("SndBGM");
                SndEnv = basePtreeNode.GetValueFromChild<string>("SndEnv");

                // Ptree children
                MMModelPos = new MMModelPos(ptreeNode.FindByHeader("MMModelPos"));
                Camera = new MapenvCamera(ptreeNode.FindByHeader("CAMERA"));
                Light = new MapenvLight(ptreeNode.FindByHeader("LIGHT"));
            }
        }

        public PtreeNode ToPtreeNode()
        {
            var entry = new Entry("PTREE");
            entry.Variables.Add(new Variable(CfgValueType.String, MapenvName));
            entry.Variables.Add(new Variable(CfgValueType.String, MapID));

            var node = new PtreeNode(entry);

            var simpleProperties = new Dictionary<string, object>
            {
                { "Default", Default },
                { "ParentID", ParentID },
                { "GroupID", GroupID },
                { "BtlMapID", BtlMapID },
                { "MapEffect", MapEffect },
                { "MMScroll", MMScroll },
                { "AntiMode", AntiMode },
                { "SndBGM", SndBGM },
                { "SndEnv", SndEnv }
            };

            foreach (var prop in simpleProperties)
            {
                if (prop.Value != null)
                {
                    var childEntry = new Entry("PTVAL");

                    var valueType = prop.Value is int ? CfgValueType.Int : CfgValueType.String;
                    childEntry.Variables.Add(new Variable(valueType, prop.Value));

                    childEntry.Variables.Add(new Variable(CfgValueType.String, prop.Key));

                    node.AddChild(new PtreeNode(childEntry));
                }
            }

            if (MMModelPos != null)
                node.AddChild(MMModelPos.ToPtreeNode());

            if (Camera != null)
                node.AddChild(Camera.ToPtreeNode());

            if (Light != null)
                node.AddChild(Light.ToPtreeNode());

            return node;
        }
    }
}
