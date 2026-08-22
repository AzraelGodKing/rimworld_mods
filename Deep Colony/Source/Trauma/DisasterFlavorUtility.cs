using System;
using RimWorld;
using Verse;

namespace DeepColony
{
    /// <summary>
    /// C10 — Strata cave-in / Stormproof ion-storm downs apply existing trauma with a keyed reason.
    /// </summary>
    public static class DisasterFlavorUtility
    {
        public static void NotifyDowned(Pawn victim, DamageInfo info)
        {
            if (!DeepColonySettings.Get.enableTrauma) return;
            if (victim == null || !victim.IsColonistPlayerControlled) return;

            if (SoftCompat.StrataLoaded && IsCaveInDamage(info, victim))
            {
                TraumaDef shock = DC_DefOf.DC_Trauma_CombatShock;
                if (shock != null && !TraumaUtility.HasTrauma(victim, shock))
                    TraumaUtility.ApplyTrauma(victim, shock, reasonOverride: "DC_TraumaReason_CaveIn");
                return;
            }

            if (SoftCompat.StrataLoaded && IsStrataGasDamage(info, victim))
            {
                TraumaDef toxic = DC_DefOf.DC_Trauma_Toxic;
                if (toxic != null)
                    TraumaUtility.ApplyTrauma(victim, toxic, reasonOverride: "DC_TraumaReason_StrataGas");
                return;
            }

            if (SoftCompat.StrataLoaded && IsStrataFirestorm(info, victim))
            {
                TraumaDef fire = DC_DefOf.DC_Trauma_Fire;
                if (fire != null)
                    TraumaUtility.ApplyTrauma(victim, fire, reasonOverride: "DC_TraumaReason_Firestorm");
                return;
            }

            if (SoftCompat.StormproofLoaded && IsIonStormDown(info, victim))
            {
                TraumaDef fire = DC_DefOf.DC_Trauma_Fire;
                TraumaDef toxic = DC_DefOf.DC_Trauma_Toxic;
                if (info.Def == DamageDefOf.Flame && fire != null)
                {
                    TraumaUtility.ApplyTrauma(victim, fire, reasonOverride: "DC_TraumaReason_IonStorm");
                    return;
                }
                if (toxic != null)
                    TraumaUtility.ApplyTrauma(victim, toxic, reasonOverride: "DC_TraumaReason_IonStorm");
                else if (DC_DefOf.DC_Trauma_CombatShock != null)
                    TraumaUtility.ApplyTrauma(victim, DC_DefOf.DC_Trauma_CombatShock,
                        reasonOverride: "DC_TraumaReason_IonStorm");
            }
        }

        private static bool IsCaveInDamage(DamageInfo info, Pawn victim)
        {
            if (info.Def == DamageDefOf.Crush) return true;
            if (info.Def != null && info.Def.defName != null
                && info.Def.defName.IndexOf("Crush", System.StringComparison.OrdinalIgnoreCase) >= 0)
                return true;
            // Roof collapse often has no pawn instigator.
            return info.Instigator == null && info.Def == DamageDefOf.Blunt
                && victim.MapHeld != null && victim.Position.Roofed(victim.MapHeld);
        }

        private static bool IsIonStormDown(DamageInfo info, Pawn victim)
        {
            if (!SoftCompat.MapHasIonStorm(victim.MapHeld)) return false;
            if (info.Def == DamageDefOf.EMP) return true;
            if (info.Def == DamageDefOf.Flame) return true;
            if (info.Def != null && info.Def.defName != null
                && (info.Def.defName.IndexOf("EMP", System.StringComparison.OrdinalIgnoreCase) >= 0
                    || info.Def.defName.IndexOf("Lightning", System.StringComparison.OrdinalIgnoreCase) >= 0))
                return true;
            return false;
        }

        private static bool IsStrataGasDamage(DamageInfo info, Pawn victim)
        {
            string n = info.Def?.defName ?? "";
            if (n.IndexOf("Toxic", System.StringComparison.OrdinalIgnoreCase) >= 0) return true;
            if (n.IndexOf("RotStink", System.StringComparison.OrdinalIgnoreCase) >= 0) return true;
            if (n.IndexOf("Gas", System.StringComparison.OrdinalIgnoreCase) >= 0
                && n.IndexOf("Strata", System.StringComparison.OrdinalIgnoreCase) >= 0)
                return true;
            return false;
        }

        private static bool IsStrataFirestorm(DamageInfo info, Pawn victim)
        {
            Map map = victim.MapHeld;
            if (map?.GameConditionManager != null)
            {
                GameConditionDef firestorm = DefDatabase<GameConditionDef>.GetNamedSilentFail("Strata_Firestorm")
                    ?? DefDatabase<GameConditionDef>.GetNamedSilentFail("StrataFirestorm");
                if (firestorm != null && map.GameConditionManager.ConditionIsActive(firestorm)
                    && (info.Def == DamageDefOf.Flame || victim.IsBurning()))
                    return true;
            }
            string n = info.Def?.defName ?? "";
            return n.IndexOf("Firestorm", System.StringComparison.OrdinalIgnoreCase) >= 0;
        }
    }
}
