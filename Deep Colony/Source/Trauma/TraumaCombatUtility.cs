using RimWorld;
using Verse;

namespace DeepColony
{
    /// <summary>A05 combat habits + B21 draft penalties (settings-gated).</summary>
    public static class TraumaCombatUtility
    {
        public static void TickPawn(Pawn pawn)
        {
            if (pawn?.health == null || !pawn.Spawned || pawn.Dead) return;
            if (!DeepColonySettings.Get.enableTrauma)
            {
                RemoveIfPresent(pawn, DC_DefOf.DC_Hediff_CombatHabit);
                RemoveIfPresent(pawn, DC_DefOf.DC_Hediff_TraumaDraftPenalty);
                return;
            }

            bool combatShock = TraumaUtility.HasTrauma(pawn, DC_DefOf.DC_Trauma_CombatShock)
                || TraumaUtility.HasTrauma(pawn, DC_DefOf.DC_Trauma_Fire)
                || TraumaUtility.HasTrauma(pawn, DC_DefOf.DC_Trauma_Insect);

            // A05 — combat habits while carrying combat-linked trauma (always on with trauma system).
            SyncHediff(pawn, DC_DefOf.DC_Hediff_CombatHabit, combatShock);

            // B21 — heavier draft penalties (default off).
            bool draftPenalty = DeepColonySettings.Get.enableTraumaPenalties
                && pawn.Drafted
                && combatShock;
            SyncHediff(pawn, DC_DefOf.DC_Hediff_TraumaDraftPenalty, draftPenalty);
        }

        private static void SyncHediff(Pawn pawn, HediffDef def, bool want)
        {
            if (def == null) return;
            Hediff existing = pawn.health.hediffSet.GetFirstHediffOfDef(def);
            if (want)
            {
                if (existing == null)
                    pawn.health.AddHediff(def);
            }
            else if (existing != null)
            {
                pawn.health.RemoveHediff(existing);
            }
        }

        private static void RemoveIfPresent(Pawn pawn, HediffDef def)
        {
            if (def == null || pawn.health == null) return;
            Hediff h = pawn.health.hediffSet.GetFirstHediffOfDef(def);
            if (h != null) pawn.health.RemoveHediff(h);
        }
    }
}
