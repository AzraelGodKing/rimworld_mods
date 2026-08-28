using RimWorld;
using Verse;

namespace DeepColony.Patches
{
    public static class TraumaEventUtility
    {
        private const float SpecialtyChance = 0.45f;
        private const float BetrayalChance = 0.55f;
        private const float ToxicSeverityThreshold = 0.55f;

        public static void TrySpecialtyOrCombatShock(Pawn victim, DamageInfo info)
        {
            if (victim == null || !victim.IsColonistPlayerControlled) return;

            Faction faction = info.Instigator?.Faction;
            Pawn instigatorPawn = info.Instigator as Pawn;

            // B13 — betrayal: hurt by someone they liked / former colonist.
            if (instigatorPawn != null
                && victim.relations != null
                && (victim.relations.OpinionOf(instigatorPawn) >= 25
                    || (GameComp_DeepColony.Instance?.WasEverPlayerColonist(instigatorPawn) ?? false)))
            {
                if (Rand.Chance(BetrayalChance) && DC_DefOf.DC_Trauma_Betrayal != null)
                {
                    TraumaUtility.ApplyTrauma(victim, DC_DefOf.DC_Trauma_Betrayal, instigatorPawn, faction);
                    return;
                }
            }

            // A06 specialty routes
            if (IsFireDamage(info, victim) && Rand.Chance(SpecialtyChance)
                && DC_DefOf.DC_Trauma_Fire != null)
            {
                TraumaUtility.ApplyTrauma(victim, DC_DefOf.DC_Trauma_Fire, sourceFaction: faction);
                return;
            }

            if (IsToxicDamage(info, victim) && Rand.Chance(SpecialtyChance)
                && DC_DefOf.DC_Trauma_Toxic != null)
            {
                TraumaUtility.ApplyTrauma(victim, DC_DefOf.DC_Trauma_Toxic, sourceFaction: faction);
                return;
            }

            if (IsInsectAttack(instigatorPawn) && Rand.Chance(SpecialtyChance)
                && DC_DefOf.DC_Trauma_Insect != null)
            {
                TraumaUtility.ApplyTrauma(victim, DC_DefOf.DC_Trauma_Insect, instigatorPawn, faction);
                return;
            }

            // Combat shock requires a hostile instigator.
            Thing instigator = info.Instigator;
            if (instigator == null || !instigator.HostileTo(victim)) return;

            if (!Rand.Chance(DeepColonySettings.Get.combatShockChance)) return;
            TraumaDef def = DC_DefOf.DC_Trauma_CombatShock;
            if (def != null)
                TraumaUtility.ApplyTrauma(victim, def, sourceFaction: faction);
        }

        public static void TryToxicBuildupTrauma(Pawn pawn)
        {
            if (!DeepColonySettings.Get.enableTrauma) return;
            if (pawn == null || !pawn.IsColonistPlayerControlled) return;
            if (DC_DefOf.DC_Trauma_Toxic == null) return;
            if (TraumaUtility.HasTrauma(pawn, DC_DefOf.DC_Trauma_Toxic)) return;

            Hediff toxic = pawn.health?.hediffSet?.GetFirstHediffOfDef(HediffDefOf.ToxicBuildup);
            if (toxic == null || toxic.Severity < ToxicSeverityThreshold) return;
            if (!Rand.Chance(0.35f)) return;
            TraumaUtility.ApplyTrauma(pawn, DC_DefOf.DC_Trauma_Toxic);
        }

        private static bool IsFireDamage(DamageInfo info, Pawn victim)
        {
            if (info.Def == DamageDefOf.Flame) return true;
            if (info.Def?.defName != null
                && info.Def.defName.IndexOf("Burn", System.StringComparison.OrdinalIgnoreCase) >= 0)
                return true;
            return victim.IsBurning();
        }

        private static bool IsToxicDamage(DamageInfo info, Pawn victim)
        {
            if (info.Def?.defName != null
                && info.Def.defName.IndexOf("Toxic", System.StringComparison.OrdinalIgnoreCase) >= 0)
                return true;
            Hediff toxic = victim.health?.hediffSet?.GetFirstHediffOfDef(HediffDefOf.ToxicBuildup);
            return toxic != null && toxic.Severity >= 0.4f;
        }

        private static bool IsInsectAttack(Pawn instigator)
        {
            return instigator?.RaceProps?.Insect == true;
        }
    }
}
