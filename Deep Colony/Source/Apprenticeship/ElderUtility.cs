using RimWorld;
using Verse;

namespace DeepColony
{
    public static class ElderUtility
    {
        public const float ElderAgeYears = 60f;

        public static bool IsElder(Pawn pawn)
        {
            if (pawn?.ageTracker == null) return false;
            return pawn.ageTracker.AgeBiologicalYearsFloat >= ElderAgeYears;
        }

        public static void TickPawn(Pawn pawn)
        {
            if (!DeepColonySettings.Get.enableMentoring && !DeepColonySettings.Get.enablePerks)
                return;
            if (pawn?.IsColonistPlayerControlled != true) return;
            if (!IsElder(pawn)) return;

            var comp = pawn.TryGetComp<Comp_DeepColony>();
            if (comp == null) return;

            if (!comp.elderPerkGranted && DeepColonySettings.Get.enablePerks)
            {
                comp.elderPerkGranted = true;
            }

            EnsureElderHediff(pawn);
        }

        private static void EnsureElderHediff(Pawn pawn)
        {
            if (DC_DefOf.DC_Hediff_Elder == null || pawn.health == null) return;
            if (pawn.health.hediffSet.HasHediff(DC_DefOf.DC_Hediff_Elder)) return;
            Hediff h = HediffMaker.MakeHediff(DC_DefOf.DC_Hediff_Elder, pawn);
            pawn.health.AddHediff(h);
        }
    }
}
