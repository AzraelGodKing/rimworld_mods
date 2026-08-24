using RimWorld;
using Verse;

namespace DeepColony
{
    /// <summary>Adjacent person below this averse pawn's required comfort tier.</summary>
    public class ThoughtWorker_TouchAverseTooClose : ThoughtWorker
    {
        protected override ThoughtState CurrentStateInternal(Pawn p)
        {
            if (!TouchAverseUtility.Enabled) return false;
            if (!TouchAverseUtility.HasAverse(p)) return false;
            if (p == null || !p.Spawned || p.Map == null) return false;

            int count = 0;
            foreach (Pawn other in p.Map.mapPawns.AllPawnsSpawned)
            {
                if (other == p || other.Dead) continue;
                if (other.RaceProps == null || !other.RaceProps.Humanlike) continue;
                if (!TouchAverseUtility.IsInTouchRange(p, other)) continue;
                if (TouchAverseUtility.IsFineBeingTouchedBy(p, other)) continue;
                count++;
                if (count >= 2) break;
            }
            if (count <= 0) return false;
            int degree = TouchAverseUtility.AverseDegree(p);
            int row = degree <= -1 ? 0 : (degree >= 1 ? 2 : 1);
            return ThoughtState.ActiveAtStage(count >= 2 ? row + 3 : row);
        }
    }

    /// <summary>Someone they trust is standing close on purpose.</summary>
    public class ThoughtWorker_TouchAverseTrustedClose : ThoughtWorker
    {
        protected override ThoughtState CurrentStateInternal(Pawn p)
        {
            if (!TouchAverseUtility.Enabled) return false;
            if (!TouchAverseUtility.HasAverse(p)) return false;
            if (p == null || !p.Spawned || p.Map == null) return false;

            foreach (Pawn other in p.Map.mapPawns.AllPawnsSpawned)
            {
                if (other == p || other.Dead) continue;
                if (other.RaceProps == null || !other.RaceProps.Humanlike) continue;
                if (!TouchAverseUtility.IsInTouchRange(p, other)) continue;
                if (!TouchAverseUtility.IsFineBeingTouchedBy(p, other)) continue;
                return ThoughtState.ActiveDefault;
            }
            return false;
        }
    }

    /// <summary>Sharing a bed with someone they refuse to be touched by.</summary>
    public class ThoughtWorker_TouchAverseSharedBed : ThoughtWorker
    {
        protected override ThoughtState CurrentStateInternal(Pawn p)
        {
            if (!TouchAverseUtility.Enabled) return false;
            if (!TouchAverseUtility.HasAverse(p)) return false;
            Building_Bed bed = p.CurrentBed();
            if (bed == null) return false;
            return HasUnwanted(bed, p) ? ThoughtState.ActiveDefault : false;
        }

        private static bool HasUnwanted(Building_Bed bed, Pawn self)
        {
            if (bed.OwnersForReading != null)
            {
                for (int i = 0; i < bed.OwnersForReading.Count; i++)
                {
                    Pawn other = bed.OwnersForReading[i];
                    if (other == null || other == self) continue;
                    if (TouchAverseUtility.RefusesToShareBed(self, other)) return true;
                }
            }
            if (self.Map == null) return false;
            foreach (Pawn other in self.Map.mapPawns.AllPawnsSpawned)
            {
                if (other == self || other.Dead) continue;
                if (other.CurrentBed() != bed) continue;
                if (TouchAverseUtility.RefusesToShareBed(self, other)) return true;
            }
            return false;
        }
    }

    public class ThoughtWorker_TouchStarvedContact : ThoughtWorker
    {
        protected override ThoughtState CurrentStateInternal(Pawn p)
        {
            if (!TouchAverseUtility.Enabled) return false;
            if (!TouchAverseUtility.HasStarved(p)) return false;
            if (p == null || !p.Spawned || p.Map == null) return false;
            foreach (Pawn other in p.Map.mapPawns.AllPawnsSpawned)
            {
                if (other == p || other.Dead) continue;
                if (other.RaceProps == null || !other.RaceProps.Humanlike) continue;
                if (!TouchAverseUtility.IsInTouchRange(p, other)) continue;
                if (!TouchAverseUtility.IsTrustedForContact(p, other)) continue;
                return ThoughtState.ActiveDefault;
            }
            return false;
        }
    }

    public class ThoughtWorker_TouchStarvedLonely : ThoughtWorker
    {
        protected override ThoughtState CurrentStateInternal(Pawn p)
        {
            if (!TouchAverseUtility.Enabled) return false;
            if (!TouchAverseUtility.StarvedIsLonely(p)) return false;
            return ThoughtState.ActiveDefault;
        }
    }

    public class ThoughtWorker_TactileNearby : ThoughtWorker
    {
        protected override ThoughtState CurrentStateInternal(Pawn p)
        {
            if (!TouchAverseUtility.Enabled) return false;
            if (!TouchAverseUtility.HasTactile(p)) return false;
            if (p == null || !p.Spawned || p.Map == null) return false;
            foreach (Pawn other in p.Map.mapPawns.AllPawnsSpawned)
            {
                if (other == p || other.Dead) continue;
                if (other.RaceProps == null || !other.RaceProps.Humanlike) continue;
                if (!TouchAverseUtility.IsInTouchRange(p, other)) continue;
                int opinion = p.relations != null ? p.relations.OpinionOf(other) : 0;
                if (opinion < 0) continue;
                return ThoughtState.ActiveDefault;
            }
            return false;
        }
    }

    public class ThoughtWorker_CuddlySharedBed : ThoughtWorker
    {
        protected override ThoughtState CurrentStateInternal(Pawn p)
        {
            if (!TouchAverseUtility.Enabled) return false;
            if (!TouchAverseUtility.HasCuddly(p)) return false;
            Building_Bed bed = p.CurrentBed();
            if (bed == null) return false;
            return TrustedBedmate(bed, p) != null ? ThoughtState.ActiveDefault : false;
        }

        internal static Pawn TrustedBedmate(Building_Bed bed, Pawn self)
        {
            if (self.Map == null) return null;
            foreach (Pawn other in self.Map.mapPawns.AllPawnsSpawned)
            {
                if (other == self || other.Dead) continue;
                if (other.CurrentBed() != bed) continue;
                if (TouchAverseUtility.IsTrustedForContact(self, other)) return other;
            }
            return null;
        }
    }

    public class ThoughtWorker_CuddlySleepingAlone : ThoughtWorker
    {
        protected override ThoughtState CurrentStateInternal(Pawn p)
        {
            if (!TouchAverseUtility.Enabled) return false;
            if (!TouchAverseUtility.HasCuddly(p)) return false;
            Building_Bed bed = p.CurrentBed();
            if (bed == null) return false;
            if (ThoughtWorker_CuddlySharedBed.TrustedBedmate(bed, p) != null) return false;
            if (p.Map == null) return false;
            foreach (Pawn other in p.Map.mapPawns.AllPawnsSpawned)
            {
                if (other == p || other.Dead) continue;
                if (other.RaceProps == null || !other.RaceProps.Humanlike) continue;
                if (!TouchAverseUtility.IsTrustedForContact(p, other)) continue;
                return ThoughtState.ActiveDefault;
            }
            return false;
        }
    }
}
