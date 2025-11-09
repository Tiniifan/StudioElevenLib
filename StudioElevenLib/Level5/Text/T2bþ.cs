using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Text;
using System.Text.RegularExpressions;
using System.Xml;
using StudioElevenLib.Level5.Binary.Logic;
using StudioElevenLib.Level5.Binary;
using StudioElevenLib.Level5.Binary.Collections;
using StudioElevenLib.Level5.Text.Logic;

namespace StudioElevenLib.Level5.Text
{
    public class T2bþ : CfgBin<CfgTreeNode>
    {
        public Dictionary<int, TextConfig> Texts { get; set; }
        public Dictionary<int, TextConfig> Nouns { get; set; }

        public T2bþ()
        {
            Texts = new Dictionary<int, TextConfig>();
            Nouns = new Dictionary<int, TextConfig>();
        }

        public T2bþ(Stream stream)
        {
            Texts = new Dictionary<int, TextConfig>();
            Nouns = new Dictionary<int, TextConfig>();
            Open(stream);
            LoadBinary();
        }

        public T2bþ(byte[] data)
        {
            Texts = new Dictionary<int, TextConfig>();
            Nouns = new Dictionary<int, TextConfig>();
            Open(data);
            LoadBinary();
        }

        private void LoadBinary()
        {
            // Get faces
            var washaBeginsNodes = Entries.FindNodes(node => node.Name == "TEXT_WASHA");
            int[] faces = washaBeginsNodes
                .SelectMany(node => node.Item.Variables.Skip(1).Where(v => v.Type == CfgValueType.Int))
                .Select(v => Convert.ToInt32(v.Value))
                .ToArray();

            // Get faces configs
            var configBeginNodes = Entries.FindNodes(node => node.Name == "TEXT_CONFIG");
            Dictionary<int, int> facesConfig = new Dictionary<int, int>();

            foreach (var configNode in configBeginNodes)
            {
                if (configNode.Item.Variables.Count >= 3)
                {
                    int key = Convert.ToInt32(configNode.Item.Variables[0].Value);
                    int value = Convert.ToInt32(configNode.Item.Variables[2].Value);
                    facesConfig[key] = value;
                }
            }

            // Get Texts
            var textInfoNodes = Entries.FindNodes(node => node.Name == "TEXT_INFO");

            Texts = textInfoNodes
                .GroupBy(
                    x => Convert.ToInt32(x.Item.Variables[0].Value),
                    y =>
                    {
                        int variable0Value = Convert.ToInt32(y.Item.Variables[0].Value);
                        int washaID = -1;
                        List<StringLevel5> strings = new List<StringLevel5>();

                        if (facesConfig.ContainsKey(variable0Value))
                        {
                            int configValue = facesConfig[variable0Value];
                            if (configValue != -1 && configValue < faces.Length)
                            {
                                washaID = faces[configValue];
                            }
                        }

                        string text = y.Item.Variables[2].Value as string;
                        if (text != null)
                        {
                            strings.Add(new StringLevel5(
                                Convert.ToInt32(y.Item.Variables[1].Value),
                                text
                            ));
                        }

                        return new TextConfig(strings, washaID);
                    }
                )
                .ToDictionary(
                    group => group.Key,
                    group =>
                    {
                        var mergedStrings = group.SelectMany(item => item.Strings).ToList();
                        return new TextConfig(mergedStrings, group.First().WashaID);
                    }
                );

            // Get Nouns
            var nounInfoNodes = Entries.FindNodes(node => node.Name == "NOUN_INFO");

            Nouns = nounInfoNodes
                .GroupBy(
                    x => Convert.ToInt32(x.Item.Variables[0].Value),
                    y =>
                    {
                        int washaID = -1;
                        List<StringLevel5> strings = new List<StringLevel5>();

                        if (y.Item.Variables.Count > 5)
                        {
                            string text = y.Item.Variables[5].Value as string;
                            if (text != null)
                            {
                                strings.Add(new StringLevel5(
                                    Convert.ToInt32(y.Item.Variables[1].Value),
                                    text
                                ));
                            }
                        }

                        return new TextConfig(strings, washaID);
                    }
                )
                .ToDictionary(
                    group => group.Key,
                    group =>
                    {
                        var mergedStrings = group.SelectMany(item => item.Strings).ToList();
                        return new TextConfig(mergedStrings, group.First().WashaID);
                    }
                );
        }

        public T2bþ(string xmlData) : base()
        {
            Texts = new Dictionary<int, TextConfig>();
            Nouns = new Dictionary<int, TextConfig>();

            XmlDocument xmlDoc = new XmlDocument();
            xmlDoc.LoadXml(xmlData);

            XmlNodeList textConfigNodes = xmlDoc.SelectNodes("Root/*/TextConfig");

            foreach (XmlNode textNode in textConfigNodes)
            {
                int crc32 = int.Parse(textNode.Attributes.GetNamedItem("crc32").Value.Replace("0x", ""), System.Globalization.NumberStyles.HexNumber);
                int washa = int.Parse(textNode.Attributes.GetNamedItem("washa").Value.Replace("0x", ""), System.Globalization.NumberStyles.HexNumber);

                XmlNodeList stringNodes = textNode.SelectNodes("String");
                List<StringLevel5> strings = new List<StringLevel5>();

                for (int i = 0; i < stringNodes.Count; i++)
                {
                    strings.Add(new StringLevel5(i, stringNodes[i].Attributes.GetNamedItem("value").Value));
                }

                if (textNode.ParentNode.Name == "Texts")
                {
                    Texts[crc32] = new TextConfig(strings, washa);
                }
                else if (textNode.ParentNode.Name == "Nouns")
                {
                    Nouns[crc32] = new TextConfig(strings, -1);
                }
            }
        }

        public T2bþ(string[] lines)
        {
            Texts = new Dictionary<int, TextConfig>();
            Nouns = new Dictionary<int, TextConfig>();

            int currentIndex = 0;
            TextConfig currentTextConfig = null;

            foreach (string line in lines)
            {
                Match match = GetMatch(line);

                if (match != null)
                {
                    string type = match.Groups[1].Value;
                    int crc32 = int.Parse(match.Groups[2].Value, System.Globalization.NumberStyles.HexNumber);
                    int washa = -1;
                    if (match.Groups[3].Value != "-1")
                    {
                        washa = int.Parse(match.Groups[3].Value, System.Globalization.NumberStyles.HexNumber);
                    }

                    currentTextConfig = new TextConfig(new List<StringLevel5>(), washa);

                    if (type.Trim().Equals("Texts"))
                    {
                        Texts[crc32] = currentTextConfig;
                    }
                    else if (type.Trim().Equals("Nouns"))
                    {
                        Nouns[crc32] = currentTextConfig;
                    }
                }
                else if (!string.IsNullOrWhiteSpace(line))
                {
                    if (currentTextConfig != null)
                    {
                        currentTextConfig.Strings.Add(new StringLevel5(currentTextConfig.Strings.Count, line));
                    }
                    else
                    {
                        Texts.Add(currentIndex, new TextConfig(new List<StringLevel5>() { new StringLevel5(0, line) }, -1));
                        currentTextConfig = null;
                    }
                }

                currentIndex++;
            }
        }

        private Match GetMatch(string line)
        {
            if (Regex.IsMatch(line, @"\[(\w+)/0x([A-Fa-f0-9]+)/0x([A-Fa-f0-9]+)\]"))
            {
                return Regex.Match(line, @"\[(\w+)/0x([A-Fa-f0-9]+)/0x([A-Fa-f0-9]+)\]");
            }
            else if (Regex.IsMatch(line, @"\[(\w+)/0x([A-Fa-f0-9]+)/(-1)\]"))
            {
                return Regex.Match(line, @"\[(\w+)/0x([A-Fa-f0-9]+)/(-1)\]");
            }
            else
            {
                return null;
            }
        }

        private string[] GetStrings()
        {
            string[] allTexts = Texts.Values
                .SelectMany(textList => textList.Strings)
                .Select(textValue => textValue.Text)
                .Distinct()
                .ToArray();

            string[] allNouns = Nouns.Values
                .SelectMany(textList => textList.Strings)
                .Select(textValue => textValue.Text)
                .Distinct()
                .ToArray();

            return allTexts.Union(allNouns).ToArray();
        }

        private CfgTreeNode GetTextEntry()
        {
            var textBeginEntry = new Entry("TEXT_INFO_BEGIN", new List<Variable>()
            {
                new Variable(CfgValueType.Int, Texts.Values.Sum(textList => textList.Strings.Count))
            });
            var textBeginNode = new CfgTreeNode(textBeginEntry, 1);

            foreach (KeyValuePair<int, TextConfig> textItem in Texts)
            {
                for (int i = 0; i < textItem.Value.Strings.Count; i++)
                {
                    StringLevel5 textValue = textItem.Value.Strings[i];

                    Entry textItemEntry = new Entry("TEXT_INFO_BEGIN", new List<Variable>()
                    {
                        new Variable(CfgValueType.Int, textItem.Key),
                        new Variable(CfgValueType.Int, i),
                        new Variable(CfgValueType.String, textValue.Text),
                        new Variable(CfgValueType.Int, 0),
                    });

                    textBeginNode.AddChild(new CfgTreeNode(textItemEntry, 2));
                }
            }

            return textBeginNode;
        }

        private CfgTreeNode GetTextConfigEntry()
        {
            int index = 0;
            List<int> washas = Texts.Where(x => x.Value.WashaID != -1).Select(x => x.Value.WashaID).ToList();

            var textConfigEntry = new Entry("TEXT_CONFIG_BEGIN", new List<Variable>()
            {
                new Variable(CfgValueType.Int, Texts.Count)
            });
            var textConfigNode = new CfgTreeNode(textConfigEntry, 1);

            foreach (KeyValuePair<int, TextConfig> textItem in Texts)
            {
                Entry textConfigItemEntry = new Entry("TEXT_CONFIG_BEGIN", new List<Variable>()
                {
                    new Variable(CfgValueType.Int, textItem.Key),
                    new Variable(CfgValueType.Int, textItem.Value.Strings.Count),
                    new Variable(CfgValueType.Int, washas.IndexOf(textItem.Value.WashaID)),
                });

                textConfigNode.AddChild(new CfgTreeNode(textConfigItemEntry, 2));
                index++;
            }

            return textConfigNode;
        }

        private CfgTreeNode GetTextWashaEntry()
        {
            int[] washas = Texts.Where(x => x.Value.WashaID != -1).Select(x => x.Value.WashaID).ToArray();

            var textWashaEntry = new Entry("TEXT_WASHA_BEGIN", new List<Variable>()
            {
                new Variable(CfgValueType.Int, washas.Length)
            });
            var textWashaNode = new CfgTreeNode(textWashaEntry, 1);

            for (int i = 0; i < washas.Length; i++)
            {
                Entry textWashaItem = new Entry("TEXT_WASHA_BEGIN", new List<Variable>()
                {
                    new Variable(CfgValueType.Int, i),
                    new Variable(CfgValueType.Int, washas[i]),
                });

                textWashaNode.AddChild(new CfgTreeNode(textWashaItem, 2));
            }

            return textWashaNode;
        }

        private CfgTreeNode GetNounEntry()
        {
            var nounEntry = new Entry("NOUN_INFO_BEGIN", new List<Variable>()
            {
                new Variable(CfgValueType.Int, Nouns.Values.Sum(textList => textList.Strings.Count))
            });
            var nounNode = new CfgTreeNode(nounEntry, 1);

            foreach (KeyValuePair<int, TextConfig> nounItem in Nouns)
            {
                for (int i = 0; i < nounItem.Value.Strings.Count; i++)
                {
                    StringLevel5 textValue = nounItem.Value.Strings[i];

                    Entry textItemEntry = new Entry("NOUN_INFO_BEGIN", new List<Variable>()
                    {
                        new Variable(CfgValueType.Int, nounItem.Key),
                        new Variable(CfgValueType.Int, i),
                        new Variable(CfgValueType.String, null),
                        new Variable(CfgValueType.String, null),
                        new Variable(CfgValueType.String, null),
                        new Variable(CfgValueType.String, textValue.Text),
                        new Variable(CfgValueType.String, null),
                        new Variable(CfgValueType.String, null),
                        new Variable(CfgValueType.String, null),
                        new Variable(CfgValueType.String, null),
                        new Variable(CfgValueType.Int, 0),
                        new Variable(CfgValueType.Int, 0),
                        new Variable(CfgValueType.Int, 0),
                        new Variable(CfgValueType.Int, 0),
                    });

                    nounNode.AddChild(new CfgTreeNode(textItemEntry, 2));
                }
            }

            return nounNode;
        }

        public void Save(string fileName, bool iego)
        {
            if (Texts.Count > 0)
            {
                var textNode = GetTextEntry();

                // Supprimer l'ancien noeud TEXT_INFO_BEGIN s'il existe
                if (Entries.Exists("TEXT_INFO_BEGIN"))
                {
                    Entries.Delete("TEXT_INFO_BEGIN");
                }
                Entries.AddChild(textNode);

                if (iego)
                {
                    var configNode = GetTextConfigEntry();
                    var washaNode = GetTextWashaEntry();

                    if (Entries.Exists("TEXT_CONFIG_BEGIN"))
                    {
                        Entries.Delete("TEXT_CONFIG_BEGIN");
                    }
                    Entries.AddChild(configNode);

                    if (Entries.Exists("TEXT_WASHA_BEGIN"))
                    {
                        Entries.Delete("TEXT_WASHA_BEGIN");
                    }
                    Entries.AddChild(washaNode);
                }
            }

            if (Nouns.Count > 0)
            {
                var nounNode = GetNounEntry();

                if (Entries.Exists("NOUN_INFO_BEGIN"))
                {
                    Entries.Delete("NOUN_INFO_BEGIN");
                }
                Entries.AddChild(nounNode);
            }

            Save(fileName);
        }

        public new byte[] Save()
        {
            if (Texts.Count > 0)
            {
                var textNode = GetTextEntry();

                if (Entries.Exists("TEXT_INFO_BEGIN"))
                {
                    Entries.Delete("TEXT_INFO_BEGIN");
                }
                Entries.AddChild(textNode);
            }

            if (Nouns.Count > 0)
            {
                var nounNode = GetNounEntry();

                if (Entries.Exists("NOUN_INFO_BEGIN"))
                {
                    Entries.Delete("NOUN_INFO_BEGIN");
                }
                Entries.AddChild(nounNode);
            }

            return base.Save();
        }

        public string[] ConvertToXml(Dictionary<int, TextConfig> texts, string baliseName)
        {
            List<string> xmlStrings = new List<string>();
            StringBuilder xmlBuilder = new StringBuilder();
            xmlBuilder.AppendLine("<" + baliseName + ">");

            foreach (var kvp in texts)
            {
                int crc32 = kvp.Key;
                string washa = "0x" + kvp.Value.WashaID.ToString("X8");

                xmlBuilder.AppendLine($" <TextConfig crc32=\"0x{crc32.ToString("X8")}\" washa=\"{washa}\">");

                foreach (var stringLevel5 in kvp.Value.Strings)
                {
                    xmlBuilder.AppendLine($"  <String value=\"{stringLevel5.Text.Replace("<", "&lt;").Replace(">", "&gt;").Replace("\"", "&quot;")}\" />");
                }

                xmlBuilder.AppendLine(" </TextConfig>");
            }

            xmlBuilder.AppendLine("</" + baliseName + ">");
            xmlStrings.Add(xmlBuilder.ToString());

            return xmlStrings.ToArray();
        }

        public string[] ExportToXML()
        {
            List<string> xmlStrings = new List<string>();

            xmlStrings.Add("<?xml version=\"1.0\"?>");
            xmlStrings.Add("<Root>");

            if (Texts.Count > 0)
            {
                xmlStrings.AddRange(ConvertToXml(Texts, "Texts"));
            }

            if (Nouns.Count > 0)
            {
                xmlStrings.AddRange(ConvertToXml(Nouns, "Nouns"));
            }

            xmlStrings.Add("</Root>");

            return xmlStrings.ToArray();
        }

        public string[] ExportToTxt()
        {
            List<string> txtStrings = new List<string>();

            foreach (var kvp in Texts)
            {
                int crc32 = kvp.Key;
                string washa = "0x" + kvp.Value.WashaID.ToString("X8");

                StringBuilder textBuilder = new StringBuilder();
                textBuilder.AppendFormat("[Texts/0x{0:X8}/{1}] {2}", crc32, washa, Environment.NewLine);

                foreach (var stringLevel5 in kvp.Value.Strings)
                {
                    textBuilder.AppendLine(stringLevel5.Text);
                }

                txtStrings.Add(textBuilder.ToString());
            }

            foreach (var kvp in Nouns)
            {
                int crc32 = kvp.Key;

                StringBuilder textBuilder = new StringBuilder();
                textBuilder.AppendFormat("[Nouns/0x{0:X8}/-1] {1}", crc32, Environment.NewLine);

                foreach (var stringLevel5 in kvp.Value.Strings)
                {
                    textBuilder.AppendLine(stringLevel5.Text);
                }

                txtStrings.Add(textBuilder.ToString());
            }

            return txtStrings.ToArray();
        }
    }
}