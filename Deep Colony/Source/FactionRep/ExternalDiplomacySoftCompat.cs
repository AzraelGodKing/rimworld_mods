using RimWorld;
using Verse;

namespace DeepColony
{
    /// <summary>
    /// Notable Deep Colony beats → Despicable karma / RimPacts trust.
    /// Does not copy the goodwill ledger. Execute/release stay with Despicable.
    /// </summary>
    public static class ExternalDiplomacySoftCompat
    {
        public static void OnTribute(Faction faction)
        {
            DespicableKarmaSoftCompat.Notify(2, 0, "DC_Tribute",
                "DC_Compat_Karma_Tribute".Translate(), null, faction);
            RimPactsTrustSoftCompat.Notify(faction, 4);
        }

        public static void OnEnvoyVisit(Faction faction, Pawn envoy)
        {
            DespicableKarmaSoftCompat.Notify(1, 0, "DC_EnvoyVisit",
                "DC_Compat_Karma_Envoy".Translate(), envoy, faction);
            RimPactsTrustSoftCompat.Notify(faction, 3);
        }

        public static void OnFamilyJoin(Pawn pawn, Faction oldFaction, bool hostileDefect)
        {
            if (hostileDefect)
            {
                DespicableKarmaSoftCompat.Notify(2, 0, "DC_FamilyDefect",
                    "DC_Compat_Karma_FamilyDefect".Translate(), pawn, oldFaction);
                RimPactsTrustSoftCompat.Notify(oldFaction, -5);
            }
            else
            {
                DespicableKarmaSoftCompat.Notify(1, 0, "DC_FamilyJoin",
                    "DC_Compat_Karma_FamilyJoin".Translate(), pawn, oldFaction);
            }
        }

        public static void OnKinExecuted(Pawn victim)
        {
            // Despicable already karmas prisoner execution. Trust only.
            Faction faction = victim?.Faction;
            if (faction == null || faction.IsPlayer) faction = victim?.HomeFaction;
            if (faction == null || faction.IsPlayer) return;
            RimPactsTrustSoftCompat.Notify(faction, -8);
        }
    }
}
