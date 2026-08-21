using RimWorld;
using Verse;

namespace DeepColony
{
    /// <summary>C15 — spend silver to write a ledger row (still AddFactionDrift).</summary>
    public static class TributeUtility
    {
        public const int SilverCost = 200;
        public const float Drift = 2.5f;

        public static bool CanTribute(Faction faction, out string reason)
        {
            reason = null;
            if (!DeepColonySettings.Get.enableFactionRep)
            {
                reason = "DC_RepDisabled".Translate();
                return false;
            }
            if (!DeepColonySettings.Get.enableApologyTribute)
            {
                reason = "DC_TributeDisabled".Translate();
                return false;
            }
            if (faction == null || faction.IsPlayer || faction.defeated)
            {
                reason = "DC_TributeNoFaction".Translate();
                return false;
            }
            Map map = Find.CurrentMap ?? (Find.Maps.Count > 0 ? Find.Maps[0] : null);
            if (map == null)
            {
                reason = "DC_TributeNoSilver".Translate();
                return false;
            }
            if (CountSilver(map) < SilverCost)
            {
                reason = "DC_TributeNoSilver".Translate();
                return false;
            }
            return true;
        }

        public static void TrySendTribute(Faction faction)
        {
            if (!CanTribute(faction, out string reason))
            {
                Messages.Message(reason, MessageTypeDefOf.RejectInput, false);
                return;
            }

            Map map = Find.CurrentMap ?? Find.Maps[0];
            if (!TryConsumeSilver(map, SilverCost))
            {
                Messages.Message("DC_TributeNoSilver".Translate(), MessageTypeDefOf.RejectInput, false);
                return;
            }

            GameComp_DeepColony.Instance?.AddFactionDrift(faction, Drift, FactionRepReason.Tribute);
            Messages.Message(
                "DC_TributeSent".Translate(faction.Name.Named("FACTION"), SilverCost),
                MessageTypeDefOf.PositiveEvent,
                false);
        }

        private static int CountSilver(Map map)
        {
            int n = 0;
            foreach (Thing t in map.listerThings.ThingsOfDef(ThingDefOf.Silver))
            {
                if (t.IsForbidden(Faction.OfPlayer)) continue;
                n += t.stackCount;
            }
            return n;
        }

        private static bool TryConsumeSilver(Map map, int amount)
        {
            if (CountSilver(map) < amount) return false;
            int remaining = amount;
            var stacks = new System.Collections.Generic.List<Thing>(
                map.listerThings.ThingsOfDef(ThingDefOf.Silver));
            for (int i = 0; i < stacks.Count && remaining > 0; i++)
            {
                Thing t = stacks[i];
                if (t.Destroyed || t.IsForbidden(Faction.OfPlayer)) continue;
                int take = System.Math.Min(remaining, t.stackCount);
                t.SplitOff(take).Destroy();
                remaining -= take;
            }
            return remaining <= 0;
        }
    }
}
