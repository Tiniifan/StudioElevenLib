using System;
using System.IO;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using StudioElevenLib.Level5.Binary.Collections;
using StudioElevenLib.Level5.Binary;
using StudioElevenLib.Level5.Compression;
using StudioElevenLibTest.TestClass;
using static StudioElevenLibTest.TestClass.Event;
using System.Runtime.InteropServices;
using StudioElevenLib.Level5.Compression.NoCompression;
using static System.Net.Mime.MediaTypeNames;
using System.Linq.Expressions;
using StudioElevenLib.Level5.Text;
using StudioElevenLib.Level5.Resource.RES;
using StudioElevenLib.Level5.Resource;
using StudioElevenLib.Level5.Resource.Types;

namespace StudioElevenLibTest
{
    internal class Program
    {
        private static void DefaultFuncpt()
        {
            var test = new CfgBin<PtreeNode>();
            byte[] fileData = File.ReadAllBytes("./mr01b01_funcpt.bin");
            test.Open(fileData);
            PtreeNode funcPtreeNode = test.Entries.FindByHeader("FUNCPOINT");
            Funcpoint funcpoint = new Funcpoint(funcPtreeNode);

            // Print Funcpoint information
            Console.WriteLine("=== FUNCPOINT INFORMATION ===");
            Console.WriteLine($"Funcpoint Name: {funcpoint.FuncpointName ?? "N/A"}");
            Console.WriteLine($"Map ID: {funcpoint.MapID ?? "N/A"}");
            Console.WriteLine($"Number of Events: {funcpoint.Events?.Count ?? 0}");
            Console.WriteLine();

            // Print Events
            if (funcpoint.Events != null && funcpoint.Events.Count > 0)
            {
                Console.WriteLine("=== EVENTS ===");
                for (int i = 0; i < funcpoint.Events.Count; i++)
                {
                    var evt = funcpoint.Events[i];
                    Console.WriteLine($"Event {i + 1}:");
                    Console.WriteLine($"  Name: {evt.EventName ?? "N/A"}");
                    Console.WriteLine($"  Type: {evt.PtreType ?? "N/A"}");

                    // Print Position
                    if (evt.Position != null)
                    {
                        Console.WriteLine($"  Position: X={evt.Position.X}, Y={evt.Position.Y}, Z={evt.Position.Z}");
                    }

                    // Print Area
                    if (evt.Area != null)
                    {
                        switch (evt.Area)
                        {
                            case BoxLimiter box:
                                Console.WriteLine($"  Area: Box - Width={box.Width:F3}, Height={box.Height:F3}, Angle={box.Angle:F3}°");
                                break;
                            case CircleLimiter circle:
                                Console.WriteLine($"  Area: Circle - Radius={circle.Radius:F3}, Angle={circle.Angle:F3}°");
                                break;
                            default:
                                Console.WriteLine($"  Area: {evt.Area.GetType().Name}");
                                break;
                        }
                    }

                    // Print Event Definition
                    if (evt.EventDef != null)
                    {
                        Console.WriteLine($"  Event Definition:");
                        Console.WriteLine($"    Name: {evt.EventDef.EventName ?? "N/A"}");
                        Console.WriteLine($"    Type: {evt.EventDef.PtreType ?? "N/A"}");
                        Console.WriteLine($"    TBoxBitCheck: {evt.EventDef.TBoxBitCheck?.ToString() ?? "N/A"}");
                        Console.WriteLine($"    BtnCheck: {evt.EventDef.BtnCheck?.ToString() ?? "N/A"}");

                        // Print Event Object details based on type
                        if (evt.EventDef.EventObject != null)
                        {
                            Console.WriteLine($"    Event Object Type: {evt.EventDef.EventObject.GetType().Name}");

                            switch (evt.EventDef.EventObject)
                            {
                                case EventTrigger trigger:
                                    Console.WriteLine($"      Event Type: {trigger.EventType ?? "N/A"}");
                                    Console.WriteLine($"      Event ID: {trigger.EventID?.ToString() ?? "N/A"}");
                                    Console.WriteLine($"      Stop: {trigger.Stop?.ToString() ?? "N/A"}");
                                    Console.WriteLine($"      Item ID: {trigger.ItemID?.ToString() ?? "N/A"}");
                                    Console.WriteLine($"      Get Bit: {trigger.GetBit?.ToString() ?? "N/A"}");
                                    Console.WriteLine($"      EV_T_BIT: {trigger.EV_T_BIT?.ToString() ?? "N/A"}");
                                    Console.WriteLine($"      EV_F_BIT: {trigger.EV_F_BIT?.ToString() ?? "N/A"}");
                                    break;

                                case EventSE se:
                                    Console.WriteLine($"      Sound Type: {se.SoundType?.ToString() ?? "N/A"}");
                                    Console.WriteLine($"      SE Name: {se.SeName ?? "N/A"}");
                                    Console.WriteLine($"      Frame: {se.Frame?.ToString() ?? "N/A"}");
                                    Console.WriteLine($"      Is Fade: {se.IsFade?.ToString() ?? "N/A"}");
                                    Console.WriteLine($"      Fade Radius: {se.FadeRadius?.ToString() ?? "N/A"}");
                                    break;

                                case EventMapJump mapJump:
                                    if (mapJump.JumpTo != null)
                                    {
                                        Console.WriteLine($"      Jump To:");
                                        Console.WriteLine($"        Map ID: {mapJump.JumpTo.MapID ?? "N/A"}");
                                        Console.WriteLine($"        Move Rot: {mapJump.JumpTo.MoveRot?.ToString() ?? "N/A"}");
                                        Console.WriteLine($"        Map Motion: {mapJump.JumpTo.MapMotion ?? "N/A"}");
                                        Console.WriteLine($"        Fade Frame: {mapJump.JumpTo.FadeFrame?.ToString() ?? "N/A"}");
                                        Console.WriteLine($"        SE Name: {mapJump.JumpTo.SeName ?? "N/A"}");
                                        Console.WriteLine($"        SE Frame: {mapJump.JumpTo.SeFrame?.ToString() ?? "N/A"}");
                                    }
                                    if (mapJump.STDPos != null)
                                    {
                                        string stdPosInfo = $"      STD Position: X={mapJump.STDPos.X:F3}, Y={mapJump.STDPos.Y:F3}, Z={mapJump.STDPos.Z:F3}";
                                        if (mapJump.STDPos.W.HasValue)
                                        {
                                            stdPosInfo += $", W={mapJump.STDPos.W.Value:F3}";
                                        }
                                        Console.WriteLine(stdPosInfo);
                                    }
                                    break;
                            }
                        }
                    }
                    Console.WriteLine();
                }
            }
            else
            {
                Console.WriteLine("No events found.");
            }
        }

        private static void DefaultMapenv()
        {
            var test = new CfgBin<PtreeNode>();

            byte[] fileData = File.ReadAllBytes("./mr05b05_mapenv.bin");
            test.Open(fileData);

            PtreeNode commonExterior = test.Entries.FindByHeader("MAP_ENV");

            //Console.WriteLine(commonExterior.PrintTree());

            Mapenv mapenv = new Mapenv(commonExterior);

            PtreeNode testPtree = mapenv.ToPtreeNode();

            Console.WriteLine(testPtree.PrintTree());

            //Mapenv mapenv = commonExterior.FlattenPtreeToClass<Mapenv>("");

            //Console.WriteLine(mapenv.Camera.Fov);

            //Console.WriteLine(mapenv.Default);
            //Console.WriteLine(mapenv.ParentID);
            //Console.WriteLine(mapenv.MMModelPos.MinX + " " + mapenv.MMModelPos.MinY + " " + mapenv.MMModelPos.MaxX + " " + mapenv.MMModelPos.MaxY);
            //Console.WriteLine(mapenv.Light.ShadowCol[0].ID);
            //Console.WriteLine(mapenv.Light.ShadowCol[0].Color.G);

        }

        private static void DefaultHealpt()
        {
            var test = new CfgBin<PtreeNode>();
            byte[] fileData = File.ReadAllBytes("./mr01b01_healpt.bin");
            test.Open(fileData);
            PtreeNode funcPtreeNode = test.Entries.FindByHeader("HEALPOINT");
            Healpoint healpoint = new Healpoint(funcPtreeNode);

            PtreeNode testPtree = healpoint.ToPtreeNode();
            healpoint = new Healpoint(testPtree);

            Console.WriteLine(testPtree.PrintTree());


            // Print Healpoint information
            Console.WriteLine("=== HEALPOINT INFORMATION ===");
            Console.WriteLine($"Healpoint Name: {healpoint.HealpointName ?? "N/A"}");
            Console.WriteLine($"Map ID: {healpoint.MapID ?? "N/A"}");
            Console.WriteLine($"Number of Heal areas: {healpoint.HealAreas?.Count ?? 0}");
            Console.WriteLine();

            // Print heals area
            if (healpoint.HealAreas != null && healpoint.HealAreas.Count > 0)
            {
                Console.WriteLine("=== HEAL AREAS ===");
                for (int i = 0; i < healpoint.HealAreas.Count; i++)
                {
                    var area = healpoint.HealAreas[i];
                    Console.WriteLine($"Heal {i + 1}:");
                    Console.WriteLine($"  Name: {area.HealAreaName ?? "N/A"}");

                    // Print Position
                    if (area.Position != null)
                    {
                        Console.WriteLine($"  Position: X={area.Position.X}, Y={area.Position.Y}, Z={area.Position.Z}");
                    }
                }
            }
        }

        private static void DefaultNPC()
        {
            var test = new CfgBin<CfgTreeNode>();
            byte[] fileData = File.ReadAllBytes("./mr01b01.npc.bin");
            test.Open(fileData);

            CfgTreeNode npcBaseBegin = test.Entries.FindByName("NPC_BASE_BEGIN");
            if (npcBaseBegin != null)
            {
                List<NPCBase> npcBases = npcBaseBegin.FlattenEntryToClassList<NPCBase>("NPC_BASE");

                for (int i = 0; i < npcBases.Count; i++)
                {
                    NPCBase npcBase = npcBases[i];
                    Console.WriteLine($"NPC {i + 1}:");
                    Console.WriteLine($"  ID: {npcBase.ID.ToString("X8") ?? "N/A"}");
                    Console.WriteLine($"  ModelHead: {npcBase.ModelHead.ToString() ?? "N/A"}");
                    Console.WriteLine($"  Type: {npcBase.Type.ToString() ?? "N/A"}");
                    Console.WriteLine($"  UniformNumber: {npcBase.UniformNumber.ToString() ?? "N/A"}");
                    Console.WriteLine($"  BootsNumber: {npcBase.BootsNumber.ToString() ?? "N/A"}");
                    Console.WriteLine($"  GloveNumber: {npcBase.GloveNumber.ToString() ?? "N/A"}");
                    Console.WriteLine($"  Icon: {npcBase.Icon.ToString() ?? "N/A"}");
                }
            }

            CfgTreeNode npcPreseteBegin = test.Entries.FindByName("NPC_PRESET_BEGIN");
            if (npcPreseteBegin != null)
            {
                List<NPCPreset> npcPresets = npcPreseteBegin.FlattenEntryToClassList<NPCPreset>("NPC_PRESET");

                for (int i = 0; i < npcPresets.Count; i++)
                {
                    NPCPreset npcPreset = npcPresets[i];
                    Console.WriteLine($"NPCPreset {i + 1}:");
                    //Console.WriteLine($"  ID: {npcPreset.ID.ToString("X8") ?? "N/A"}");
                    Console.WriteLine($"  NPCAppearStartIndex: {npcPreset.NPCAppearStartIndex.ToString() ?? "N/A"}");
                    Console.WriteLine($"  NPCAppearCount: {npcPreset.NPCAppearCount.ToString() ?? "N/A"}");
                }
            }

            CfgTreeNode npcAppearBegin = test.Entries.FindByName("NPC_APPEAR_BEGIN");
            if (npcAppearBegin != null)
            {
                List<NPCAppear> npcAppears = npcAppearBegin.FlattenEntryToClassList<NPCAppear>("NPC_APPEAR");

                for (int i = 0; i < npcAppears.Count; i++)
                {
                    NPCAppear npcAppear = npcAppears[i];
                    Console.WriteLine($"NPCPreset {i + 1}:");
                    Console.WriteLine($"  LocationX: {npcAppear.LocationX.ToString() ?? "N/A"}");
                    Console.WriteLine($"  LocationZ: {npcAppear.LocationZ.ToString() ?? "N/A"}");
                    Console.WriteLine($"  LocationY: {npcAppear.LocationY.ToString() ?? "N/A"}");
                    Console.WriteLine($"  Rotation: {npcAppear.Rotation.ToString() ?? "N/A"}");
                    Console.WriteLine($"  StandAnimation: {npcAppear.StandAnimation.ToString() ?? "N/A"}");
                    Console.WriteLine($"  LookAtThePlayer: {npcAppear.LookAtThePlayer.ToString() ?? "N/A"}");
                    Console.WriteLine($"  TalkAnimation: {npcAppear.TalkAnimation.ToString() ?? "N/A"}");
                    Console.WriteLine($"  UnkAnimation: {npcAppear.UnkAnimation.ToString() ?? "N/A"}");
                    Console.WriteLine($"  PhaseAppear: {npcAppear.PhaseAppear.ToString() ?? "N/A"}");
                }
            }
        }

        private static void NoCompression()
        {
            // Tes données brutes
            byte[] rawData = new byte[]
            {
            0x3C, 0x00, 0x00, 0x00, 0xFF, 0xFF, 0xFF, 0xFF,
            0xFF, 0xFF, 0xFF, 0xFF, 0xFF, 0xFF, 0xFF, 0xFF,
            0xFF, 0xFF, 0xFF, 0xFF, 0xFF, 0xFF, 0xFF, 0xFF,
            0xFF, 0xFF, 0xFF, 0xFF, 0xFF, 0xFF, 0xFF, 0xFF,
            0x00, 0x00, 0x80, 0xBF, 0x00, 0x00, 0x80, 0xBF,
            0xFF, 0xFF, 0xFF, 0xFF, 0xFF, 0xFF, 0xFF, 0xFF,
            0xFF, 0xFF, 0xFF, 0xFF, 0xFF, 0xFF, 0xFF, 0xFF,
            0xFF, 0xFF, 0xFF, 0xFF
            };

            // Création de l'objet NoCompression
            NoCompression compressor = new NoCompression();

            // Compression (en réalité, juste ajoute le header)
            byte[] compressedData = compressor.Compress(rawData);
            string hexString = string.Join(" ", compressedData.Select(b => b.ToString("X2")));
            Console.WriteLine(hexString);
        }

        private static void GetCondition()
        {
            HashSet<string> uniqueBase64s = new HashSet<string>();
            Dictionary<(byte, byte), string> seenTuples = new Dictionary<(byte, byte), string>();

            string mapsPath = "./map";

            if (!Directory.Exists(mapsPath))
            {
                Console.WriteLine("Le dossier 'map' n'existe pas.");
                return;
            }

            // --------------------
            // Traiter tous les fichiers .npc.bin
            // --------------------
            foreach (var file in Directory.GetFiles(mapsPath, "*.npc.bin", SearchOption.AllDirectories))
            {
                Console.WriteLine(file);

                var cfgbin = new CfgBin<CfgTreeNode>();
                byte[] fileData = File.ReadAllBytes(file);
                cfgbin.Open(fileData);

                CfgTreeNode npcAppearBegin = cfgbin.Entries.FindByName("NPC_APPEAR_BEGIN");
                if (npcAppearBegin != null)
                {
                    try
                    {
                        List<NPCAppear> npcAppears = npcAppearBegin.FlattenEntryToClassList<NPCAppear>("NPC_APPEAR");

                        foreach (var npcAppear in npcAppears)
                        {
                            string base64 = npcAppear.PhaseAppear?.ToString();
                            if (string.IsNullOrEmpty(base64))
                                continue;

                            try
                            {
                                byte[] bytes = Convert.FromBase64String(base64);
                                if (bytes.Length > 5)
                                {
                                    uniqueBase64s.Add(base64);

                                }
                            }
                            catch
                            {
                                // Ignore Base64 invalides
                            }
                        }
                    }
                    catch
                    {
                        // Ignore fichiers npc invalides
                    }
                }
            }

            // --------------------
            // Traiter tous les fichiers .talk.bin
            // --------------------
            foreach (var file in Directory.GetFiles(mapsPath, "*.talk.bin", SearchOption.AllDirectories))
            {
                Console.WriteLine(file);

                var cfgbin = new CfgBin<CfgTreeNode>();
                byte[] fileData = File.ReadAllBytes(file);
                cfgbin.Open(fileData);

                CfgTreeNode talkConfigBegin = cfgbin.Entries.FindByName("TALK_CONFIG_BEGIN");
                if (talkConfigBegin != null)
                {
                    try
                    {
                        List<NPCTalk> talks = talkConfigBegin.FlattenEntryToClassList<NPCTalk>("TALK_CONFIG");

                        foreach (var talk in talks)
                        {
                            string base64 = talk.EventCondition?.ToString();
                            if (string.IsNullOrEmpty(base64))
                                continue;

                            try
                            {
                                byte[] bytes = Convert.FromBase64String(base64);
                                if (bytes.Length > 5)
                                {
                                    uniqueBase64s.Add(base64);
                                }
                            }
                            catch
                            {
                                // Ignore Base64 invalides
                            }
                        }
                    }
                    catch
                    {
                        // Ignore fichiers talk invalides
                    }
                }
            }

            Console.WriteLine("Base64 uniques avec tuples uniques (octets 0x04 et 0x05) :");
            foreach (var b64 in System.Linq.Enumerable.OrderBy(uniqueBase64s, s => s.Length))
            {
                Console.WriteLine(b64);
            }
        }

        static void Main(string[] args)
        {
            byte[] fileData = File.ReadAllBytes("./binder_fr_xa/RES_player_cs.bin");
            //byte[] fileData = File.ReadAllBytes("./binder_fr_xa/RES_cs.bin");
            RES resTest = new RES(fileData);

            foreach (KeyValuePair<RESType, List<RESElement>> item in resTest.Items)
            {
                foreach (RESElement reselement in item.Value) {
                    Console.WriteLine(reselement.Name);
                }

            }

            
            // GetCondition();

            // NoCompression();
            // DefaultMapenv();

            // DefaultFuncpt();

            // DefaultHealpt();

            // DefaultNPC();

            //byte[] fileData = File.ReadAllBytes("./mr01b01_funcpt.bin");
            //test.Open(fileData);

            //Console.WriteLine(test.Entries.PrintTree());

            //PtreeNode MJ_mr01i51 = test.Entries.FindByHeaderAndValue("FP", "MJ_mr01i51");
            //MapJump mapJump = MJ_mr01i51.FlattenPtreeToClass<MapJump>();

            //Console.WriteLine(mapJump.FuncName);
            //Console.WriteLine(mapJump.ID);
            //Console.WriteLine(string.Join(", ", mapJump.Pos));
            //Console.WriteLine((mapJump.Area as BoxLimiter).Width + " " + (mapJump.Area as BoxLimiter).Height + " " + (mapJump.Area as BoxLimiter).Angle);
            //Console.WriteLine(mapJump.Definition.FuncName);
            //Console.WriteLine(mapJump.Definition.ID);
            //Console.WriteLine(mapJump.Definition.BtnCheck.Enable);
            //Console.WriteLine(mapJump.Definition.Event.MapJumpEventJumpTo.SeName);
            //Console.WriteLine(mapJump.Definition.Event.MapJumpEventJumpTo.FadeFrame);
            //Console.WriteLine(mapJump.Definition.Event.MapJumpEventJumpTo.MapID);
            //Console.WriteLine(string.Join(", ", mapJump.Definition.Event.MapJumpEventSTDPos.Pos));


            //var test = new CfgBin<PtreeNode>();

            //byte[] fileData = File.ReadAllBytes("./mr05b05_mapenv.bin");
            //test.Open(fileData);

            //Console.WriteLine(test.Entries.PrintTree());

            //Mapenv mapenv = test.Entries.FlattenPtreeToClass<Mapenv>("MAP_ENV");

            //Console.WriteLine(mapenv.Default);
            //Console.WriteLine(mapenv.ParentID);
            //Console.WriteLine(mapenv.MMModelPos.MinX + " " + mapenv.MMModelPos.MinY + " " + mapenv.MMModelPos.MaxX + " " + mapenv.MMModelPos.MaxY);
            //Console.WriteLine(mapenv.Light.ShadowCol[0].ID);
            //Console.WriteLine(mapenv.Light.ShadowCol[0].Color.G);

            //var charaparam = new CfgBin<CfgTreeNode>();
            //charaparam.Open(File.ReadAllBytes("./chara_param.cfg.bin"));

            //Console.WriteLine(test.Entries.PrintTree());

            //CfgTreeNode charaparamNode = charaparam.Entries.FindByName("CHARA_PARAM_INFO_BEGIN");

            //Console.WriteLine(charaparamNode.Item.Variables[0].Value);

            //var charaParams = charaparamNode.FlattenEntryToClassList<Charaparam>("CHARA_PARAM_INFO");

            //Console.WriteLine(charaParams[0].ParamHash.ToString("X8"));
            //Console.WriteLine(charaParams[0].IgnoreMe);
            //Console.WriteLine(charaParams[0].BaseHash.ToString("X8"));
        }
    }
}
