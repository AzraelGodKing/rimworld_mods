using HarmonyLib;
using RimWorld;
using Verse;
using Verse.AI;

namespace Strata
{
    // Biotech mechs only look for chargers on their current map. When the pad
    // is upstairs they idle until the battery dies — send them through stairs.
    [HarmonyPatch(typeof(JobGiver_GetEnergy_Charger), "TryGiveJob")]
    public static class Patch_MechChargeAcrossLevels
    {
        public static void Postfix(Pawn pawn, ref Job __result)
        {
            if (__result != null || !ModsConfig.BiotechActive || pawn == null
                || !pawn.IsColonyMech || !pawn.Spawned)
            {
                return;
            }

            Need_MechEnergy energy = pawn.needs?.energy;
            if (energy == null)
            {
                return;
            }

            if (energy.CurLevel + 0.1f
                >= JobGiver_GetEnergy.GetMinAutorechargeThreshold(pawn))
            {
                return;
            }

            if (!PawnRelay.CanRelay(pawn) || !LevelGraph.AnyLinkFrom(pawn.Map))
            {
                return;
            }

            Map bestMap = null;
            float bestScore = float.MaxValue;
            foreach (LevelGraph.LevelLink link in LevelGraph.ReachableLevels(pawn.Map))
            {
                if (!link.arrivalCell.IsValid || !link.arrivalCell.InBounds(link.map))
                {
                    continue;
                }

                foreach (Building building in link.map.listerBuildings.allBuildingsColonist)
                {
                    if (building is not Building_MechCharger charger
                        || !charger.CanPawnChargeCurrently(pawn)
                        || charger.IsForbidden(pawn))
                    {
                        continue;
                    }

                    if (!link.map.reachability.CanReach(
                            link.arrivalCell,
                            charger,
                            PathEndMode.InteractionCell,
                            TraverseParms.For(TraverseMode.PassDoors)))
                    {
                        continue;
                    }

                    float score = link.arrivalCell.DistanceToSquared(charger.Position);
                    if (link.firstStep != null)
                    {
                        score += pawn.Position.DistanceToSquared(link.firstStep.Position);
                    }

                    if (score < bestScore)
                    {
                        bestScore = score;
                        bestMap = link.map;
                    }
                }
            }

            if (bestMap == null)
            {
                return;
            }

            Job hop = PawnRelay.TryRelayToMap(
                pawn,
                bestMap,
                touchCooldown: false,
                RelayPurpose.Work);
            if (hop != null)
            {
                __result = hop;
            }
        }
    }
}
