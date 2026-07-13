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
    // 1. The ritual dialog's candidate pool includes free colonists, prisoners,
    //    and colony animals from every linked level.
    // 2. When the ritual starts, participants on other levels are kept OUT of
    //    the ritual lord (a lord with pawns on mixed maps breaks duties), but
    //    they keep their role assignments.
    // 3. A world component walks colonists through stairwells hop by hop;
    //    prisoners and animals are escorted by a warden, handler, or bonded
    //    master on the same map. Everyone joins the lord on arrival.
    public static class RitualTravelUtility
    {
        private static readonly AccessTools.FieldRef<MapPawns, Map> mapField =
            AccessTools.FieldRefAccess<MapPawns, Map>("map");

        // Swapped in for MapPawns.FreeColonistsAndPrisonersSpawned inside the
        // ritual dialog's pool builder.
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
            AddLinkedPawns(home, result);
            return result;
        }

        internal static void AddLinkedPawns(Map home, List<Pawn> result)
        {
            var seen = new HashSet<Pawn>(result);
            foreach (LevelGraph.LevelLink link in LevelGraph.ReachableLevels(home))
            {
                Map map = link.map;
                List<Pawn> colonists = map.mapPawns.FreeColonistsSpawned;
                for (int i = 0; i < colonists.Count; i++)
                {
                    TryAdd(seen, result, colonists[i]);
                }
                List<Pawn> prisoners = map.mapPawns.PrisonersOfColony;
                for (int i = 0; i < prisoners.Count; i++)
                {
                    TryAdd(seen, result, prisoners[i]);
                }
                List<Pawn> animals = map.mapPawns.SpawnedPawnsInFaction(Faction.OfPlayer);
                for (int i = 0; i < animals.Count; i++)
                {
                    Pawn animal = animals[i];
                    if (animal.RaceProps.Animal)
                    {
                        TryAdd(seen, result, animal);
                    }
                }
            }
        }

        private static void TryAdd(HashSet<Pawn> seen, List<Pawn> result, Pawn pawn)
        {
            if (pawn != null && seen.Add(pawn))
            {
                result.Add(pawn);
            }
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
                if (pawn == null || !pawn.Spawned || pawn.MapHeld == target.Map)
                {
                    continue;
                }
                if (pawn.IsFreeColonist)
                {
                    travel.Register(pawn, target.Map, lord, assignments);
                }
                else if (RitualEscortUtility.NeedsEscort(pawn))
                {
                    travel.RegisterEscorted(pawn, target.Map, lord, assignments);
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
            public Pawn escort;
            public int started;
            public bool escorted;

            public void ExposeData()
            {
                Scribe_References.Look(ref pawn, "pawn");
                Scribe_References.Look(ref destination, "destination");
                Scribe_References.Look(ref lord, "lord");
                Scribe_References.Look(ref escort, "escort");
                Scribe_Values.Look(ref started, "started");
                Scribe_Values.Look(ref escorted, "escorted");
            }
        }

        private const int CheckInterval = 60;
        private const int TimeoutTicks = 20000; // ~8 in-game hours

        private List<Traveler> travelers = new List<Traveler>();

        // Transient: which ritual assignments an escorted pawn belongs to.
        private readonly Dictionary<Pawn, RitualRoleAssignments> escortAssignments = new Dictionary<Pawn, RitualRoleAssignments>();

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
                escortAssignments.Clear();
            }
        }

        public void Register(Pawn pawn, Map destination, Lord lord, RitualRoleAssignments assignments = null)
        {
            travelers.RemoveAll(t => t.pawn == pawn);
            var traveler = new Traveler
            {
                pawn = pawn,
                destination = destination,
                lord = lord,
                started = Find.TickManager.TicksGame,
                escorted = false,
            };
            travelers.Add(traveler);
            if (assignments != null)
            {
                escortAssignments[pawn] = assignments;
            }
            TryAdvance(traveler);
        }

        public void RegisterEscorted(Pawn pawn, Map destination, Lord lord, RitualRoleAssignments assignments)
        {
            travelers.RemoveAll(t => t.pawn == pawn);
            var traveler = new Traveler
            {
                pawn = pawn,
                destination = destination,
                lord = lord,
                started = Find.TickManager.TicksGame,
                escorted = true,
            };
            travelers.Add(traveler);
            if (assignments != null)
            {
                escortAssignments[pawn] = assignments;
            }
            TryAdvanceEscorted(traveler);
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
                    DropTraveler(t);
                    travelers.RemoveAt(i);
                    continue;
                }
                if (!t.pawn.Spawned)
                {
                    continue; // mid-transit through a portal this very tick
                }
                if (t.pawn.Map == t.destination)
                {
                    JoinLord(t);
                    DropTraveler(t);
                    travelers.RemoveAt(i);
                    continue;
                }
                if (t.escorted)
                {
                    TryAdvanceEscorted(t);
                }
                else
                {
                    TryAdvance(t);
                }
            }
        }

        private static void JoinLord(Traveler t)
        {
            if (!t.lord.ownedPawns.Contains(t.pawn) && t.lord.CanAddPawn(t.pawn))
            {
                t.lord.AddPawn(t.pawn);
            }
        }

        private void DropTraveler(Traveler t)
        {
            if (t.pawn != null)
            {
                escortAssignments.Remove(t.pawn);
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
            MapPortal step = LevelGraph.BestFirstStep(pawn.Map, t.destination, pawn.Position);
            if (step != null && step.Spawned && step.IsEnterable(out _)
                && pawn.CanReach(step, PathEndMode.Touch, Danger.Some))
            {
                Job job = JobMaker.MakeJob(JobDefOf.EnterPortal, step);
                pawn.jobs.TryTakeOrderedJob(job, JobTag.Misc);
            }
        }

        private void TryAdvanceEscorted(Traveler t)
        {
            Pawn escortee = t.pawn;
            if (!escortee.Spawned)
            {
                return;
            }
            if (escortee.CurJobDef == JobDefOf.EnterPortal)
            {
                return;
            }
            escortAssignments.TryGetValue(escortee, out RitualRoleAssignments assignments);
            if (t.escort == null || !t.escort.Spawned || t.escort.Dead || t.escort.Map != escortee.Map)
            {
                t.escort = RitualEscortUtility.FindEscort(escortee, escortee.Map, assignments);
            }
            if (t.escort == null)
            {
                return;
            }
            MapPortal step = LevelGraph.BestFirstStep(escortee.Map, t.destination, escortee.Position);
            if (step == null || !step.Spawned || !step.IsEnterable(out _))
            {
                return;
            }
            if (TryEnterPortalTogether(t.escort, escortee, step))
            {
                return;
            }
            RitualEscortUtility.TryStartEscortHop(t.escort, escortee, step);
        }

        private static bool TryEnterPortalTogether(Pawn escort, Pawn escortee, MapPortal portal)
        {
            if (escort == null || escortee == null || portal == null || !portal.Spawned)
            {
                return false;
            }
            if (escort.Map != escortee.Map || escort.Map != portal.Map)
            {
                return false;
            }
            if (escort.CurJobDef == JobDefOf.EnterPortal || escortee.CurJobDef == JobDefOf.EnterPortal)
            {
                return true;
            }
            if (!escort.CanReach(portal, PathEndMode.Touch, Danger.Some))
            {
                return false;
            }
            if (!escort.Position.InHorDistOf(portal.Position, 2.5f)
                || !escortee.Position.InHorDistOf(portal.Position, 3f))
            {
                return false;
            }
            escortee.jobs.StartJob(JobMaker.MakeJob(JobDefOf.EnterPortal, portal), JobCondition.InterruptForced);
            escort.jobs.StartJob(JobMaker.MakeJob(JobDefOf.EnterPortal, portal), JobCondition.InterruptForced);
            return true;
        }
    }
}
