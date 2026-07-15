using RimWorld;
using Verse;

namespace Strata
{
    // Extends the landing's power shaft down through the portal — the landing
    // is the only transmitter on its cells; dig extensions have no power comp.
    public static class StairwellPowerUtility
    {
        public static void MaintainVerticalTie(Building_StairsUp landing)
        {
            if (landing == null || !landing.Spawned)
            {
                return;
            }
            Building_StairsDown down = landing.DownEntrance;
            if (down == null || !down.Spawned || !down.PocketMapExists || down.exit == null || !down.exit.Spawned)
            {
                return;
            }
            CompPowerShaft top = landing.GetComp<CompPowerShaft>();
            CompPowerShaft bottom = down.exit.GetComp<CompPowerShaft>();
            if (top == null || bottom == null)
            {
                return;
            }
            top.DriveTie(bottom);
        }
    }
}
