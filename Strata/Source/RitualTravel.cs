using System.Collections.Generic;
using System.Linq;
using System.Reflection;
using System.Reflection.Emit;
using HarmonyLib;
using RimWorld;
using RimWorld.Planet;
using Verse;
using Verse.AI;
using Verse.AI.Group;

namespace Strata
{
    // Cross-level rituals, in three pieces that keep the vanilla lord system
    // strictly one-map:
    //
    // 1. The ritual dialog's candidate pool includes free colonists from every
    //    linked level, so you don't have to gather the colony on one floor
    //    before opening the menu.
    // 2. When the ritual starts, participants on other levels are kept OUT of
    //    the ritual lord (a lord with pawns on mixed maps breaks duties), but
    //    they keep their role assignments.
    // 3. A world component walks them through the stairwells hop by hop and
    //    joins them to the lord the moment they arrive - roles intact, since
    //    the lord's assignments still list them.
    public static class RitualTravelUtility
    {
        private static readonly AccessTools.FieldRef<MapPawns, Map> mapField =
            AccessTools.FieldRefAccess<MapPawns, Map>("map");

        // Swapped in for MapPawns.FreeColonistsAndPrisonersSpawned inside the
        // ritual dialog's pool builder. Prisoners and animals stay same-map:
        // nobody escorts them through a stairwell.
        public static List<Pawn> ColonistsAndPrisonersAcrossLevels(MapPawns mapPawns)
        {
            var result = new List<Pawn>(mapPawns.FreeColonistsAndPrisonersSpawned);
            if (StrataMod.Settings != null && !StrataMod.Settings.crossLevelRitualsEnabled)
            {
                return result;
            }
            Map home = mapField(mapPawns);
            if (home == null)
            {
                return result;
            }
            foreach (LevelGraph.LevelLink link in LevelGraph.ReachableLevels(home))
            {
                List<Pawn> colonists = link.map.mapPawns.FreeColonistsSpawned;
                for (int i = 0; i < colonists.Count; i++)
                {
                    result.Add(colonists[i]);
                }
            }
            return result;
        }
    }

    // Widen the ritual dialog's candidate pool to the whole level graph.
    [HarmonyPatch(typeof(Dialog_BeginRitual), nameof(Dialog_BeginRitual.CreateRitualRoleAssignments))]
    public static class Patch_RitualDialog_Candidates
    {
        public static IEnumerable<CodeInstruction> Transpiler(IEnumerable<CodeInstruction> instructions)
        {
            MethodInfo original = AccessTools.PropertyGetter(typeof(MapPawns), nameof(MapPawns.FreeColonistsAndPrisonersSpawned));
            MethodInfo replacement = AccessTools.Method(typeof(RitualTravelUtility), nameof(RitualTravelUtility.ColonistsAndPrisonersAcrossLevels));
            bool patched = false;
            foreach (CodeInstruction instruction in instructions)
            {
                if (!patched && instruction.Calls(original))
                {
                    patched = true;
                    yield return new CodeInstruction(OpCodes.Call, replacement)
                        .WithLabels(instruction.labels).WithBlocks(instruction.blocks);
                    continue;
                }
                yield return instruction;
            }
            if (!patched)
            {
                Log.Warning("[Strata] Could not widen the ritual dialog to other levels (candidate pool call not found).");
            }
        }
    }

    // A ritual lord must never start with pawns from another map - mixed-map
    // lords hand out duties that point at cells the pawn cannot see. Off-level
    // participants join later, when they physically arrive.
    [HarmonyPatch(typeof(LordMaker), nameof(LordMaker.MakeNewLord))]
    public static class Patch_RitualLord_SameMapOnly
    {
        public static void Prefix(LordJob lordJob, Map map, ref IEnumerable<Pawn> startingPawns)
        {
            if (lordJob is LordJob_Ritual && startingPawns != null)
            {
                startingPawns = startingPawns.Where(p => p != null && p.MapHeld == map).ToList();
            }
        }
    }

    // After the ritual actually starts, send the off-level participants
    // walking and remember which lord they belong to.
    [HarmonyPatch(typeof(RitualBehaviorWorker), nameof(RitualBehaviorWorker.TryExecuteOn))]
    public static class Patch_RitualExecute_SendTravelers
    {
        public static void Postfix(TargetInfo target, RitualRoleAssignments assignments)
        {
            if (StrataMod.Settings != null && !StrataMod.Settings.crossLevelRitualsEnabled)
            {
                return;
            }
            if (target.Map == null)
            {
                return;
            }
            Lord lord = null;
            foreach (Lord candidate in target.Map.lordManager.lords)
            {
                if (candidate.LordJob is LordJob_Ritual ritualJob && ritualJob.assignments == assignments)
                {
                    lord = candidate;
                    break;
                }
            }
            if (lord == null)
            {
                return; // the ritual never actually started
            }
            StrataRitualTravel travel = StrataRitualTravel.Get;
            if (travel == null)
            {
                return;
            }
            foreach (Pawn pawn in assignments.Participants)
            {
                if (pawn != null && pawn.Spawned && pawn.IsFreeColonist && pawn.MapHeld != target.Map)
                {
                    travel.Register(pawn, target.Map, lord);
                }
            }
        }
    }

    // Walks registered pawns through the stairwells toward their ritual, one
    // portal hop at a time, and joins them to the ritual lord on arrival.
    // Everything degrades safely: if the ritual ends, the player drafts the
    // pawn, or the route breaks, the entry is dropped and normal AI (and the
    // level relays) take over.
    public class StrataRitualTravel : WorldComponent
    {
        private class Traveler : IExposable
        {
            public Pawn pawn;
            public Map destination;
            public Lord lord;
            public int started;

            public void ExposeData()
            {
                Scribe_References.Look(ref pawn, "pawn");
                Scribe_References.Look(ref destination, "destination");
                Scribe_References.Look(ref lord, "lord");
                Scribe_Values.Look(ref started, "started");
            }
        }

        private const int CheckInterval = 60;
        private const int TimeoutTicks = 20000; // ~8 in-game hours

        private List<Traveler> travelers = new List<Traveler>();

        public static StrataRitualTravel Get => Find.World?.GetComponent<StrataRitualTravel>();

        public StrataRitualTravel(World world) : base(world)
        {
        }

        public override void ExposeData()
        {
            base.ExposeData();
            Scribe_Collections.Look(ref travelers, "strataRitualTravelers", LookMode.Deep);
            if (Scribe.mode == LoadSaveMode.PostLoadInit)
            {
                travelers ??= new List<Traveler>();
                travelers.RemoveAll(t => t?.pawn == null);
            }
        }

        public void Register(Pawn pawn, Map destination, Lord lord)
        {
            travelers.RemoveAll(t => t.pawn == pawn);
            var traveler = new Traveler
            {
                pawn = pawn,
                destination = destination,
                lord = lord,
                started = Find.TickManager.TicksGame,
            };
            travelers.Add(traveler);
            TryAdvance(traveler);
        }

        public override void WorldComponentTick()
        {
            base.WorldComponentTick();
            if (travelers.Count == 0 || Find.TickManager.TicksGame % CheckInterval != 0)
            {
                return;
            }
            for (int i = travelers.Count - 1; i >= 0; i--)
            {
                Traveler t = travelers[i];
                if (t.pawn == null || t.pawn.Dead || t.pawn.Destroyed
                    || t.pawn.Drafted
                    || Find.TickManager.TicksGame - t.started > TimeoutTicks
                    || !LordAlive(t))
                {
                    travelers.RemoveAt(i);
                    continue;
                }
                if (!t.pawn.Spawned)
                {
                    continue; // mid-transit through a portal this very tick
                }
                if (t.pawn.Map == t.destination)
                {
                    if (!t.lord.ownedPawns.Contains(t.pawn) && t.lord.CanAddPawn(t.pawn))
                    {
                        t.lord.AddPawn(t.pawn);
                    }
                    travelers.RemoveAt(i);
                    continue;
                }
                TryAdvance(t);
            }
        }

        private static bool LordAlive(Traveler t)
        {
            return t.destination != null && t.lord != null
                && Find.Maps.Contains(t.destination)
                && t.destination.lordManager.lords.Contains(t.lord);
        }

        private static void TryAdvance(Traveler t)
        {
            Pawn pawn = t.pawn;
            if (!pawn.Spawned || pawn.CurJobDef == JobDefOf.EnterPortal)
            {
                return;
            }
            foreach (LevelGraph.LevelLink link in LevelGraph.ReachableLevels(pawn.Map))
            {
                if (link.map != t.destination)
                {
                    continue;
                }
                if (link.firstStep.Spawned && link.firstStep.IsEnterable(out _)
                    && pawn.CanReach(link.firstStep, PathEndMode.Touch, Danger.Some))
                {
                    Job job = JobMaker.MakeJob(JobDefOf.EnterPortal, link.firstStep);
                    pawn.jobs.TryTakeOrderedJob(job, JobTag.Misc);
                }
                return;
            }
        }
    }
}
