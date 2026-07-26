using HarmonyLib;
using RimWorld;
using Verse;

namespace Strata
{
    // Removing a host stairwell/elevator collapses its empty linked level.
    // Ask before the deconstruct designation is placed — travel must never
    // wipe maps; only intentional surface/host shaft destruction does.
    [HarmonyPatch(typeof(Designator_Deconstruct), nameof(Designator_Deconstruct.DesignateThing))]
    public static class Patch_DeconstructAbandonLevel
    {
        private static Thing pendingConfirm;

        public static bool Prefix(Designator_Deconstruct __instance, Thing t)
        {
            if (t == null || t.Destroyed || t.Map == null)
            {
                return true;
            }
            if (t is not MapPortal portal || portal is PocketMapExit)
            {
                return true;
            }
            if (portal.def?.defName == null || !portal.def.defName.StartsWith("Strata_"))
            {
                return true;
            }
            if (!portal.PocketMapExists)
            {
                return true;
            }
            if (t.Map.designationManager.DesignationOn(t, DesignationDefOf.Deconstruct) != null)
            {
                return true;
            }
            // Re-entry after the player confirms the abandon dialog.
            if (pendingConfirm == t)
            {
                pendingConfirm = null;
                return true;
            }

            string body = portal is Building_StairsBuildUp
                ? "Strata_AbandonLevelAboveConfirm".Translate()
                : "Strata_AbandonLevelBelowConfirm".Translate();
            Find.WindowStack.Add(Dialog_MessageBox.CreateConfirmation(
                body,
                delegate
                {
                    pendingConfirm = t;
                    try
                    {
                        __instance.DesignateThing(t);
                    }
                    finally
                    {
                        if (pendingConfirm == t)
                        {
                            pendingConfirm = null;
                        }
                    }
                },
                destructive: true));
            return false;
        }
    }
}
