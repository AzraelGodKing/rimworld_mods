#nullable enable
using System.Collections.Generic;
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
    }

    internal class RemovalReport
    {
        public string Display = "";
        public string PackageId = "";
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

            var scan = new Scanner(packageId ?? string.Empty, report);
            foreach (Map map in Find.Maps)
            {
                if (map != null)
                {
                    scan.Map(map);
                }
            }
            foreach (Caravan caravan in Find.WorldObjects.Caravans)
            {
                scan.Caravan(caravan);
            }
            scan.WorldPawns();
            scan.World();
            scan.Components();
            scan.Flush();

            AddSisterState(packageId ?? string.Empty, report);
            return report;
        }

        public static void DesignateDeconstruct(RemovalReport? report)
        {
            if (report?.Deconstructables == null || report.Deconstructables.Count == 0)
            {
                return;
            }
            var ids = new List<int>(report.Deconstructables.Count);
            foreach (Thing t in report.Deconstructables)
            {
                if (t != null)
                {
                    ids.Add(t.thingIDNumber);
                }
            }
            AzraelPlayerCommand.DesignateDeconstruct(ids.ToArray(), report.Display);
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

        /// <summary>The one ownership test used for every kind of content (AZR-587).</summary>
        internal static bool Owns(Def? def, string packageId)
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

        /// <summary>
        /// One pass over everything a save can hold for a mod: spawned and held things
        /// (minified, blueprints and frames, worn, carried, inside graves, shuttles and
        /// transporters), plants, creatures, floors, conditions, hediffs, memories,
        /// world objects, factions and saved components. Lines are grouped per def and kind.
        /// </summary>
        private sealed class Scanner
        {
            private readonly string packageId;
            private readonly RemovalReport report;
            private readonly Dictionary<string, RemovalLine> lines = new Dictionary<string, RemovalLine>();
            private readonly HashSet<Thing> seen = new HashSet<Thing>();
            private readonly HashSet<Pawn> seenPawns = new HashSet<Pawn>();
            private readonly List<Thing> held = new List<Thing>();
            private int hediffs;
            private int memories;

            internal Scanner(string packageId, RemovalReport report)
            {
                this.packageId = packageId;
                this.report = report;
            }

            internal void Map(Map map)
            {
                foreach (Thing t in map.listerThings.AllThings)
                {
                    Spawned(t);
                }

                TerrainGrid? terrain = map.terrainGrid;
                if (terrain != null)
                {
                    var floors = new Dictionary<TerrainDef, int>();
                    foreach (IntVec3 c in map.AllCells)
                    {
                        TerrainDef? top = terrain.TerrainAt(c);
                        if (top != null && Owns(top, packageId))
                        {
                            floors[top] = floors.TryGetValue(top, out int n) ? n + 1 : 1;
                        }
                    }
                    foreach (KeyValuePair<TerrainDef, int> floor in floors)
                    {
                        Add("floor:" + floor.Key.defName, "Azrael_Removal_Floor".Translate(floor.Key.LabelCap),
                            floor.Value, RemovalVerdict.ResolveFirst, new GlobalTargetInfo(map.Center, map));
                    }
                }

                Conditions(map.gameConditionManager);
            }

            internal void Caravan(Caravan caravan)
            {
                if (caravan?.AllThings == null)
                {
                    return;
                }
                foreach (Thing t in caravan.AllThings)
                {
                    if (t is Pawn pawn)
                    {
                        PawnState(pawn);
                    }
                    if (t?.def != null && Owns(t.def, packageId) && seen.Add(t))
                    {
                        Add("caravan:" + caravan.ID + ":" + t.def.defName,
                            "Azrael_Removal_Caravan".Translate(caravan.LabelCap, t.def.LabelCap),
                            t.stackCount, RemovalVerdict.ConsumeOrSell, caravan);
                    }
                }
            }

            // Kidnapped colonists, quest lodgers, and other off-map pawns live here.
            internal void WorldPawns()
            {
                if (Find.WorldPawns == null)
                {
                    return;
                }
                foreach (Pawn pawn in Find.WorldPawns.AllPawnsAlive)
                {
                    PawnState(pawn);
                }
            }

            internal void World()
            {
                foreach (WorldObject o in Find.WorldObjects.AllWorldObjects)
                {
                    if (o?.def != null && Owns(o.def, packageId))
                    {
                        Add("world:" + o.def.defName, "Azrael_Removal_WorldObject".Translate(o.def.LabelCap),
                            1, RemovalVerdict.AutoSafe, o);
                    }
                }
                foreach (Faction f in Find.FactionManager.AllFactionsListForReading)
                {
                    if (f?.def != null && Owns(f.def, packageId))
                    {
                        Add("faction:" + f.loadID, "Azrael_Removal_Faction".Translate(f.Name ?? f.def.LabelCap.ToString()),
                            1, RemovalVerdict.ResolveFirst, GlobalTargetInfo.Invalid);
                    }
                }
                Conditions(Find.World?.gameConditionManager);
            }

            // Game, world and map components from the mod's assembly are dropped on load with a warning.
            internal void Components()
            {
                var names = new List<string>();
                foreach (GameComponent c in Current.Game.components)
                {
                    AddComponent(c, names);
                }
                if (Find.World != null)
                {
                    foreach (WorldComponent c in Find.World.components)
                    {
                        AddComponent(c, names);
                    }
                }
                foreach (Map map in Find.Maps)
                {
                    foreach (MapComponent c in map.components)
                    {
                        AddComponent(c, names);
                    }
                }
                if (names.Count > 0)
                {
                    Add("components", "Azrael_Removal_Components".Translate(string.Join(", ", names)),
                        names.Count, RemovalVerdict.AutoSafe, GlobalTargetInfo.Invalid);
                }
            }

            internal void Flush()
            {
                if (hediffs > 0)
                {
                    Add("hediffs", "Azrael_Removal_Hediffs".Translate(), hediffs, RemovalVerdict.AutoSafe, GlobalTargetInfo.Invalid);
                }
                if (memories > 0)
                {
                    Add("memories", "Azrael_Removal_Memories".Translate(), memories, RemovalVerdict.AutoSafe, GlobalTargetInfo.Invalid);
                }
                report.Lines.AddRange(lines.Values);
            }

            private void Spawned(Thing t)
            {
                if (t?.def == null)
                {
                    return;
                }
                if (t is Pawn pawn)
                {
                    PawnState(pawn);
                }
                Classify(t, t);
                if (t is IThingHolder holder)
                {
                    held.Clear();
                    ThingOwnerUtility.GetAllThingsRecursively(holder, held, allowUnreal: true);
                    foreach (Thing inner in held)
                    {
                        if (inner != null && inner != t)
                        {
                            if (inner is Pawn innerPawn)
                            {
                                PawnState(innerPawn);
                            }
                            Classify(inner, t);
                        }
                    }
                }
            }

            /// <param name="anchor">Spawned thing to jump to (the holder when <paramref name="t"/> is held).</param>
            private void Classify(Thing t, Thing anchor)
            {
                if (!seen.Add(t))
                {
                    return;
                }
                bool isHeld = t != anchor;
                ThingDef def = t.def;

                if (t is MinifiedThing minified)
                {
                    Thing? inner = minified.InnerThing;
                    if (inner?.def != null && Owns(inner.def, packageId))
                    {
                        Add("minified:" + inner.def.defName, "Azrael_Removal_Minified".Translate(inner.def.LabelCap),
                            t.stackCount, RemovalVerdict.ConsumeOrSell, anchor);
                    }
                    return;
                }
                if (t is Blueprint || t is Frame)
                {
                    BuildableDef? built = def.entityDefToBuild;
                    if (built != null && Owns(built, packageId))
                    {
                        Add("plan:" + built.defName, "Azrael_Removal_Blueprint".Translate(built.LabelCap),
                            1, RemovalVerdict.ResolveFirst, anchor);
                    }
                    return;
                }
                if (!Owns(def, packageId))
                {
                    return;
                }
                switch (def.category)
                {
                    case ThingCategory.Building when !isHeld:
                        Add("building:" + def.defName, def.LabelCap, 1, RemovalVerdict.Deconstruct, anchor);
                        report.Deconstructables.Add(t);
                        break;
                    case ThingCategory.Plant:
                        Add("plant:" + def.defName, "Azrael_Removal_Plant".Translate(def.LabelCap),
                            1, RemovalVerdict.ConsumeOrSell, anchor);
                        break;
                    case ThingCategory.Pawn:
                        Add("pawn:" + def.defName, "Azrael_Removal_Creature".Translate(def.LabelCap),
                            1, RemovalVerdict.ResolveFirst, anchor);
                        break;
                    case ThingCategory.Item:
                    case ThingCategory.Building:
                        Add((isHeld ? "held:" : "item:") + def.defName,
                            isHeld ? "Azrael_Removal_Held".Translate(def.LabelCap) : def.LabelCap,
                            t.stackCount, RemovalVerdict.ConsumeOrSell, anchor);
                        break;
                    default:
                        Add("other:" + def.defName, def.LabelCap, t.stackCount, RemovalVerdict.AutoSafe, anchor);
                        break;
                }
            }

            private void PawnState(Pawn pawn)
            {
                if (pawn == null || !seenPawns.Add(pawn))
                {
                    return;
                }
                List<Hediff>? list = pawn.health?.hediffSet?.hediffs;
                if (list != null)
                {
                    foreach (Hediff h in list)
                    {
                        if (h?.def != null && Owns(h.def, packageId))
                        {
                            hediffs++;
                        }
                    }
                }
                List<Thought_Memory>? mems = pawn.needs?.mood?.thoughts?.memories?.Memories;
                if (mems != null)
                {
                    foreach (Thought_Memory m in mems)
                    {
                        if (m?.def != null && Owns(m.def, packageId))
                        {
                            memories++;
                        }
                    }
                }
            }

            private void Conditions(GameConditionManager? manager)
            {
                if (manager?.ActiveConditions == null)
                {
                    return;
                }
                foreach (GameCondition c in manager.ActiveConditions)
                {
                    if (c?.def != null && Owns(c.def, packageId))
                    {
                        Add("condition:" + c.def.defName, "Azrael_Removal_Condition".Translate(c.def.LabelCap),
                            1, RemovalVerdict.AutoSafe, GlobalTargetInfo.Invalid);
                    }
                }
            }

            private void AddComponent(object? component, List<string> names)
            {
                if (component == null)
                {
                    return;
                }
                System.Type type = component.GetType();
                if (OwnsAssembly(type.Assembly) && !names.Contains(type.Name))
                {
                    names.Add(type.Name);
                }
            }

            private bool OwnsAssembly(System.Reflection.Assembly assembly)
            {
                foreach (ModContentPack pack in LoadedModManager.RunningModsListForReading)
                {
                    if (pack != null
                        && (string.Equals(pack.PackageId, packageId, System.StringComparison.OrdinalIgnoreCase)
                            || string.Equals(pack.PackageIdPlayerFacing, packageId, System.StringComparison.OrdinalIgnoreCase)))
                    {
                        return pack.assemblies?.loadedAssemblies?.Contains(assembly) == true;
                    }
                }
                return false;
            }

            private void Add(string key, string label, int count, RemovalVerdict verdict, GlobalTargetInfo jump)
            {
                if (!lines.TryGetValue(key, out RemovalLine line))
                {
                    line = new RemovalLine { Label = label, Verdict = verdict, Jump = jump };
                }
                line.Count += count;
                lines[key] = line;
            }
        }

        private static void AddSisterState(string packageId, RemovalReport report)
        {
            if (packageId.IndexOf("Nemesis", System.StringComparison.OrdinalIgnoreCase) >= 0)
            {
                if (SisterContracts.NemesisEngaged())
                {
                    report.Blockers.Add("Azrael_Removal_NemesisHunt".Translate());
                    report.Lines.Add(new RemovalLine
                    {
                        Label = "Azrael_Removal_NemesisHunt".Translate(),
                        Count = 1,
                        Verdict = RemovalVerdict.ResolveFirst,
                    });
                }
                else if (SisterContracts.NemesisPawnParked())
                {
                    report.Lines.Add(new RemovalLine
                    {
                        Label = "Azrael_Removal_NemesisWorldPawn".Translate(),
                        Count = 1,
                        Verdict = RemovalVerdict.AutoSafe,
                    });
                }
            }

            // Colonists on a map this mod generated (Strata floors), not on any pocket map:
            // Anomaly and Odyssey pocket maps belong to vanilla (AZR-587).
            int onModMap = 0;
            foreach (Map map in Find.Maps)
            {
                if (map != null
                    && (Owns(map.generatorDef, packageId) || Owns(map.Parent?.def, packageId)))
                {
                    onModMap += map.mapPawns.FreeColonistsSpawned.Count;
                }
            }
            if (onModMap > 0)
            {
                report.Blockers.Add("Azrael_Removal_ModMapPawns".Translate(onModMap));
                report.Lines.Add(new RemovalLine
                {
                    Label = "Azrael_Removal_ModMapPawns".Translate(onModMap),
                    Count = onModMap,
                    Verdict = RemovalVerdict.ResolveFirst,
                });
            }

            if (packageId.IndexOf("DeepColony", System.StringComparison.OrdinalIgnoreCase) >= 0)
            {
                int letters = SisterContracts.DeepColonyFamilyLetterCount();
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
    }

    internal class Dialog_RemovalPrep : Window
    {
        private readonly RemovalReport report;
        private Vector2 scroll;
        private string? summary;

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
                else if (report.Deconstructables.Count == 0)
                {
                    Messages.Message("Azrael_Removal_NothingToDeconstruct".Translate(), MessageTypeDefOf.RejectInput, historical: false);
                }
                else
                {
                    // Map-wide and only undone by hand, so confirm first (AZR-690).
                    RemovalReport confirmed = report;
                    Find.WindowStack.Add(Dialog_MessageBox.CreateConfirmation(
                        "Azrael_Removal_Confirm".Translate(confirmed.Deconstructables.Count, confirmed.Display),
                        () =>
                        {
                            SeriesRemoval.DesignateDeconstruct(confirmed);
                            Close();
                        },
                        destructive: true));
                }
            }
        }

        private static string Summary(RemovalReport report)
        {
            int deconstruct = 0;
            int sell = 0;
            int attention = 0;
            int safe = 0;
            foreach (RemovalLine line in report.Lines)
            {
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

        private static string VerdictLabel(RemovalVerdict v) => v switch
        {
            RemovalVerdict.Deconstruct => "Azrael_Removal_V_Deconstruct".Translate(),
            RemovalVerdict.ConsumeOrSell => "Azrael_Removal_V_Sell".Translate(),
            RemovalVerdict.ResolveFirst => "Azrael_Removal_V_Resolve".Translate(),
            _ => "Azrael_Removal_V_Safe".Translate(),
        };
    }
}
