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

        private static readonly List<Map> tmpNetworkMaps = new List<Map>();

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
            ColonyBedUtility.GetColonyNetworkMaps(home, tmpNetworkMaps);
            for (int m = 0; m < tmpNetworkMaps.Count; m++)
            {
                Map map = tmpNetworkMaps[m];
                if (map == null || map == home)
                {
                    continue;
                }
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
                List<Pawn> animals = map.mapPawns.SpawnedColonyAnimals;
                for (int i = 0; i < animals.Count; i++)
                {
                    TryAdd(seen, result, animals[i]);
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

        // Name-based match: MethodInfo reference equality fails across some
        // Harmony / ref-assembly setups even when the IL call is present.
        internal static bool IsFreeColonistsAndPrisonersSpawnedCall(CodeInstruction instruction)
        {
            if (instruction.opcode != OpCodes.Call && instruction.opcode != OpCodes.Callvirt)
            {
                return false;
            }
            var method = instruction.operand as MethodInfo;
            return method != null
                && method.DeclaringType == typeof(MapPawns)
                && method.Name == "get_FreeColonistsAndPrisonersSpawned";
        }

        // Shared by the ritual dialog transpiler.
        internal static IEnumerable<CodeInstruction> TranspileFreeColonistsAndPrisoners(
            IEnumerable<CodeInstruction> instructions,
            string missWarning)
        {
            MethodInfo replacement = AccessTools.Method(
                typeof(RitualTravelUtility),
                nameof(ColonistsAndPrisonersAcrossLevels));
            bool patched = false;
            foreach (CodeInstruction instruction in instructions)
            {
                if (!patched && IsFreeColonistsAndPrisonersSpawnedCall(instruction))
                {
                    patched = true;
                    yield return new CodeInstruction(OpCodes.Call, replacement)
                        .WithLabels(instruction.labels)
                        .WithBlocks(instruction.blocks);
                    continue;
                }

                yield return instruction;
            }

            if (!patched)
            {
                Log.Warning(missWarning);
            }
        }

        // Re-run the required-role gate with the across-level pool. Used when
        // CanStartRitualNow fails with "You need a doctor…" (etc.).
        internal static bool HasRequiredRolesAcrossLevels(
            TargetInfo target,
            Precept_Ritual ritual,
            Dictionary<string, Pawn> forcedForRole)
        {
            if (target.Map == null || ritual?.behavior?.def?.roles == null)
            {
                return false;
            }
            List<Pawn> list = ColonistsAndPrisonersAcrossLevels(target.Map.mapPawns);
            // Local animals are added by vanilla after the colonist list; mirror that
            // for the home map, then linked animals are already in AddLinkedPawns.
            list.AddRange(target.Map.mapPawns.SpawnedColonyAnimals);

            foreach (RitualRole role in ritual.behavior.def.roles)
            {
                if (!role.required || role.substitutable)
                {
                    continue;
                }
                IEnumerable<RitualRole> source = role.mergeId == null
                    ? Gen.YieldSingle(role)
                    : ritual.behavior.def.roles.Where(r => r.mergeId == role.mergeId);
                int needed = source.Count();
                int found = 0;
                for (int i = 0; i < list.Count; i++)
                {
                    if (role.AppliesToPawn(list[i], out _, target, null, null, null, skipReason: true))
                    {
                        found++;
                        if (found >= needed)
                        {
                            break;
                        }
                    }
                }
                if (found < needed
                    && (forcedForRole == null || !forcedForRole.ContainsKey(role.id)))
                {
                    return false;
                }
            }
            return true;
        }

        internal static bool LooksLikeMissingRequiredRole(string reason, Precept_Ritual ritual)
        {
            if (reason.NullOrEmpty() || ritual?.behavior?.def?.roles == null)
            {
                return false;
            }
            foreach (RitualRole role in ritual.behavior.def.roles)
            {
                if (!role.required || role.substitutable)
                {
                    continue;
                }
                if (!role.noCandidatesGizmoDesc.NullOrEmpty()
                    && reason == role.noCandidatesGizmoDesc)
                {
                    return true;
                }
                string article = role.missingDesc
                    ?? Find.ActiveLanguageWorker.WithIndefiniteArticle(role.Label);
                string expected = "MessageNoRequiredRolePawnToBeginRitual"
                    .Translate(article, ritual.Label);
                if (reason == expected || reason == (string)expected)
                {
                    return true;
                }
                Precept precept = ritual.ideo?.PreceptsListForReading
                    ?.FirstOrDefault(p => p.def == role.precept);
                if (precept != null)
                {
                    string needRole = "MessageNeedAssignedRoleToBeginRitual".Translate(
                        role.missingDesc
                            ?? Find.ActiveLanguageWorker.WithIndefiniteArticle(precept.LabelCap),
                        ritual.Label);
                    if (reason == needRole || reason == (string)needRole)
                    {
                        return true;
                    }
                }
                // Loose match for plural/merge-id wording and translation variants.
                if (reason.IndexOf(role.Label, System.StringComparison.OrdinalIgnoreCase) >= 0
                    && reason.IndexOf(ritual.Label, System.StringComparison.OrdinalIgnoreCase) >= 0)
                {
                    return true;
                }
            }
            return false;
        }
    }

    // Widen the ritual dialog's candidate pool to the whole level graph.
    [HarmonyPatch(typeof(Dialog_BeginRitual), nameof(Dialog_BeginRitual.CreateRitualRoleAssignments))]
    public static class Patch_RitualDialog_Candidates
    {
        public static IEnumerable<CodeInstruction> Transpiler(IEnumerable<CodeInstruction> instructions)
        {
            return RitualTravelUtility.TranspileFreeColonistsAndPrisoners(
                instructions,
                "[Strata] Could not widen the ritual dialog to other levels (candidate pool call not found).");
        }
    }

    // Gather-for-childbirth (and other ritual gizmos) call CanStartRitualNow,
    // which only scans the target map for required roles (e.g. doctor). A
    // Postfix re-check is more reliable than IL matching here — when the
    // failure is "need a doctor", clear it if a capable colonist exists on a
    // linked floor. RitualTravel still walks them in after start.
    [HarmonyPatch(typeof(RitualBehaviorWorker), nameof(RitualBehaviorWorker.CanStartRitualNow))]
    public static class Patch_CanStartRitualNow_AcrossLevels
    {
        public static void Postfix(
            TargetInfo target,
            Precept_Ritual ritual,
            Dictionary<string, Pawn> forcedForRole,
            ref string __result)
        {
            if (__result == null
                || target.Map == null
                || ritual?.behavior?.def?.roles == null)
            {
                return;
            }
            if (StrataMod.Settings != null && !StrataMod.Settings.crossLevelRitualsEnabled)
            {
                return;
            }
            if (!RitualTravelUtility.LooksLikeMissingRequiredRole(__result, ritual))
            {
                return;
            }
            if (RitualTravelUtility.HasRequiredRolesAcrossLevels(target, ritual, forcedForRole))
            {
                __result = null;
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
            var sent = new HashSet<Pawn>();
            void TrySend(Pawn pawn)
            {
                if (pawn == null || !sent.Add(pawn) || !pawn.Spawned || pawn.MapHeld == target.Map)
                {
                    return;
                }
                if (pawn.IsFreeColonist)
                {
                    travel.Register(pawn, target.Map, lord, assignments, target.Cell);
                }
                else if (RitualEscortUtility.NeedsEscort(pawn))
                {
                    travel.RegisterEscorted(pawn, target.Map, lord, assignments, target.Cell);
                }
            }

            foreach (Pawn pawn in assignments.Participants)
            {
                TrySend(pawn);
            }
            // Childbirth doctor/mother use countsAsParticipant=false but are still
            // in Participants via RoleForPawn; also pull AssignedPawns explicitly.
            foreach (RitualRole role in assignments.AllRolesForReading)
            {
                foreach (Pawn pawn in assignments.AssignedPawns(role))
                {
                    TrySend(pawn);
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
            public IntVec3 preferArrivalNear;

            public void ExposeData()
            {
                Scribe_References.Look(ref pawn, "pawn");
                Scribe_References.Look(ref destination, "destination");
                Scribe_References.Look(ref lord, "lord");
                Scribe_References.Look(ref escort, "escort");
                Scribe_Values.Look(ref started, "started");
                Scribe_Values.Look(ref escorted, "escorted");
                Scribe_Values.Look(ref preferArrivalNear, "preferArrivalNear");
            }
        }

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

        public void Register(
            Pawn pawn,
            Map destination,
            Lord lord,
            RitualRoleAssignments assignments = null,
            IntVec3 preferArrivalNear = default)
        {
            travelers.RemoveAll(t => t.pawn == pawn);
            var traveler = new Traveler
            {
                pawn = pawn,
                destination = destination,
                lord = lord,
                started = Find.TickManager.TicksGame,
                escorted = false,
                preferArrivalNear = preferArrivalNear,
            };
            travelers.Add(traveler);
            if (assignments != null)
            {
                escortAssignments[pawn] = assignments;
            }
            PortalTravelWalker.TryAdvance(pawn, destination, preferArrivalNear);
        }

        public void RegisterEscorted(
            Pawn pawn,
            Map destination,
            Lord lord,
            RitualRoleAssignments assignments,
            IntVec3 preferArrivalNear = default)
        {
            travelers.RemoveAll(t => t.pawn == pawn);
            var traveler = new Traveler
            {
                pawn = pawn,
                destination = destination,
                lord = lord,
                started = Find.TickManager.TicksGame,
                escorted = true,
                preferArrivalNear = preferArrivalNear,
            };
            travelers.Add(traveler);
            if (assignments != null)
            {
                escortAssignments[pawn] = assignments;
            }
            Pawn escort = traveler.escort;
            PortalTravelWalker.TryAdvanceEscorted(
                pawn, ref escort, destination, preferArrivalNear, assignments);
            traveler.escort = escort;
        }

        public override void WorldComponentTick()
        {
            base.WorldComponentTick();
            if (travelers.Count == 0
                || Find.TickManager.TicksGame % PortalTravelWalker.CheckInterval != 0)
            {
                return;
            }
            for (int i = travelers.Count - 1; i >= 0; i--)
            {
                Traveler t = travelers[i];
                if (PortalTravelWalker.ShouldAbandon(t.pawn, t.destination, t.lord, t.started))
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
                    escortAssignments.TryGetValue(t.pawn, out RitualRoleAssignments assignments);
                    Pawn escort = t.escort;
                    PortalTravelWalker.TryAdvanceEscorted(
                        t.pawn, ref escort, t.destination, t.preferArrivalNear, assignments);
                    t.escort = escort;
                }
                else
                {
                    PortalTravelWalker.TryAdvance(t.pawn, t.destination, t.preferArrivalNear);
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
    }
}
