using System;
using System.IO;
using System.Xml;
using System.Linq;
using System.Text;
using System.Text.RegularExpressions;
using System.Collections.Generic;
using StudioElevenLib.Level5.Binary;
using StudioElevenLib.Level5.Binary.Logic;
using StudioElevenLib.Level5.Text.Logic;
using StudioElevenLib.Level5.Binary.Collections;
using StudioElevenLib.Tools;

namespace StudioElevenLib.Level5.Text
{
    public class T2bþ : CfgBin<CfgTreeNode>
    {
        public Dictionary<int, TextConfig> Texts { get; set; }
        public Dictionary<int, TextConfig> Nouns { get; set; }
        public Dictionary<int, TextConfig> TextsDebug { get; set; }
        public Dictionary<int, string> Keys { get; set; }

        public T2bþ()
        {
            Texts = new Dictionary<int, TextConfig>();
            Nouns = new Dictionary<int, TextConfig>();
            TextsDebug = new Dictionary<int, TextConfig>();
            Keys = new Dictionary<int, string>();
        }

        public T2bþ(Stream stream)
        {
            Texts = new Dictionary<int, TextConfig>();
            Nouns = new Dictionary<int, TextConfig>();
            TextsDebug = new Dictionary<int, TextConfig>();
            Keys = new Dictionary<int, string>();
            Open(stream);
            LoadBinary();
        }

        public T2bþ(byte[] data)
        {
            Texts = new Dictionary<int, TextConfig>();
            Nouns = new Dictionary<int, TextConfig>();
            TextsDebug = new Dictionary<int, TextConfig>();
            Keys = new Dictionary<int, string>();
            Open(data);
            LoadBinary();
        }

        private static string EscapeNewLines(string text)
        {
            // Converts every real line break (\r\n, \n or \r) into the literal two-character sequence "\n"

            if (text == null)
                return text;

            return text.Replace("\r\n", "\\n").Replace("\n", "\\n").Replace("\r", "\\n");
        }

        private static string UnescapeNewLines(string text)
        {
            // Converts every literal "\n" sequence back into a real line break

            if (text == null)
                return text;

            return text.Replace("\\n", "\n");
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

                        // Convert literal "\n" back to real line breaks when reading
                        string text = UnescapeNewLines(y.Item.Variables[2].Value as string);
                        int textNumber = Convert.ToInt32(y.Item.Variables[1].Value);
                        int varianceKey = 0;

                        // Check if varianceKey exists (4th variable)
                        if (y.Item.Variables.Count > 3)
                        {
                            varianceKey = Convert.ToInt32(y.Item.Variables[3].Value);
                        }

                        if (text != null)
                        {
                            strings.Add(new StringLevel5(textNumber, text, varianceKey));
                        }
                        else
                        {
                            strings.Add(new StringLevel5(textNumber, "", varianceKey));
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

            // Get Debug Texts
            var debugTextInfoNodes = Entries.FindNodes(node => node.Name == "DEBUG_TEXT_INFO");

            TextsDebug = debugTextInfoNodes
                .GroupBy(
                    x => Convert.ToInt32(x.Item.Variables[0].Value),
                    y =>
                    {
                        List<StringLevel5> strings = new List<StringLevel5>();

                        // Convert literal "\n" back to real line breaks when reading
                        string text = UnescapeNewLines(y.Item.Variables[2].Value as string);
                        string textDebug = y.Item.Variables.Count > 3 ? UnescapeNewLines(y.Item.Variables[3].Value as string) : "";
                        int textNumber = Convert.ToInt32(y.Item.Variables[1].Value);

                        if (text != null)
                        {
                            strings.Add(new StringLevel5(textNumber, text, 0, textDebug));
                        }
                        else
                        {
                            strings.Add(new StringLevel5(textNumber, "", 0, textDebug));
                        }

                        return new TextConfig(strings, -1);
                    }
                )
                .ToDictionary(
                    group => group.Key,
                    group =>
                    {
                        var mergedStrings = group.SelectMany(item => item.Strings).ToList();
                        return new TextConfig(mergedStrings, -1);
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
                            // Convert literal "\n" back to real line breaks when reading
                            string text = UnescapeNewLines(y.Item.Variables[5].Value as string);
                            int varianceKey = Convert.ToInt32(y.Item.Variables[1].Value);

                            if (text != null)
                            {
                                strings.Add(new StringLevel5(0, text, varianceKey));
                            }
                            else
                            {
                                strings.Add(new StringLevel5(0, "", varianceKey));
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

            // Get Keys
            var keyInfoNodes = Entries.FindNodes(node => node.Name == "KEY_INFO");

            foreach (var keyNode in keyInfoNodes)
            {
                if (keyNode.Item.Variables.Count < 2)
                    continue;

                int keyCrc32 = Convert.ToInt32(keyNode.Item.Variables[0].Value);

                // The key must exist in Texts, Nouns, or TextsDebug; otherwise, it is skipped.
                if (!Texts.ContainsKey(keyCrc32) && !Nouns.ContainsKey(keyCrc32) && !TextsDebug.ContainsKey(keyCrc32))
                    continue;

                // If the key does not already exist in Keys, it is added.
                if (!Keys.ContainsKey(keyCrc32))
                {
                    string keyValue = keyNode.Item.Variables[1].Value as string;
                    Keys[keyCrc32] = keyValue;
                }
            }
        }

        public T2bþ(string xmlData) : base()
        {
            Encoding = Encoding.UTF8;

            Texts = new Dictionary<int, TextConfig>();
            Nouns = new Dictionary<int, TextConfig>();
            TextsDebug = new Dictionary<int, TextConfig>();
            Keys = new Dictionary<int, string>();

            XmlDocument xmlDoc = new XmlDocument();
            xmlDoc.LoadXml(xmlData);

            XmlNodeList textConfigNodes = xmlDoc.SelectNodes("Root/*/TextConfig");

            foreach (XmlNode textNode in textConfigNodes)
            {
                string crc32Attr = textNode.Attributes.GetNamedItem("crc32").Value;
                string washaAttr = textNode.Attributes.GetNamedItem("washa").Value;

                // The key and the washa can now be either in 0x format or plain text (in which case the crc32 is calculated).
                int crc32 = ParseKeyOrCrc32(crc32Attr);
                int washa = ParseKeyOrCrc32(washaAttr);

                //If the key is text (not 0x, not -1), it is saved in Keys.
                if (!crc32Attr.StartsWith("0x") && crc32Attr != "-1" && !Keys.ContainsKey(crc32))
                {
                    Keys[crc32] = crc32Attr;
                }

                XmlNodeList stringNodes = textNode.SelectNodes("String");
                List<StringLevel5> strings = new List<StringLevel5>();

                foreach (XmlNode stringNode in stringNodes)
                {
                    // Convert literal "\n" back to real line breaks when reading
                    string value = UnescapeNewLines(stringNode.Attributes.GetNamedItem("value").Value);
                    int textNumber = 0;
                    int varianceKey = 0;
                    string textDebug = null;

                    if (stringNode.Attributes.GetNamedItem("textNumber") != null)
                    {
                        textNumber = int.Parse(stringNode.Attributes.GetNamedItem("textNumber").Value);
                    }

                    if (stringNode.Attributes.GetNamedItem("varianceKey") != null)
                    {
                        varianceKey = int.Parse(stringNode.Attributes.GetNamedItem("varianceKey").Value);
                    }

                    if (stringNode.Attributes.GetNamedItem("textDebug") != null)
                    {
                        textDebug = UnescapeNewLines(stringNode.Attributes.GetNamedItem("textDebug").Value);
                    }

                    strings.Add(new StringLevel5(textNumber, value, varianceKey, textDebug));
                }

                if (textNode.ParentNode.Name == "Texts")
                {
                    Texts[crc32] = new TextConfig(strings, washa);
                }
                else if (textNode.ParentNode.Name == "Nouns")
                {
                    Nouns[crc32] = new TextConfig(strings, -1);
                }
                else if (textNode.ParentNode.Name == "TextsDebug")
                {
                    TextsDebug[crc32] = new TextConfig(strings, -1);
                }
            }
        }

        public T2bþ(string[] lines)
        {
            Encoding = Encoding.UTF8;

            Texts = new Dictionary<int, TextConfig>();
            Nouns = new Dictionary<int, TextConfig>();
            TextsDebug = new Dictionary<int, TextConfig>();
            Keys = new Dictionary<int, string>();

            int currentIndex = 0;
            TextConfig currentTextConfig = null;

            foreach (string line in lines)
            {
                Match match = GetMatch(line);

                if (match != null)
                {
                    string type = match.Groups[1].Value;
                    string keyPart = match.Groups[2].Value.Trim();
                    string washaPart = match.Groups[3].Value.Trim();

                    // The key and the washa can now be in either 0x format, -1 format, or plain text (in which case the crc32 is calculated).
                    int crc32 = ParseKeyOrCrc32(keyPart);
                    int washa = ParseKeyOrCrc32(washaPart);

                    // If the key is text (not 0x, not -1), it is saved in Keys.
                    if (!keyPart.StartsWith("0x") && keyPart != "-1" && !Keys.ContainsKey(crc32))
                    {
                        Keys[crc32] = keyPart;
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
                    else if (type.Trim().Equals("TextsDebug"))
                    {
                        TextsDebug[crc32] = currentTextConfig;
                    }
                }
                else if (!string.IsNullOrWhiteSpace(line))
                {
                    // Check for [textNumber; varianceKey] format
                    Match entryMatch = Regex.Match(line, @"^\[(\d+);\s*(\d+)\](.*)$");

                    if (entryMatch.Success && currentTextConfig != null)
                    {
                        int textNumber = int.Parse(entryMatch.Groups[1].Value);
                        int varianceKey = int.Parse(entryMatch.Groups[2].Value);

                        string rawText = entryMatch.Groups[3].Value;

                        // Only one space (the separator) is removed if it is present
                        // all additional spaces belong to the text
                        if (rawText.StartsWith(" "))
                            rawText = rawText.Substring(1);

                        // Convert literal "\n" back to real line breaks when reading
                        string text = UnescapeNewLines(rawText);

                        currentTextConfig.Strings.Add(new StringLevel5(textNumber, text, varianceKey));
                    }
                    else if (currentTextConfig != null)
                    {
                        // Convert literal "\n" back to real line breaks when reading
                        currentTextConfig.Strings.Add(new StringLevel5(currentTextConfig.Strings.Count, UnescapeNewLines(line), 0));
                    }
                    else
                    {
                        // Convert literal "\n" back to real line breaks when reading
                        Texts.Add(currentIndex, new TextConfig(new List<StringLevel5>() { new StringLevel5(0, UnescapeNewLines(line), 0) }, -1));
                        currentTextConfig = null;
                    }
                }

                currentIndex++;
            }
        }

        private Match GetMatch(string line)
        {
            // The format is now [Type/key/washa] where key and washa can be 0xHEX, -1, or plain text
            Match match = Regex.Match(line, @"\[(\w+)/([^/\]]+)/([^\]]+)\]");

            if (match.Success)
                return match;

            return null;
        }

        private int ParseKeyOrCrc32(string value)
        {
            // Parses a key or a washa: "-1" -> -1, "0x..." -> hexadecimal, otherwise -> CRC32 of the text

            if (value == "-1")
                return -1;

            if (value.StartsWith("0x"))
            {
                return int.Parse(value.Replace("0x", ""), System.Globalization.NumberStyles.HexNumber);
            }

            return unchecked((int)Crc32.Compute(Encoding.GetBytes(value)));
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

            string[] allDebugTexts = TextsDebug.Values
                .SelectMany(textList => textList.Strings)
                .Select(textValue => textValue.Text)
                .Distinct()
                .ToArray();

            return allTexts.Union(allNouns).Union(allDebugTexts).ToArray();
        }

        private CfgTreeNode GetTextEntry(bool variance)
        {
            var textBeginEntry = new Entry("TEXT_INFO_BEGIN", new List<Variable>()
            {
                new Variable(CfgValueType.Int, Texts.Values.Sum(textList => textList.Strings.Count))
            });

            var textBeginNode = new CfgTreeNode(textBeginEntry, 0);

            foreach (KeyValuePair<int, TextConfig> textItem in Texts)
            {
                for (int i = 0; i < textItem.Value.Strings.Count; i++)
                {
                    StringLevel5 textValue = textItem.Value.Strings[i];

                    List<Variable> variables = new List<Variable>()
                    {
                        new Variable(CfgValueType.Int, textItem.Key),
                        new Variable(CfgValueType.Int, textValue.TextNumber),
                        new Variable(CfgValueType.String, EscapeNewLines(textValue.Text)),
                    };

                    // Add variance only if variance = true
                    if (variance)
                    {
                        variables.Add(new Variable(CfgValueType.Int, textValue.VarianceKey));
                    }

                    Entry textItemEntry = new Entry("TEXT_INFO", variables);
                    textBeginNode.AddChild(new CfgTreeNode(textItemEntry, 1));
                }
            }

            return textBeginNode;
        }

        private CfgTreeNode GetDebugTextEntry()
        {
            var debugTextBeginEntry = new Entry("DEBUG_TEXT_INFO_BEGIN", new List<Variable>()
            {
                new Variable(CfgValueType.Int, TextsDebug.Values.Sum(textList => textList.Strings.Count))
            });

            var debugTextBeginNode = new CfgTreeNode(debugTextBeginEntry, 0);

            foreach (KeyValuePair<int, TextConfig> textItem in TextsDebug)
            {
                for (int i = 0; i < textItem.Value.Strings.Count; i++)
                {
                    StringLevel5 textValue = textItem.Value.Strings[i];

                    Entry textItemEntry = new Entry("DEBUG_TEXT_INFO", new List<Variable>()
                    {
                        new Variable(CfgValueType.Int, textItem.Key),
                        new Variable(CfgValueType.Int, textValue.TextNumber),
                        new Variable(CfgValueType.String, EscapeNewLines(textValue.Text)),
                        new Variable(CfgValueType.String, EscapeNewLines(textValue.TextDebug)),
                    });

                    debugTextBeginNode.AddChild(new CfgTreeNode(textItemEntry, 1));
                }
            }

            return debugTextBeginNode;
        }

        private CfgTreeNode GetTextConfigEntry()
        {
            int index = 0;
            List<int> washas = Texts.Where(x => x.Value.WashaID != -1).Select(x => x.Value.WashaID).ToList();

            var textConfigEntry = new Entry("TEXT_CONFIG_BEGIN", new List<Variable>()
            {
                new Variable(CfgValueType.Int, Texts.Count)
            });
            var textConfigNode = new CfgTreeNode(textConfigEntry, 0);

            foreach (KeyValuePair<int, TextConfig> textItem in Texts)
            {
                Entry textConfigItemEntry = new Entry("TEXT_CONFIG", new List<Variable>()
                {
                    new Variable(CfgValueType.Int, textItem.Key),
                    new Variable(CfgValueType.Int, textItem.Value.Strings.Count),
                    new Variable(CfgValueType.Int, washas.IndexOf(textItem.Value.WashaID)),
                });

                textConfigNode.AddChild(new CfgTreeNode(textConfigItemEntry, 1));
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
            var textWashaNode = new CfgTreeNode(textWashaEntry, 0);

            for (int i = 0; i < washas.Length; i++)
            {
                Entry textWashaItem = new Entry("TEXT_WASHA", new List<Variable>()
                {
                    new Variable(CfgValueType.Int, i),
                    new Variable(CfgValueType.Int, washas[i]),
                });

                textWashaNode.AddChild(new CfgTreeNode(textWashaItem, 1));
            }

            return textWashaNode;
        }

        private CfgTreeNode GetNounEntry(bool variance)
        {
            var nounEntry = new Entry("NOUN_INFO_BEGIN", new List<Variable>()
            {
                new Variable(CfgValueType.Int, Nouns.Values.Sum(textList => textList.Strings.Count))
            });
            var nounNode = new CfgTreeNode(nounEntry, 0);

            foreach (KeyValuePair<int, TextConfig> nounItem in Nouns)
            {
                for (int i = 0; i < nounItem.Value.Strings.Count; i++)
                {
                    StringLevel5 textValue = nounItem.Value.Strings[i];

                    // For NOUN_INFO, we keep the variable but set the value to 0 if variance = false
                    int varianceValue = variance ? textValue.VarianceKey : 0;

                    Entry textItemEntry = new Entry("NOUN_INFO", new List<Variable>()
                    {
                        new Variable(CfgValueType.Int, nounItem.Key),
                        new Variable(CfgValueType.Int, varianceValue),
                        new Variable(CfgValueType.String, null),
                        new Variable(CfgValueType.String, null),
                        new Variable(CfgValueType.String, null),
                        new Variable(CfgValueType.String, EscapeNewLines(textValue.Text)),
                        new Variable(CfgValueType.String, null),
                        new Variable(CfgValueType.String, null),
                        new Variable(CfgValueType.String, null),
                        new Variable(CfgValueType.String, null),
                        new Variable(CfgValueType.Int, 0),
                        new Variable(CfgValueType.Int, 0),
                        new Variable(CfgValueType.Int, 0),
                        new Variable(CfgValueType.Int, 0),
                    });

                    nounNode.AddChild(new CfgTreeNode(textItemEntry, 1));
                }
            }

            return nounNode;
        }

        private CfgTreeNode GetKeyEntry()
        {
            var keyBeginEntry = new Entry("KEY_INFO_BEGIN", new List<Variable>()
            {
                new Variable(CfgValueType.Int, Keys.Count)
            });

            var keyBeginNode = new CfgTreeNode(keyBeginEntry, 0);

            foreach (KeyValuePair<int, string> keyItem in Keys)
            {
                Entry keyItemEntry = new Entry("KEY_INFO", new List<Variable>()
                {
                    new Variable(CfgValueType.Int, keyItem.Key),
                    new Variable(CfgValueType.String, keyItem.Value),
                });

                keyBeginNode.AddChild(new CfgTreeNode(keyItemEntry, 1));
            }

            return keyBeginNode;
        }

        private void AddSection(string beginKey, string endKey, Func<CfgTreeNode> createNode)
        {
            if (Entries.Exists(beginKey))
                Entries.Delete(beginKey);

            Entries.AddChild(createNode());

            if (Entries.Exists(endKey))
                Entries.Delete(endKey);

            Entries.AddChild(new CfgTreeNode(new Entry(endKey)));
        }

        public void Save(string fileName, bool iego, bool variance, bool saveKey = false)
        {
            if (Texts.Count > 0)
                AddSection("TEXT_INFO_BEGIN", "TEXT_INFO_END", () => GetTextEntry(variance));

            if (TextsDebug.Count > 0)
                AddSection("DEBUG_TEXT_INFO_BEGIN", "DEBUG_TEXT_INFO_END", () => GetDebugTextEntry());

            if (Nouns.Count > 0)
                AddSection("NOUN_INFO_BEGIN", "NOUN_INFO_END", () => GetNounEntry(variance));

            if (iego)
            {
                AddSection("TEXT_CONFIG_BEGIN", "TEXT_CONFIG_END", () => GetTextConfigEntry());
                AddSection("TEXT_WASHA_BEGIN", "TEXT_WASHA_END", () => GetTextWashaEntry());
            }

            if (saveKey && Keys.Count > 0)
                AddSection("KEY_INFO_BEGIN", "KEY_INFO_END", () => GetKeyEntry());

            Save(fileName);
        }

        public new byte[] Save(bool saveKey = false)
        {
            if (Texts.Count > 0)
                AddSection("TEXT_INFO_BEGIN", "TEXT_INFO_END", () => GetTextEntry(true));

            if (TextsDebug.Count > 0)
                AddSection("DEBUG_TEXT_INFO_BEGIN", "DEBUG_TEXT_INFO_END", () => GetDebugTextEntry());

            if (Nouns.Count > 0)
                AddSection("NOUN_INFO_BEGIN", "NOUN_INFO_END", () => GetNounEntry(true));

            if (saveKey && Keys.Count > 0)
                AddSection("KEY_INFO_BEGIN", "KEY_INFO_END", () => GetKeyEntry());

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

                // If we have the corresponding text key in Keys, we write it as is; otherwise, we keep the 0x format
                string crc32Attr = Keys.ContainsKey(crc32) ? Keys[crc32] : "0x" + crc32.ToString("X8");

                xmlBuilder.AppendLine($" <TextConfig crc32=\"{crc32Attr}\" washa=\"{washa}\">");

                foreach (var stringLevel5 in kvp.Value.Strings)
                {
                    // Convert real line breaks to literal "\n" when saving, then escape XML-sensitive characters
                    string escapedText = EscapeNewLines(stringLevel5.Text).Replace("<", "&lt;").Replace(">", "&gt;").Replace("\"", "&quot;");

                    if (!string.IsNullOrEmpty(stringLevel5.TextDebug))
                    {
                        string escapedDebugText = EscapeNewLines(stringLevel5.TextDebug).Replace("<", "&lt;").Replace(">", "&gt;").Replace("\"", "&quot;");
                        xmlBuilder.AppendLine($"  <String textNumber=\"{stringLevel5.TextNumber}\" varianceKey=\"{stringLevel5.VarianceKey}\" value=\"{escapedText}\" textDebug=\"{escapedDebugText}\" />");
                    }
                    else
                    {
                        xmlBuilder.AppendLine($"  <String textNumber=\"{stringLevel5.TextNumber}\" varianceKey=\"{stringLevel5.VarianceKey}\" value=\"{escapedText}\" />");
                    }
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

            if (TextsDebug.Count > 0)
            {
                xmlStrings.AddRange(ConvertToXml(TextsDebug, "TextsDebug"));
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

            // Calculate maximum width for alignment
            int maxPrefixWidth = 0;
            foreach (var kvp in Texts.Concat(Nouns).Concat(TextsDebug))
            {
                foreach (var stringLevel5 in kvp.Value.Strings)
                {
                    int prefixLength = $"[{stringLevel5.TextNumber}; {stringLevel5.VarianceKey}]".Length;
                    if (prefixLength > maxPrefixWidth)
                        maxPrefixWidth = prefixLength;
                }
            }

            foreach (var kvp in Texts)
            {
                int crc32 = kvp.Key;
                string washa = "0x" + kvp.Value.WashaID.ToString("X8");

                // If we have the corresponding text key in Keys, we write it as is; otherwise, we keep the 0x format
                string crc32Str = Keys.ContainsKey(crc32) ? Keys[crc32] : "0x" + crc32.ToString("X8");

                StringBuilder textBuilder = new StringBuilder();
                textBuilder.AppendFormat("[Texts/{0}/{1}] {2}", crc32Str, washa, Environment.NewLine);

                foreach (var stringLevel5 in kvp.Value.Strings)
                {
                    string formattedPrefix = $"[{stringLevel5.TextNumber}; {stringLevel5.VarianceKey}]".PadRight(maxPrefixWidth);
                    // Convert real line breaks to literal "\n" when saving
                    textBuilder.AppendLine($"{formattedPrefix} {EscapeNewLines(stringLevel5.Text)}");
                }

                txtStrings.Add(textBuilder.ToString());
            }

            foreach (var kvp in TextsDebug)
            {
                int crc32 = kvp.Key;

                // If we have the corresponding text key in Keys, we write it as is; otherwise, we keep the 0x format
                string crc32Str = Keys.ContainsKey(crc32) ? Keys[crc32] : "0x" + crc32.ToString("X8");

                StringBuilder textBuilder = new StringBuilder();
                textBuilder.AppendFormat("[TextsDebug/{0}/-1] {1}", crc32Str, Environment.NewLine);

                foreach (var stringLevel5 in kvp.Value.Strings)
                {
                    string formattedPrefix = $"[{stringLevel5.TextNumber}; 0]".PadRight(maxPrefixWidth);
                    // Convert real line breaks to literal "\n" when saving
                    string debugInfo = !string.IsNullOrEmpty(stringLevel5.TextDebug) ? $" [DEBUG: {EscapeNewLines(stringLevel5.TextDebug)}]" : "";
                    textBuilder.AppendLine($"{formattedPrefix} {EscapeNewLines(stringLevel5.Text)}{debugInfo}");
                }

                txtStrings.Add(textBuilder.ToString());
            }

            foreach (var kvp in Nouns)
            {
                int crc32 = kvp.Key;

                // If we have the corresponding text key in Keys, we write it as is; otherwise, we keep the 0x format
                string crc32Str = Keys.ContainsKey(crc32) ? Keys[crc32] : "0x" + crc32.ToString("X8");

                StringBuilder textBuilder = new StringBuilder();
                textBuilder.AppendFormat("[Nouns/{0}/-1] {1}", crc32Str, Environment.NewLine);

                foreach (var stringLevel5 in kvp.Value.Strings)
                {
                    string formattedPrefix = $"[{stringLevel5.TextNumber}; {stringLevel5.VarianceKey}]".PadRight(maxPrefixWidth);
                    // Convert real line breaks to literal "\n" when saving
                    textBuilder.AppendLine($"{formattedPrefix} {EscapeNewLines(stringLevel5.Text)}");
                }

                txtStrings.Add(textBuilder.ToString());
            }

            return txtStrings.ToArray();
        }
    }
}