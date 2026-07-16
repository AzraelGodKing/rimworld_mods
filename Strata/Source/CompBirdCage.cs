using System.Collections.Generic;
using RimWorld;
using Verse;

namespace Strata
{
    public class CompProperties_BirdCage : CompProperties_CagedBird
    {
        public CompProperties_BirdCage()
        {
            compClass = typeof(CompBirdCage);
        }
    }

    public class CompBirdCage : CompCagedBirdBase
    {
        protected override string OccupantNoun => "bird";

        protected override bool CanAccept(Pawn pawn) =>
            StrataCanaryBirdUtility.IsAssignableBird(pawn);

        protected override IEnumerable<Pawn> EnumerateAssignCandidates()
        {
            Map map = parent.Map;
            if (map == null)
            {
                yield break;
            }
            foreach (Pawn pawn in map.mapPawns.AllPawnsSpawned)
            {
                if (!StrataCanaryBirdUtility.IsAssignableBird(pawn))
                {
                    continue;
                }
                if (pawn.Dead || IsContained(pawn))
                {
                    continue;
                }
                if (pawn.Faction != Faction.OfPlayer)
                {
                    continue;
                }
                if (!pawn.Position.InHorDistOf(parent.Position, Props.assignRadius))
                {
                    continue;
                }
                yield return pawn;
            }
        }

        protected override string EmptyInspectExtra() =>
            UsesSustainMode
                ? "Empty — assign a nearby tame bird. Hunger is sustained while caged (mod setting)."
                : "Empty — assign a nearby tame bird and stock food in the cage.";
    }
}
