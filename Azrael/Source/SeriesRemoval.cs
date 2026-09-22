using System.Collections.Generic;
using System.Text;
using HarmonyLib;
using RimWorld;
using RimWorld.Planet;
using UnityEngine;
using Verse;

namespace Azrael
{
    internal enum RemovalVerdict
    {
        Deconstruct,
        ConsumeOrSell,
        ResolveFirst,
        AutoSafe,
    }

    internal struct RemovalLine
    {
        public string Label;
        public int Count;
        public RemovalVerdict Verdict;
        public GlobalTargetInfo Jump;
        public bool CanDeconstruct;
    }

    internal class RemovalReport
    {
        public string Display;
        public string PackageId;
        public List<RemovalLine> Lines = new List<RemovalLine>();
        public List<Thing> Deconstructables = new List<Thing>();
        public List<string> Blockers = new List<string>();
    }

    internal static class SeriesRemoval
    {
        public static RemovalReport Scan(string display, string packageId)
        {
            var report = new RemovalReport { Display = display, PackageId = packageId };
            if (Current.Game == null)
            {
                report.Blockers.Add("Azrael_Removal_NeedSave".Translate());
                return report;
            }

            string needle = packageId ?? string.Empty;
            List<Map> maps = Find.Maps;
            for (int m = 0; m < maps.Count; m++)
            {
                Map map = maps[m];
                if (map == null)
                {
                    continue;
                }
                ScanMap(map, needle, report);
            }

            List<Caravan> caravans = Find.WorldObjects.Caravans;
            for (int c = 0; c < caravans.Count; c++)
            {
                ScanCaravan(caravans[c], needle, report);
            }

            AddPawnState(needle, report);
            return report;
        }

        public static void DesignateDeconstruct(RemovalReport report)
        {
            if (report?.Deconstructables == null)
            {
                return;
            }
            int n = 0;
            for (int i = 0; i < report.Deconstructables.Count; i++)
            {
                Thing t = report.Deconstructables[i];
                if (t == null || t.Destroyed || t.Map == null)
                {
                    continue;
                }
                if (t.Map.designationManager.DesignationOn(t, DesignationDefOf.Deconstruct) != null)
                {
                    continue;
                }
                t.Map.designationManager.AddDesignation(new Designation(t, DesignationDefOf.Deconstruct));
                n++;
            }
            Messages.Message("Azrael_Removal_Designated".Translate(n, report.Display), MessageTypeDefOf.TaskCompletion);
        }

        private static bool Owns(Def def, string packageId)
        {
            if (def?.modContentPack == null || string.IsNullOrEmpty(packageId))
            {
                return false;
            }
            string id = def.modContentPack.PackageId;
            string player = def.modContentPack.PackageIdPlayerFacing;
            return (!string.IsNullOrEmpty(id) && string.Equals(id, packageId, System.StringComparison.OrdinalIgnoreCase))
                || (!string.IsNullOrEmpty(player) && string.Equals(player, packageId, System.StringComparison.OrdinalIgnoreCase));
        }

        private static void ScanMap(Map map, string packageId, RemovalReport report)
        {
            List<Thing> buildings = map.listerThings.ThingsInGroup(ThingRequestGroup.BuildingArtificial);
            Dictionary<string, RemovalLine> buildingCounts = new Dictionary<string, RemovalLine>();
            for (int i = 0; i < buildings.Count; i++)
            {
                Thing t = buildings[i];
                if (t?.def == null || !Owns(t.def, packageId))
                {
                    continue;
                }
                string key = t.def.defName;
                if (!buildingCounts.TryGetValue(key, out RemovalLine line))
                {
                    line = new RemovalLine
                    {
                        Label = t.def.LabelCap,
                        Verdict = RemovalVerdict.Deconstruct,
                        Jump = t,
                        CanDeconstruct = true,
                    };
                }
                line.Count++;
                buildingCounts[key] = line;
                report.Deconstructables.Add(t);
            }
            foreach (RemovalLine line in buildingCounts.Values)
            {
                report.Lines.Add(line);
            }

            List<Thing> items = map.listerThings.ThingsInGroup(ThingRequestGroup.HaulableEver);
            Dictionary<string, RemovalLine> itemCounts = new Dictionary<string, RemovalLine>();
            for (int i = 0; i < items.Count; i++)
            {
                Thing t = items[i];
                if (t?.def == null || t.def.category != ThingCategory.Item || !Owns(t.def, packageId))
                {
                    continue;
                }
                string key = t.def.defName;
                if (!itemCounts.TryGetValue(key, out RemovalLine line))
                {
                    line = new RemovalLine
                    {
                        Label = t.def.LabelCap,
                        Verdict = RemovalVerdict.ConsumeOrSell,
                        Jump = t,
                    };
                }
                line.Count += t.stackCount;
                itemCounts[key] = line;
            }
            foreach (RemovalLine line in itemCounts.Values)
            {
                report.Lines.Add(line);
            }
        }

        private static void ScanCaravan(Caravan caravan, string packageId, RemovalReport report)
        {
            if (caravan?.AllThings == null)
            {
                return;
            }
            foreach (Thing t in caravan.AllThings)
            {
                if (t?.def == null || !Owns(t.def, packageId))
                {
                    continue;
                }
                report.Lines.Add(new RemovalLine
                {
                    Label = "Azrael_Removal_Caravan".Translate(caravan.LabelCap, t.LabelCap),
                    Count = t.stackCount,
                    Verdict = RemovalVerdict.ConsumeOrSell,
                    Jump = caravan,
                });
            }
        }

        private static void AddPawnState(string packageId, RemovalReport report)
        {
            if (packageId.IndexOf("Nemesis", System.StringComparison.OrdinalIgnoreCase) >= 0
                && HuntActive())
            {
                report.Blockers.Add("Azrael_Removal_NemesisHunt".Translate());
                report.Lines.Add(new RemovalLine
                {
                    Label = "Azrael_Removal_NemesisHunt".Translate(),
                    Count = 1,
                    Verdict = RemovalVerdict.ResolveFirst,
                });
            }

            if (packageId.IndexOf("Strata", System.StringComparison.OrdinalIgnoreCase) >= 0)
            {
                int onSub = 0;
                List<Map> maps = Find.Maps;
                for (int i = 0; i < maps.Count; i++)
                {
                    Map map = maps[i];
                    if (map?.Parent is PocketMapParent)
                    {
                        onSub += map.mapPawns.FreeColonistsSpawned.Count;
                    }
                }
                if (onSub > 0)
                {
                    report.Blockers.Add("Azrael_Removal_StrataPawns".Translate(onSub));
                    report.Lines.Add(new RemovalLine
                    {
                        Label = "Azrael_Removal_StrataPawns".Translate(onSub),
                        Count = onSub,
                        Verdict = RemovalVerdict.ResolveFirst,
                    });
                }
            }

            if (packageId.IndexOf("DeepColony", System.StringComparison.OrdinalIgnoreCase) >= 0)
            {
                report.Lines.Add(new RemovalLine
                {
                    Label = "Azrael_Removal_DeepHediffs".Translate(),
                    Count = 1,
                    Verdict = RemovalVerdict.AutoSafe,
                });
            }
        }

        private static bool HuntActive()
        {
            System.Type t = AccessTools.TypeByName("Nemesis.GameComponent_Nemesis");
            if (t == null || Current.Game == null)
            {
                return false;
            }
            object inst = Current.Game.GetComponent(t);
            if (inst == null)
            {
                return false;
            }
            object data = AccessTools.Field(t, "data")?.GetValue(inst)
                ?? AccessTools.Property(t, "Data")?.GetValue(inst);
            if (data == null)
            {
                return false;
            }
            object active = AccessTools.Field(data.GetType(), "active")?.GetValue(data);
            return active is bool b && b;
        }
    }

    internal class Dialog_RemovalPrep : Window
    {
        private readonly RemovalReport report;
        private Vector2 scroll;

        public override Vector2 InitialSize => new Vector2(640f, 520f);

        public Dialog_RemovalPrep(RemovalReport report)
        {
            this.report = report;
            doCloseX = true;
            absorbInputAroundWindow = true;
        }

        public override void DoWindowContents(Rect inRect)
        {
            Text.Font = GameFont.Medium;
            Widgets.Label(new Rect(inRect.x, inRect.y, inRect.width, 28f),
                "Azrael_Removal_Title".Translate(report.Display));
            Text.Font = GameFont.Small;
            float y = inRect.y + 32f;
            Widgets.Label(new Rect(inRect.x, y, inRect.width, 40f), "Azrael_Removal_Intro".Translate());
            y += 44f;

            Rect list = new Rect(inRect.x, y, inRect.width, inRect.height - y - 40f);
            float viewH = 8f + report.Lines.Count * 26f + report.Blockers.Count * 22f;
            Widgets.BeginScrollView(list, ref scroll, new Rect(0f, 0f, list.width - 16f, viewH));
            float ly = 0f;
            for (int i = 0; i < report.Lines.Count; i++)
            {
                RemovalLine line = report.Lines[i];
                string verdict = VerdictLabel(line.Verdict);
                Rect row = new Rect(0f, ly, list.width - 80f, 24f);
                Widgets.Label(row, line.Label + " ×" + line.Count + "  —  " + verdict);
                if (line.Jump.IsValid && Widgets.ButtonText(new Rect(list.width - 76f, ly, 60f, 22f),
                        "Azrael_Removal_Jump".Translate()))
                {
                    CameraJumper.TryJumpAndSelect(line.Jump);
                }
                ly += 26f;
            }
            for (int i = 0; i < report.Blockers.Count; i++)
            {
                GUI.color = new Color(0.9f, 0.4f, 0.35f);
                Widgets.Label(new Rect(0f, ly, list.width, 22f), report.Blockers[i]);
                GUI.color = Color.white;
                ly += 22f;
            }
            if (report.Lines.Count == 0 && report.Blockers.Count == 0)
            {
                Widgets.Label(new Rect(0f, ly, list.width, 24f), "Azrael_Removal_Empty".Translate());
            }
            Widgets.EndScrollView();

            Rect btn = new Rect(inRect.x, inRect.yMax - 32f, 280f, 28f);
            if (Widgets.ButtonText(btn, "Azrael_Removal_Deconstruct".Translate()))
            {
                if (report.Blockers.Count > 0)
                {
                    Messages.Message("Azrael_Removal_Blocked".Translate(), MessageTypeDefOf.RejectInput);
                }
                else
                {
                    SeriesRemoval.DesignateDeconstruct(report);
                    Close();
                }
            }
        }

        private static string VerdictLabel(RemovalVerdict v)
        {
            switch (v)
            {
                case RemovalVerdict.Deconstruct:
                    return "Azrael_Removal_V_Deconstruct".Translate();
                case RemovalVerdict.ConsumeOrSell:
                    return "Azrael_Removal_V_Sell".Translate();
                case RemovalVerdict.ResolveFirst:
                    return "Azrael_Removal_V_Resolve".Translate();
                default:
                    return "Azrael_Removal_V_Safe".Translate();
            }
        }
    }
}
