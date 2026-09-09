using System;
using System.Collections.Generic;
using System.Text;
using HarmonyLib;
using RimWorld;
using RimWorld.Planet;
using Verse;

namespace DeepColony.Patches
{
    /// <summary>
    /// AZR-158 / AZR-159 — failed birth can leave labor attached after
    /// HediffWithParents already un-preserved the father. Swallow teardown
    /// exceptions so the mother is not tick-wedged, then roll stuck labor
    /// back to a healthy Hediff_Pregnant (never abort the pregnancy).
    /// </summary>
    [HarmonyPatch(typeof(WorldPawns), nameof(WorldPawns.RemovePreservedPawnHediff))]
    public static class Patch_WorldPawns_RemovePreservedPawnHediff
    {
        public static Exception Finalizer(Exception __exception)
        {
            return __exception is KeyNotFoundException or ArgumentNullException
                ? null
                : __exception;
        }
    }

    [HarmonyPatch(typeof(HediffWithParents), nameof(HediffWithParents.PreRemoved))]
    public static class Patch_HediffWithParents_PreRemoved
    {
        public static Exception Finalizer(Exception __exception, HediffWithParents __instance)
        {
            if (__exception == null)
            {
                return null;
            }

            Log.Warning("[DeepColony] HediffWithParents.PreRemoved failed on "
                + (__instance?.pawn?.LabelShort ?? "unknown")
                + " (" + __exception.GetType().Name + "): " + __exception.Message
                + ". Continuing teardown.");
            return null;
        }
    }

    [HarmonyPatch(typeof(Hediff_Labor), nameof(Hediff_Labor.PreRemoved))]
    public static class Patch_Hediff_Labor_PreRemoved
    {
        public static Exception Finalizer(Exception __exception, Hediff_Labor __instance)
        {
            if (__exception == null)
            {
                return null;
            }

            Pawn pawn = __instance?.pawn;
            Log.Warning("[DeepColony] Hediff_Labor.PreRemoved failed on "
                + (pawn?.LabelShort ?? "unknown")
                + " (" + __exception.GetType().Name + "): " + __exception.Message);
            LaborWedgeRecovery.NoteFailedBirth(pawn, __instance);
            return null;
        }
    }

    [HarmonyPatch(typeof(Hediff_LaborPushing), nameof(Hediff_LaborPushing.PreRemoved))]
    public static class Patch_Hediff_LaborPushing_PreRemoved
    {
        public static Exception Finalizer(Exception __exception, Hediff_LaborPushing __instance)
        {
            if (__exception == null)
            {
                return null;
            }

            Pawn pawn = __instance?.pawn;
            Log.Warning("[DeepColony] Hediff_LaborPushing.PreRemoved failed on "
                + (pawn?.LabelShort ?? "unknown")
                + " (" + __exception.GetType().Name + "): " + __exception.Message);
            LaborWedgeRecovery.NoteFailedBirth(pawn, __instance);
            return null;
        }
    }

    [HarmonyPatch(typeof(Pawn_HealthTracker), nameof(Pawn_HealthTracker.RemoveHediff))]
    public static class Patch_HealthTracker_RemoveHediff
    {
        public static Exception Finalizer(
            Exception __exception,
            Pawn_HealthTracker __instance,
            Hediff hediff)
        {
            if (__exception == null)
            {
                return null;
            }

            if (LaborWedgeRecovery.IsPregnancyHediff(hediff))
            {
                Log.Warning("[DeepColony] RemoveHediff failed for pregnancy on "
                    + (hediff?.pawn?.LabelShort ?? "unknown")
                    + " (" + __exception.GetType().Name + "). Leaving the pregnancy in place.");
                return null;
            }

            if (!LaborWedgeRecovery.IsLaborStage(hediff))
            {
                return __exception;
            }

            Log.Warning("[DeepColony] RemoveHediff aborted for labor on "
                + (hediff?.pawn?.LabelShort ?? "unknown")
                + " (" + __exception.GetType().Name + "). Pregnancy will be restored.");
            LaborWedgeRecovery.NoteFailedBirth(hediff?.pawn, hediff);
            return null;
        }
    }

    [HarmonyPatch(typeof(PawnGenerator), nameof(PawnGenerator.GeneratePawn), new[] { typeof(PawnGenerationRequest) })]
    [HarmonyPriority(Priority.Last)]
    public static class Patch_PawnGenerator_BirthFailOpen
    {
        public static Exception Finalizer(Exception __exception, PawnGenerationRequest request, ref Pawn __result)
        {
            if (__exception == null)
            {
                return null;
            }

            if (!BirthSafetyNet.IsNewbornOrBabyRequest(request))
            {
                return __exception;
            }

            Log.Warning("[DeepColony] GeneratePawn failed for a newborn ("
                + __exception.GetType().Name + "): " + __exception.Message
                + ". Returning null; pregnancy will be restored instead of ending labor empty.");
            __result = null;
            return null;
        }
    }

    internal static class LaborWedgeRecovery
    {
        private const float RestoredGestationProgress = 0.9f;

        private static readonly Dictionary<int, PregnancySnapshot> pending =
            new Dictionary<int, PregnancySnapshot>();
        private static readonly HashSet<int> lettered = new HashSet<int>();
        private static readonly List<Pawn> recoveredBuffer = new List<Pawn>();

        private struct PregnancySnapshot
        {
            public Pawn geneticMother;
            public Pawn father;
            public GeneSet geneSet;
        }

        internal static void ResetSession()
        {
            pending.Clear();
            lettered.Clear();
        }

        internal static void NoteFailedBirth(Pawn carrier, Hediff source = null, Pawn father = null)
        {
            if (carrier == null || carrier.Destroyed)
            {
                return;
            }

            PregnancySnapshot snap = SnapshotFrom(carrier, source, father);
            pending[carrier.thingIDNumber] = snap;
        }

        internal static bool IsPregnancyHediff(Hediff hediff)
        {
            if (hediff is Hediff_Pregnant)
            {
                return true;
            }

            return hediff?.def?.defName == "Pregnant";
        }

        internal static bool IsLaborStage(Hediff hediff)
        {
            if (hediff == null)
            {
                return false;
            }

            if (hediff is Hediff_LaborPushing || hediff is Hediff_Labor)
            {
                return true;
            }

            string name = hediff.def?.defName;
            return name == "PregnancyLabor" || name == "PregnancyLaborPushing";
        }

        internal static void RecoverStuckLabor()
        {
            recoveredBuffer.Clear();
            foreach (Pawn pawn in EnumeratePawns())
            {
                if (TryRepairPawn(pawn, default, hasSnapshot: false) && pawn != null)
                {
                    recoveredBuffer.Add(pawn);
                }
            }

            SendLetter(recoveredBuffer);
        }

        internal static void Tick()
        {
            if (pending.Count > 0)
            {
                DrainPending();
            }

            if (!DeepColony.TickPhase.Due(947))
            {
                return;
            }

            recoveredBuffer.Clear();
            foreach (Pawn pawn in EnumeratePawns())
            {
                if (TryRepairPawn(pawn, default, hasSnapshot: false) && pawn != null)
                {
                    recoveredBuffer.Add(pawn);
                }
            }

            SendLetter(recoveredBuffer);
        }

        private static void DrainPending()
        {
            recoveredBuffer.Clear();
            int[] ids = new int[pending.Count];
            pending.Keys.CopyTo(ids, 0);
            PregnancySnapshot[] snaps = new PregnancySnapshot[ids.Length];
            for (int i = 0; i < ids.Length; i++)
            {
                snaps[i] = pending[ids[i]];
            }

            pending.Clear();
            for (int i = 0; i < ids.Length; i++)
            {
                Pawn pawn = FindPawn(ids[i]);
                if (TryRepairPawn(pawn, snaps[i], hasSnapshot: true) && pawn != null)
                {
                    recoveredBuffer.Add(pawn);
                }
            }

            SendLetter(recoveredBuffer);
        }

        private static PregnancySnapshot SnapshotFrom(Pawn carrier, Hediff source, Pawn father)
        {
            PregnancySnapshot snap = new PregnancySnapshot
            {
                geneticMother = carrier,
                father = father
            };

            if (source is HediffWithParents parents)
            {
                snap.geneticMother = parents.Mother ?? carrier;
                snap.father = parents.Father ?? father;
                snap.geneSet = parents.geneSet;
            }

            return snap;
        }

        private static bool TryRepairPawn(Pawn pawn, PregnancySnapshot snapshot, bool hasSnapshot)
        {
            if (pawn?.health?.hediffSet?.hediffs == null || pawn.Dead)
            {
                return false;
            }

            Hediff_Pregnant existing = pawn.health.hediffSet.GetFirstHediff<Hediff_Pregnant>();
            HediffWithParents labor = FindLaborStage(pawn);
            bool laborBroken = LaborShouldRemoveBroke(labor);

            // Only restore when this session already failed a birth, or ShouldRemove
            // itself throws. Do not roll back a healthy pawn who is about to deliver.
            if (!hasSnapshot && !laborBroken)
            {
                return false;
            }

            if (existing != null && labor == null)
            {
                return false;
            }

            Pawn geneticMother = snapshot.geneticMother ?? labor?.Mother ?? pawn;
            Pawn father = snapshot.father ?? labor?.Father;
            GeneSet genes = snapshot.geneSet ?? labor?.geneSet;
            if (genes == null && (father != null || geneticMother != null))
            {
                try
                {
                    genes = PregnancyUtility.GetInheritedGeneSet(father, geneticMother);
                }
                catch (Exception e)
                {
                    Log.Warning("[DeepColony] GetInheritedGeneSet failed for "
                        + pawn.LabelShort + ": " + e.Message);
                }
            }

            DetachLaborStages(pawn);

            existing = pawn.health.hediffSet.GetFirstHediff<Hediff_Pregnant>();
            if (existing != null)
            {
                Log.Warning("[DeepColony] Kept existing pregnancy on " + pawn.LabelShort
                    + " after detaching stuck labor.");
                return true;
            }

            return ApplyHealthyPregnancy(pawn, geneticMother, father, genes);
        }

        private static bool ApplyHealthyPregnancy(
            Pawn pawn,
            Pawn geneticMother,
            Pawn father,
            GeneSet genes)
        {
            HediffDef def = HediffDefOf.Pregnant;
            if (def == null)
            {
                Log.Warning("[DeepColony] Cannot restore pregnancy on " + pawn.LabelShort
                    + ": HediffDefOf.Pregnant is missing.");
                return false;
            }

            try
            {
                Hediff added = pawn.health.AddHediff(def);
                if (added is not Hediff_Pregnant preg)
                {
                    Log.Warning("[DeepColony] AddHediff(Pregnant) did not return Hediff_Pregnant on "
                        + pawn.LabelShort + ".");
                    return added != null;
                }

                preg.SetParents(geneticMother ?? pawn, father, genes);
                preg.Severity = RestoredGestationProgress;
                Log.Warning("[DeepColony] Restored healthy pregnancy on " + pawn.LabelShort
                    + " after stuck labor.");
                return true;
            }
            catch (Exception e)
            {
                Log.Warning("[DeepColony] Restoring pregnancy on " + pawn.LabelShort
                    + " failed (" + e.GetType().Name + "): " + e.Message);
                return false;
            }
        }

        private static HediffWithParents FindLaborStage(Pawn pawn)
        {
            List<Hediff> hediffs = pawn.health.hediffSet.hediffs;
            HediffWithParents labor = null;
            for (int i = 0; i < hediffs.Count; i++)
            {
                Hediff hediff = hediffs[i];
                if (!IsLaborStage(hediff))
                {
                    continue;
                }

                if (hediff is Hediff_LaborPushing pushing)
                {
                    return pushing;
                }

                if (hediff is HediffWithParents parents)
                {
                    labor = parents;
                }
            }

            return labor;
        }

        private static void DetachLaborStages(Pawn pawn)
        {
            List<Hediff> hediffs = pawn.health.hediffSet.hediffs;
            for (int i = hediffs.Count - 1; i >= 0; i--)
            {
                if (i >= hediffs.Count)
                {
                    continue;
                }

                Hediff hediff = hediffs[i];
                if (!IsLaborStage(hediff))
                {
                    continue;
                }

                if (hediff is HediffWithParents parents)
                {
                    Unpreserve(parents);
                }

                ForceDetach(pawn.health, hediff);
            }
        }

        private static void Unpreserve(HediffWithParents hediff)
        {
            WorldPawns world = Find.WorldPawns;
            if (world == null || hediff == null)
            {
                return;
            }

            try
            {
                if (hediff.Mother != null)
                {
                    world.RemovePreservedPawnHediff(hediff.Mother, hediff);
                }
            }
            catch (Exception e)
            {
                Log.Warning("[DeepColony] Unpreserve mother leftover: " + e.Message);
            }

            try
            {
                if (hediff.Father != null)
                {
                    world.RemovePreservedPawnHediff(hediff.Father, hediff);
                }
            }
            catch (Exception e)
            {
                Log.Warning("[DeepColony] Unpreserve father leftover: " + e.Message);
            }
        }

        private static void ForceDetach(Pawn_HealthTracker tracker, Hediff hediff)
        {
            if (tracker == null || hediff == null)
            {
                return;
            }

            try
            {
                List<Hediff> list = tracker.hediffSet?.hediffs;
                if (list != null)
                {
                    list.Remove(hediff);
                }

                hediff.pawn = null;
            }
            catch (Exception e)
            {
                Log.Warning("[DeepColony] Force-detach leftover: " + e.Message);
            }
        }

        private static bool LaborShouldRemoveBroke(Hediff hediff)
        {
            if (hediff == null)
            {
                return false;
            }

            try
            {
                _ = hediff.ShouldRemove;
                return false;
            }
            catch (Exception e)
            {
                Log.Warning("[DeepColony] Hediff.ShouldRemove threw on "
                    + (hediff.pawn?.LabelShort ?? "unknown")
                    + " (" + e.GetType().Name + "). Treating as stuck.");
                return true;
            }
        }

        private static void SendLetter(List<Pawn> recovered)
        {
            if (recovered == null || recovered.Count == 0 || Find.LetterStack == null)
            {
                return;
            }

            StringBuilder names = new StringBuilder();
            Pawn look = null;
            int named = 0;
            for (int i = 0; i < recovered.Count; i++)
            {
                Pawn pawn = recovered[i];
                if (pawn == null || !lettered.Add(pawn.thingIDNumber))
                {
                    continue;
                }

                if (named > 0)
                {
                    names.Append(", ");
                }

                names.Append(pawn.LabelShort);
                look ??= pawn;
                named++;
            }

            if (named == 0)
            {
                return;
            }

            try
            {
                Find.LetterStack.ReceiveLetter(
                    "DC_Letter_LaborWedgeLabel".Translate(),
                    "DC_Letter_LaborWedge".Translate(names.ToString()),
                    LetterDefOf.NeutralEvent,
                    look);
            }
            catch (Exception e)
            {
                Log.Warning("[DeepColony] Labor-wedge letter failed: " + e.Message);
            }
        }

        private static Pawn FindPawn(int thingId)
        {
            foreach (Pawn pawn in EnumeratePawns())
            {
                if (pawn != null && pawn.thingIDNumber == thingId)
                {
                    return pawn;
                }
            }

            return null;
        }

        private static IEnumerable<Pawn> EnumeratePawns()
        {
            if (Find.Maps != null)
            {
                for (int m = 0; m < Find.Maps.Count; m++)
                {
                    IReadOnlyList<Pawn> spawned = null;
                    try
                    {
                        spawned = Find.Maps[m]?.mapPawns?.AllPawnsSpawned;
                    }
                    catch
                    {
                        continue;
                    }

                    if (spawned == null)
                    {
                        continue;
                    }

                    for (int i = 0; i < spawned.Count; i++)
                    {
                        yield return spawned[i];
                    }
                }
            }

            IEnumerable<Pawn> world = null;
            try
            {
                world = Find.WorldPawns?.AllPawnsAlive;
            }
            catch
            {
                yield break;
            }

            if (world == null)
            {
                yield break;
            }

            foreach (Pawn pawn in world)
            {
                yield return pawn;
            }
        }
    }
}
