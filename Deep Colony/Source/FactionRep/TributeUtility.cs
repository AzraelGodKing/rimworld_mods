using System.Collections.Generic;
using RimWorld;
using UnityEngine;
using Verse;

namespace DeepColony
{
    /// <summary>C15 — spend silver or a gift to write a ledger row (still AddFactionDrift).</summary>
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
            ExternalDiplomacySoftCompat.OnTribute(faction);
            Messages.Message(
                "DC_TributeSent".Translate(faction.Name.Named("FACTION"), SilverCost),
                MessageTypeDefOf.PositiveEvent,
                false);
        }

        public static bool IsTributeGood(Thing t)
        {
            if (t == null || t.Destroyed) return false;
            if (t.def == ThingDefOf.Silver || t.def == ThingDefOf.Gold || t.def == ThingDefOf.Jade)
                return true;
            if (t is Corpse || t is MinifiedThing) return false;
            if (t.def.IsCorpse) return false;
            float unit = t.MarketValue;
            if (unit < 50f) return false;
            return unit * t.stackCount + 0.01f >= SilverCost;
        }

        public static bool CanTributeThing(Thing t, Faction faction, out string reason)
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
            if (!IsTributeGood(t))
            {
                reason = "DC_TributeNeedGift".Translate();
                return false;
            }
            if (t.MarketValue * t.stackCount + 0.01f < SilverCost)
            {
                reason = "DC_TributeNeedGift".Translate();
                return false;
            }
            return true;
        }

        public static void TrySendTributeThing(Thing t, Faction faction)
        {
            if (!CanTributeThing(t, faction, out string reason))
            {
                Messages.Message(reason, MessageTypeDefOf.RejectInput, false);
                return;
            }
            if (!TryConsumeValue(t, SilverCost))
            {
                Messages.Message("DC_TributeNeedGift".Translate(), MessageTypeDefOf.RejectInput, false);
                return;
            }

            GameComp_DeepColony.Instance?.AddFactionDrift(faction, Drift, FactionRepReason.Tribute);
            ExternalDiplomacySoftCompat.OnTribute(faction);
            Messages.Message(
                "DC_TributeGiftSent".Translate(faction.Name.Named("FACTION"), t.LabelNoCount.Named("GIFT")),
                MessageTypeDefOf.PositiveEvent,
                false);
        }

        public static IEnumerable<Faction> TributeFactions()
        {
            return Find.FactionManager.AllFactionsListForReading.FindAll(
                f => !f.IsPlayer && !f.defeated && !f.Hidden);
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
            var stacks = new List<Thing>(
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

        private static bool TryConsumeValue(Thing t, float need)
        {
            if (t == null || t.Destroyed) return false;
            float unit = t.MarketValue;
            if (unit <= 0.01f) return false;
            int count = Mathf.CeilToInt(need / unit);
            count = Mathf.Clamp(count, 1, t.stackCount);
            if (unit * count + 0.01f < need) return false;
            t.SplitOff(count).Destroy();
            return true;
        }
    }
}
