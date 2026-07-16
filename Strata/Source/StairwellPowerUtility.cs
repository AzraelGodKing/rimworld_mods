using RimWorld;
using Verse;

namespace Strata
{
    // Each stairwell landing drives shaft power ties once per cycle: upward
    // through the portal above (entrance StairsDown -> this landing) and
    // downward through an optional dig shaft (DownEntrance -> level below).
    public static class StairwellPowerUtility
    {
        public static void MaintainVerticalTie(Building_StairsUp landing)
        {
            if (landing == null || !landing.Spawned)
            {
                return;
            }

            if (landing.entrance is Building_StairsDown stairsAbove
                && stairsAbove.Spawned && stairsAbove.exit == landing)
            {
                DriveShaftTie(stairsAbove, landing);
            }

            Building_StairsDown digDown = landing.DownEntrance;
            if (digDown != null && digDown.Spawned && digDown.PocketMapExists
                && digDown.exit is Building_StairsUp lower && lower.Spawned)
            {
                DriveShaftTie(digDown, lower);
            }
        }

        private static void DriveShaftTie(Building_StairsDown topPortal, Building_StairsUp bottomLanding)
        {
            if (topPortal is Building_AncientColonyStairsDown || bottomLanding is Building_AncientColonyStairsUp)
            {
                return;
            }
            CompPowerShaft top = topPortal.GetComp<CompPowerShaft>();
            CompPowerShaft bottom = bottomLanding.GetComp<CompPowerShaft>();
            if (top != null && bottom != null)
            {
                top.DriveTie(bottom);
            }
        }
    }
}
