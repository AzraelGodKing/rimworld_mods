using System.Collections.Generic;
using RimWorld;
using RimWorld.Planet;
using UnityEngine;
using Verse;

namespace Strata
{
    // Rough three-step reading the seismograph gives for ore and gas.
    public enum DeepSurveyGrade : byte
    {
        Low,
        Moderate,
        High,
    }

    // One finished survey: what the next level dug under parentMapId will be.
    public class DeepSurveyRecord : IExposable
    {
        public int parentMapId = -1;
        public int depth;
        public StrataLevelKind kind;
        public DeepSurveyGrade ore = DeepSurveyGrade.Moderate;
        public DeepSurveyGrade gas = DeepSurveyGrade.Moderate;

        public void ExposeData()
        {
            Scribe_Values.Look(ref parentMapId, "parentMapId", -1);
            Scribe_Values.Look(ref depth, "depth");
            Scribe_Values.Look(ref kind, "kind", StrataLevelKind.Standard);
            Scribe_Values.Look(ref ore, "ore", DeepSurveyGrade.Moderate);
            Scribe_Values.Look(ref gas, "gas", DeepSurveyGrade.Moderate);
        }
    }

    // Holds the locked surveys, one per colony stack (keyed by the deepest
    // level, the map the next dig starts from). Old saves have none.
    public class GameComponent_StrataDeepSurvey : GameComponent
    {
        private List<DeepSurveyRecord> records = new List<DeepSurveyRecord>();

        public GameComponent_StrataDeepSurvey(Game game)
        {
        }

        internal static GameComponent_StrataDeepSurvey Get => Current.Game?.GetComponent<GameComponent_StrataDeepSurvey>();

        internal DeepSurveyRecord For(int parentMapId)
        {
            for (int i = 0; i < records.Count; i++)
            {
                if (records[i].parentMapId == parentMapId)
                {
                    return records[i];
                }
            }
            return null;
        }

        internal void Set(DeepSurveyRecord record)
        {
            Remove(record.parentMapId);
            records.Add(record);
        }

        internal void Remove(int parentMapId)
        {
            records.RemoveAll(r => r.parentMapId == parentMapId);
        }

        public override void LoadedGame()
        {
            base.LoadedGame();
            records.RemoveAll(r => DeepSurvey.MapById(r.parentMapId) == null);
        }

        public override void ExposeData()
        {
            base.ExposeData();
            Scribe_Collections.Look(ref records, "strataDeepSurveys", LookMode.Deep);
            if (Scribe.mode == LoadSaveMode.PostLoadInit)
            {
                records ??= new List<DeepSurveyRecord>();
                records.RemoveAll(r => r == null);
            }
        }
    }

    // Deep Survey: a researcher at a powered seismograph reads the rock under
    // the deepest level of its stack. The roll is locked, and the next dig
    // there uses it (level kind, rich ore node count, gas chamber count).
    public static class DeepSurvey
    {
        public const float WorkAmount = 2500f;

        // Claimed by GenStep_StrataLevelKind, read by the later gen steps
        // (some of which run after the deferred rock fill).
        private static readonly Dictionary<int, DeepSurveyRecord> claimedByMapId =
            new Dictionary<int, DeepSurveyRecord>();

        public static void ResetSession()
        {
            claimedByMapId.Clear();
        }

        internal static Map MapById(int id)
        {
            List<Map> maps = Find.Maps;
            for (int i = 0; i < maps.Count; i++)
            {
                if (maps[i].uniqueID == id)
                {
                    return maps[i];
                }
            }
            return null;
        }

        // The map the next level of this map's colony stack would be dug from:
        // the deepest dig-down level, or the surface when nothing is dug yet.
        public static Map TargetFor(Map here, out string reason)
        {
            reason = null;
            Map surface = here != null && StrataMapUtility.IsSurfacePlayerHome(here)
                ? here
                : SurfaceHomeOf(here);
            if (surface == null)
            {
                reason = "Strata_DeepSurvey_NoStack".Translate();
                return null;
            }
            Map deepest = surface;
            int deepestDepth = 0;
            foreach (LevelGraph.LevelLink link in LevelGraph.ReachableLevels(surface))
            {
                Map level = link.map;
                if (level == null
                    || level.generatorDef?.defName != StrataMapUtility.UndergroundGenerator
                    || StrataGravshipUtility.FindGravshipStackRoot(level) != null)
                {
                    continue;
                }
                int depth = StrataDepth.Of(level);
                if (depth > deepestDepth)
                {
                    deepest = level;
                    deepestDepth = depth;
                }
            }
            if (LevelExcavationUtility.MapHasOpenLevelBelow(deepest))
            {
                reason = "Strata_DeepSurvey_NoStack".Translate();
                return null;
            }
            if (!StrataLevelCap.AllowsNewLevelBelow(deepest, out string capReason))
            {
                reason = capReason.NullOrEmpty() ? "Strata_DeepSurvey_Bottom".Translate().ToString() : capReason;
                return null;
            }
            return deepest;
        }

        private static Map SurfaceHomeOf(Map map)
        {
            if (map == null)
            {
                return null;
            }
            foreach (LevelGraph.LevelLink link in LevelGraph.ReachableLevels(map))
            {
                if (StrataMapUtility.IsSurfacePlayerHome(link.map))
                {
                    return link.map;
                }
            }
            return null;
        }

        // The locked survey for the level below parent, or null. Drops a record
        // once a level below exists (dug by a generator that skipped the claim).
        public static DeepSurveyRecord For(Map parent)
        {
            GameComponent_StrataDeepSurvey store = GameComponent_StrataDeepSurvey.Get;
            DeepSurveyRecord record = parent != null ? store?.For(parent.uniqueID) : null;
            if (record == null)
            {
                return null;
            }
            if (record.depth != StrataDepth.Of(parent) + 1 || LevelExcavationUtility.MapHasOpenLevelBelow(parent))
            {
                store.Remove(parent.uniqueID);
                return null;
            }
            return record;
        }

        internal static DeepSurveyRecord Roll(Map parent)
        {
            int depth = StrataDepth.Of(parent) + 1;
            var record = new DeepSurveyRecord
            {
                parentMapId = parent.uniqueID,
                depth = depth,
                kind = StrataLevelKinds.PickForDepth(depth),
                ore = RollOre(),
                gas = RollGas(depth),
            };
            GameComponent_StrataDeepSurvey.Get?.Set(record);
            return record;
        }

        private static DeepSurveyGrade RollOre()
        {
            float r = Rand.Value;
            return r < 0.25f ? DeepSurveyGrade.Low : r < 0.75f ? DeepSurveyGrade.Moderate : DeepSurveyGrade.High;
        }

        // Same odds as GenStep_HiddenChambers: B1 has no gas chambers.
        private static DeepSurveyGrade RollGas(int depth)
        {
            if (depth <= 1 || !Rand.Chance(Mathf.Min(0.5f + 0.15f * (depth - 1), 0.95f)))
            {
                return DeepSurveyGrade.Low;
            }
            return Rand.Chance(0.6f) ? DeepSurveyGrade.Moderate : DeepSurveyGrade.High;
        }

        // Called from the level-kind gen step: takes the survey for the map
        // being dug, so it can't be rolled again.
        internal static bool TryClaim(Map map, int depth, out DeepSurveyRecord record)
        {
            record = null;
            Map parent = map.Parent is PocketMapParent pocket && pocket.sourceMap != null
                ? pocket.sourceMap
                : PocketMapUtility.currentlyGeneratingPortal?.Map;
            GameComponent_StrataDeepSurvey store = GameComponent_StrataDeepSurvey.Get;
            DeepSurveyRecord found = parent != null ? store?.For(parent.uniqueID) : null;
            if (found == null || found.depth != depth)
            {
                return false;
            }
            store.Remove(parent.uniqueID);
            claimedByMapId[map.uniqueID] = found;
            record = found;
            return true;
        }

        internal static int AdjustOreNodeCount(Map map, int count)
        {
            if (map == null || !claimedByMapId.TryGetValue(map.uniqueID, out DeepSurveyRecord record))
            {
                return count;
            }
            switch (record.ore)
            {
                case DeepSurveyGrade.Low:
                    return 1;
                case DeepSurveyGrade.High:
                    return count + 1;
                default:
                    return count;
            }
        }

        // Gas chamber count for a surveyed level; -1 when not surveyed.
        internal static int GasChamberCount(Map map, int depth)
        {
            if (map == null || !claimedByMapId.TryGetValue(map.uniqueID, out DeepSurveyRecord record))
            {
                return -1;
            }
            int max = Mathf.Min(1 + depth, 4);
            switch (record.gas)
            {
                case DeepSurveyGrade.Low:
                    return 0;
                case DeepSurveyGrade.Moderate:
                    return Rand.RangeInclusive(1, Mathf.Min(2, max));
                default:
                    return Rand.RangeInclusive(Mathf.Min(3, max), max);
            }
        }

        public static string KindLabel(StrataLevelKind kind)
        {
            switch (kind)
            {
                case StrataLevelKind.Magma:
                    return "Strata_DeepSurvey_KindMagma".Translate();
                case StrataLevelKind.Flooded:
                    return "Strata_DeepSurvey_KindFlooded".Translate();
                case StrataLevelKind.LostFloor:
                    return "Strata_DeepSurvey_KindLostFloor".Translate();
                default:
                    return "Strata_DeepSurvey_KindStandard".Translate();
            }
        }

        public static string GradeLabel(DeepSurveyGrade grade)
        {
            switch (grade)
            {
                case DeepSurveyGrade.Low:
                    return "Strata_Seismograph_RiskLow".Translate();
                case DeepSurveyGrade.High:
                    return "Strata_Seismograph_RiskHigh".Translate();
                default:
                    return "Strata_Seismograph_RiskModerate".Translate();
            }
        }

        public static string Describe(DeepSurveyRecord record)
        {
            return "Strata_DeepSurvey_Result".Translate(
                "B" + record.depth,
                KindLabel(record.kind),
                GradeLabel(record.ore),
                GradeLabel(record.gas));
        }

        internal static void Complete(Thing console, Pawn surveyor)
        {
            Map target = TargetFor(console.Map, out _);
            if (target == null)
            {
                return;
            }
            DeepSurveyRecord record = For(target) ?? Roll(target);
            Messages.Message(
                "Strata_DeepSurvey_Done".Translate(surveyor.LabelShort, Describe(record)),
                console,
                MessageTypeDefOf.PositiveEvent);
        }
    }
}
