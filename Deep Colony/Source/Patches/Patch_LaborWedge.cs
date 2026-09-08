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
    /// AZR-158 — failed birth can leave labor attached after
    /// HediffWithParents already un-preserved the father. Layered recovery:
    /// swallow teardown exceptions, force-detach if RemoveHediff aborts,
    /// clear on load, and keep sweeping mid-session.
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
            LaborWedgeRecovery.NoteFailedBirth(pawn);
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
            if (__exception == null || !LaborWedgeRecovery.IsLaborFamily(hediff))
            {
                return __exception;
            }

            Log.Warning("[DeepColony] RemoveHediff aborted for labor/pregnancy on "
                + (hediff?.pawn?.LabelShort ?? "unknown")
                + " (" + __exception.GetType().Name + "). Force-detaching.");
            LaborWedgeRecovery.ForceDetach(__instance, hediff);
            LaborWedgeRecovery.NoteFailedBirth(hediff?.pawn);
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
                + ". Returning null so labor can still end.");
            __result = null;
            return null;
        }
    }

    internal static class LaborWedgeRecovery
    {
        private static readonly HashSet<int> pending = new HashSet<int>();
        private static readonly HashSet<int> lettered = new HashSet<int>();
        private static readonly List<Pawn> recoveredBuffer = new List<Pawn>();

        internal static void ResetSession()
        {
            pending.Clear();
            lettered.Clear();
        }

        internal static void NoteFailedBirth(Pawn mother)
        {
            if (mother == null || mother.Destroyed)
            {
                return;
            }

            pending.Add(mother.thingIDNumber);
        }

        internal static bool IsLaborFamily(Hediff hediff)
        {
            if (hediff == null)
            {
                return false;
            }

            if (hediff is Hediff_LaborPushing || hediff is Hediff_Pregnant)
            {
                return true;
            }

            string name = hediff.def?.defName;
            return name == "Pregnant"
                || name == "PregnancyLabor"
                || name == "PregnancyLaborPushing";
        }

        internal static void RecoverStuckLabor()
        {
            recoveredBuffer.Clear();
            foreach (Pawn pawn in EnumeratePawns())
            {
                if (TryClearPawn(pawn, onlyIfShouldRemove: false) && pawn != null)
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
                if (TryClearPawn(pawn, onlyIfShouldRemove: true) && pawn != null)
                {
                    recoveredBuffer.Add(pawn);
                }
            }

            SendLetter(recoveredBuffer);
        }

        internal static void ForceDetach(Pawn_HealthTracker tracker, Hediff hediff)
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

        private static void DrainPending()
        {
            recoveredBuffer.Clear();
            int[] ids = new int[pending.Count];
            pending.CopyTo(ids);
            pending.Clear();
            for (int i = 0; i < ids.Length; i++)
            {
                Pawn pawn = FindPawn(ids[i]);
                if (TryClearPawn(pawn, onlyIfShouldRemove: false) && pawn != null)
                {
                    recoveredBuffer.Add(pawn);
                }
            }

            SendLetter(recoveredBuffer);
        }

        private static bool TryClearPawn(Pawn pawn, bool onlyIfShouldRemove)
        {
            if (pawn?.health?.hediffSet?.hediffs == null || pawn.Dead)
            {
                return false;
            }

            List<Hediff> hediffs = pawn.health.hediffSet.hediffs;
            bool cleared = false;
            for (int i = hediffs.Count - 1; i >= 0; i--)
            {
                if (i >= hediffs.Count)
                {
                    continue;
                }

                Hediff hediff = hediffs[i];
                if (!IsLaborFamily(hediff))
                {
                    continue;
                }

                if (onlyIfShouldRemove && !SafeShouldRemove(hediff))
                {
                    continue;
                }

                if (onlyIfShouldRemove && hediff is Hediff_Pregnant)
                {
                    continue;
                }

                if (ForceRemove(pawn, hediff))
                {
                    cleared = true;
                }
            }

            return cleared;
        }

        private static bool SafeShouldRemove(Hediff hediff)
        {
            try
            {
                return hediff.ShouldRemove;
            }
            catch (Exception e)
            {
                Log.Warning("[DeepColony] Hediff.ShouldRemove threw on "
                    + (hediff.pawn?.LabelShort ?? "unknown")
                    + " (" + e.GetType().Name + "). Treating as stuck.");
                return true;
            }
        }

        private static bool ForceRemove(Pawn pawn, Hediff hediff)
        {
            try
            {
                pawn.health.RemoveHediff(hediff);
                Log.Warning("[DeepColony] Cleared stuck " + (hediff.def?.defName ?? "labor")
                    + " on " + pawn.LabelShort + " after a failed birth.");
                return true;
            }
            catch (Exception e)
            {
                Log.Warning("[DeepColony] RemoveHediff threw on " + pawn.LabelShort
                    + " (" + e.GetType().Name + "). Force-detaching.");
                ForceDetach(pawn.health, hediff);
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
