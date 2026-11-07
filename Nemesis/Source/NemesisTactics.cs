#nullable enable
using RimWorld;
using Verse;

namespace Nemesis
{
    /// <summary>How a nemesis assault is run (AZR-554). Picked by combat focus.</summary>
    public enum NemesisTactic
    {
        /// <summary>Captain hunts the fixation target alongside a plain supporting raid.</summary>
        Assault,
        /// <summary>Supporting raid breaches walls; the captain follows through the gap.</summary>
        Breach,
        /// <summary>Supporting raid digs in for a siege; the captain hunts from the flank.</summary>
        Siege,
        /// <summary>Captain holds back at the landing point and steps in once the raid is bleeding.</summary>
        Commander,
    }

    public static class NemesisTactics
    {
        public static NemesisTactic Choose(NemesisData data)
        {
            switch (data.combatFocus)
            {
                case NemesisCombatFocus.Destroyer:
                case NemesisCombatFocus.Berserker:
                    return NemesisTactic.Breach;
                case NemesisCombatFocus.Sniper:
                    return NemesisTactic.Siege;
                case NemesisCombatFocus.Psycho:
                case NemesisCombatFocus.Mechanitor:
                    return NemesisTactic.Commander;
                default:
                    return NemesisTactic.Assault;
            }
        }

        /// <summary>
        /// The supporting raid's strategy, or null to let vanilla pick. Only returned when the
        /// strategy can run with these parms (Siege needs a faction that builds, for example).
        /// </summary>
        public static RaidStrategyDef? SupportStrategy(NemesisTactic tactic, IncidentParms parms)
        {
            string? defName = tactic switch
            {
                NemesisTactic.Breach => "ImmediateAttackBreaching",
                NemesisTactic.Siege => "Siege",
                NemesisTactic.Commander => "StageThenAttack",
                _ => null,
            };
            if (defName == null) return null;
            RaidStrategyDef? def = DefDatabase<RaidStrategyDef>.GetNamedSilentFail(defName);
            if (def == null) return null;
            try
            {
                return def.Worker.CanUseWith(parms, PawnGroupKindDefOf.Combat) ? def : null;
            }
            catch
            {
                return null;
            }
        }
    }
}
