using System;
using System.Collections.Generic;
using HarmonyLib;
using RimWorld;
using Verse;

namespace Homesteader
{
    internal static class SpoilageTriage
    {
        internal static bool Enabled =>
            HomesteaderMod.Settings == null || HomesteaderMod.Settings.spoilageTriage;

        internal static void ApplyEatBias(Thing foodSource, ref float optimality)
        {
            if (!Enabled || foodSource == null)
            {
                return;
            }

            CompRottable rot = foodSource.TryGetComp<CompRottable>();
            if (rot == null || rot.Stage != RotStage.Fresh)
            {
                return;
            }

            optimality += rot.RotProgressPct * 32f;
        }

        internal static bool IsPreserveBill(Bill bill)
        {
            string n = bill?.recipe?.defName;
            if (n == null || !n.StartsWith("Homesteader_", StringComparison.Ordinal))
            {
                return false;
            }

            if (n.IndexOf("RockSalt", StringComparison.Ordinal) >= 0)
            {
                return false;
            }

            return n.IndexOf("Jerky", StringComparison.Ordinal) >= 0
                || n.IndexOf("Dried", StringComparison.Ordinal) >= 0
                || n.IndexOf("FruitLeather", StringComparison.Ordinal) >= 0
                || n.IndexOf("Pemmican", StringComparison.Ordinal) >= 0
                || n.IndexOf("Cure", StringComparison.Ordinal) >= 0
                || n.IndexOf("Smoke", StringComparison.Ordinal) >= 0
                || n.IndexOf("Pickl", StringComparison.Ordinal) >= 0
                || n.IndexOf("Jam", StringComparison.Ordinal) >= 0
                || n.IndexOf("CanStew", StringComparison.Ordinal) >= 0
                || n.IndexOf("CanJam", StringComparison.Ordinal) >= 0
                || n.IndexOf("Sausage", StringComparison.Ordinal) >= 0;
        }

        internal static void SortRotFirst(List<Thing> availableThings, IntVec3 rootCell)
        {
            if (availableThings == null || availableThings.Count < 2)
            {
                return;
            }

            availableThings.Sort(delegate (Thing a, Thing b)
            {
                return Score(b, rootCell).CompareTo(Score(a, rootCell));
            });
        }

        private static float Score(Thing t, IntVec3 rootCell)
        {
            if (t == null)
            {
                return float.MinValue;
            }

            float rot = 0f;
            CompRottable comp = t.TryGetComp<CompRottable>();
            if (comp != null && comp.Stage == RotStage.Fresh)
            {
                rot = comp.RotProgressPct;
            }

            float dist = t.Position.DistanceTo(rootCell);
            return rot * 100f - dist * 0.35f;
        }
    }

    [HarmonyPatch(typeof(WorkGiver_DoBill), "TryFindBestBillIngredientsInSet")]
    public static class Patch_DoBillIngredientsInSet
    {
        public static void Prefix(List<Thing> availableThings, Bill bill, IntVec3 rootCell, ref bool alreadySorted)
        {
            if (!SpoilageTriage.Enabled || !SpoilageTriage.IsPreserveBill(bill))
            {
                return;
            }

            SpoilageTriage.SortRotFirst(availableThings, rootCell);
            alreadySorted = true;
        }
    }

    [HarmonyPatch(typeof(WorkGiver_DoBill), "TryFindBestBillIngredientsInSet_AllowMix")]
    public static class Patch_DoBillIngredientsAllowMix
    {
        public static void Prefix(List<Thing> availableThings, Bill bill, IntVec3 rootCell)
        {
            if (!SpoilageTriage.Enabled || !SpoilageTriage.IsPreserveBill(bill))
            {
                return;
            }

            SpoilageTriage.SortRotFirst(availableThings, rootCell);
        }
    }
}
