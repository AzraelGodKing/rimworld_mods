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
            AddWorldState(needle, report);
            return report;
        }

        public static void DesignateDeconstruct(RemovalReport report)
        {
            if (report?.Deconstructables == null)
            {
                return;
            }
            AzraelPlayerCommand.EnqueueDeconstruct(report.Deconstructables, report.Display);
            Messages.Message("Azrael_Removal_Queued".Translate(), MessageTypeDefOf.NeutralEvent, historical: false);
        }

        internal static int ApplyDeconstruct(List<Thing> things)
        {
            int n = 0;
            for (int i = 0; i < things.Count; i++)
            {
                Thing t = things[i];
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
            return n;
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
                int hediffs = CountOwnedHediffs(packageId);
                if (hediffs > 0)
                {
                    report.Lines.Add(new RemovalLine
                    {
                        Label = "Azrael_Removal_DeepHediffs".Translate(),
                        Count = hediffs,
                        Verdict = RemovalVerdict.AutoSafe,
                    });
                }
            }
        }

        private static void AddWorldState(string packageId, RemovalReport report)
        {
            if (packageId.IndexOf("Nemesis", System.StringComparison.OrdinalIgnoreCase) >= 0
                && NemesisWorldPawnParked())
            {
                // HuntActive already blocks when engaged; parked pawn without engagement is rare.
                if (!HuntActive())
                {
                    report.Lines.Add(new RemovalLine
                    {
                        Label = "Azrael_Removal_NemesisWorldPawn".Translate(),
                        Count = 1,
                        Verdict = RemovalVerdict.AutoSafe,
                    });
                }
            }

            if (packageId.IndexOf("DeepColony", System.StringComparison.OrdinalIgnoreCase) >= 0)
            {
                int letters = DeepColonyFamilyLetterCount();
                if (letters > 0)
                {
                    report.Lines.Add(new RemovalLine
                    {
                        Label = "Azrael_Removal_DeepLetters".Translate(),
                        Count = letters,
                        Verdict = RemovalVerdict.AutoSafe,
                    });
                }
            }
        }

        private static int CountOwnedHediffs(string packageId)
        {
            int n = 0;
            var seen = new HashSet<Pawn>();
            List<Map> maps = Find.Maps;
            for (int m = 0; m < maps.Count; m++)
            {
                Map map = maps[m];
                if (map?.mapPawns?.AllPawns == null)
                {
                    continue;
                }
                n += CountPawnsHediffs(map.mapPawns.AllPawns, packageId, seen);
            }

            List<Caravan> caravans = Find.WorldObjects.Caravans;
            for (int c = 0; c < caravans.Count; c++)
            {
                Caravan caravan = caravans[c];
                if (caravan?.PawnsListForReading == null)
                {
                    continue;
                }
                n += CountPawnsHediffs(caravan.PawnsListForReading, packageId, seen);
            }

            // Kidnapped colonists, quest lodgers, and other off-map pawns live here.
            if (Find.WorldPawns != null)
            {
                n += CountPawnsHediffs(Find.WorldPawns.AllPawnsAlive, packageId, seen);
            }

            return n;
        }

        private static int CountPawnsHediffs(IEnumerable<Pawn> pawns, string packageId, HashSet<Pawn> seen)
        {
            int n = 0;
            foreach (Pawn pawn in pawns)
            {
                if (pawn != null && seen.Add(pawn))
                {
                    n += CountPawnHediffs(pawn, packageId);
                }
            }
            return n;
        }

        private static int CountPawnHediffs(Pawn pawn, string packageId)
        {
            if (pawn?.health?.hediffSet?.hediffs == null)
            {
                return 0;
            }
            int n = 0;
            List<Hediff> hediffs = pawn.health.hediffSet.hediffs;
            for (int i = 0; i < hediffs.Count; i++)
            {
                Hediff h = hediffs[i];
                if (h?.def != null && Owns(h.def, packageId))
                {
                    n++;
                }
            }
            return n;
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

            // Prefer IsEngaged (active hunt OR truce window).
            object engaged = AccessTools.Property(t, "IsEngaged")?.GetValue(inst);
            if (engaged is bool e)
            {
                return e;
            }

            object data = AccessTools.Field(t, "data")?.GetValue(inst)
                ?? AccessTools.Property(t, "Data")?.GetValue(inst);
            if (data == null)
            {
                return false;
            }
            object active = AccessTools.Field(data.GetType(), "active")?.GetValue(data);
            if (active is bool b && b)
            {
                return true;
            }
            object truce = AccessTools.Field(data.GetType(), "truceUntilTick")?.GetValue(data);
            return truce is int tick && tick > 0;
        }

        private static bool NemesisWorldPawnParked()
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
            object pawn = AccessTools.Method(t, "FindNemesisPawn")?.Invoke(inst, null);
            return pawn is Pawn p && !p.Destroyed && !p.Dead;
        }

        private static int DeepColonyFamilyLetterCount()
        {
            try
            {
                System.Type t = AccessTools.TypeByName("DeepColony.GameComp_DeepColony");
                if (t == null || Current.Game == null)
                {
                    return 0;
                }
                object inst = AccessTools.Property(t, "Instance")?.GetValue(null)
                    ?? Current.Game.GetComponent(t);
                if (inst == null)
                {
                    return 0;
                }
                object letters = AccessTools.Field(t, "familyLetters")?.GetValue(inst);
                if (letters is System.Collections.ICollection coll)
                {
                    return coll.Count;
                }
            }
            catch
            {
            }
            return 0;
        }
    }

    internal class Dialog_RemovalPrep : Window
    {
        private readonly RemovalReport report;
        private Vector2 scroll;
        private string summary;

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
            if (report.Lines.Count > 0)
            {
                GUI.color = report.Blockers.Count > 0 ? SeriesHub.ConflictColor : Color.white;
                Widgets.Label(new Rect(inRect.x, y, inRect.width, 24f), summary ?? (summary = Summary(report)));
                GUI.color = Color.white;
                y += 26f;
            }

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

        private static string Summary(RemovalReport report)
        {
            int deconstruct = 0;
            int sell = 0;
            int attention = 0;
            int safe = 0;
            for (int i = 0; i < report.Lines.Count; i++)
            {
                RemovalLine line = report.Lines[i];
                switch (line.Verdict)
                {
                    case RemovalVerdict.Deconstruct:
                        deconstruct += line.Count;
                        break;
                    case RemovalVerdict.ConsumeOrSell:
                        sell += line.Count;
                        break;
                    case RemovalVerdict.ResolveFirst:
                        attention++;
                        break;
                    default:
                        safe++;
                        break;
                }
            }
            return "Azrael_Removal_Summary".Translate(deconstruct, sell, attention, safe);
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
