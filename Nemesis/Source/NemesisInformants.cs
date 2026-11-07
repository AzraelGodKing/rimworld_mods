using System.Collections.Generic;
using RimWorld;
using UnityEngine;
using Verse;

namespace Nemesis
{
    public static class NemesisInformants
    {
        public static int LeadCost(NemesisData data)
        {
            int baseCost = NemesisMod.Settings?.informantLeadCost ?? 180;
            int bump = data != null ? Mathf.RoundToInt(data.EffectiveAggression * 40f) : 0;
            return Mathf.Max(50, baseCost + bump);
        }

        public static bool TryBuyLead(NemesisData data, Map map)
        {
            string message;
            GameComponent_Nemesis comp = GameComponent_Nemesis.Instance;
            if (!(NemesisMod.Settings?.enableInformants ?? true))
            {
                Messages.Message("Nemesis_Intel_Disabled".Translate(), MessageTypeDefOf.RejectInput, historical: false);
                return false;
            }
            if (comp == null || data == null || !data.active)
            {
                Messages.Message("Nemesis_Intel_NoHunt".Translate(), MessageTypeDefOf.RejectInput, historical: false);
                return false;
            }

            int cost = LeadCost(data);
            if (!TryChargeSilver(map, cost))
            {
                Messages.Message("Nemesis_Intel_NoSilver".Translate(cost), MessageTypeDefOf.RejectInput, historical: false);
                return false;
            }

            int roll = Rand.RangeInclusive(0, 3);
            if (roll == 0)
            {
                Map home = SoftCompat.PreferHarassmentMap(map ?? Find.AnyPlayerHomeMap);
                NemesisTells.RecordSighting(data, home);
                message = "Nemesis_Intel_Tile".Translate(
                    data.lastKnownTileLabel ?? "—", cost);
            }
            else if (roll == 1)
            {
                Pawn pawn = comp.FindNemesisPawnFor(data);
                NemesisTells.RecordGear(data, pawn);
                message = "Nemesis_Intel_Gear".Translate(
                    data.lastGearSeen ?? NemesisTells.WeaponLabel(data), cost);
            }
            else if (roll == 2)
            {
                NemesisTells.RecordNote(data, "Nemesis_Note_RaidWarning".Translate());
                message = "Nemesis_Intel_Warning".Translate(cost);
                data.aggressionLevel = Mathf.Min(data.aggressionLevel + 0.15f, 10f);
            }
            else
            {
                data.lastKnownTileLabel = "Nemesis_Intel_FalseTile".Translate();
                NemesisTells.RecordNote(data, "Nemesis_Note_FalseLead".Translate());
                data.aggressionLevel = Mathf.Min(data.aggressionLevel + 0.25f, 10f);
                message = "Nemesis_Intel_False".Translate(cost);
            }

            Messages.Message(message, MessageTypeDefOf.NeutralEvent);
            return true;
        }

        public static bool TryPostBounty(NemesisData data, Map map, int silver)
        {
            if (data == null || !data.active || silver <= 0)
            {
                Messages.Message("Nemesis_Intel_NoHunt".Translate(), MessageTypeDefOf.RejectInput, historical: false);
                return false;
            }
            if (!TryChargeSilver(map, silver))
            {
                Messages.Message("Nemesis_Intel_NoSilver".Translate(silver), MessageTypeDefOf.RejectInput, historical: false);
                return false;
            }
            data.bountySilver += silver;
            Messages.Message("Nemesis_Bounty_Posted".Translate(data.bountySilver), MessageTypeDefOf.PositiveEvent);
            return true;
        }

        static bool TryChargeSilver(Map map, int cost)
        {
            if (map == null)
                map = Find.AnyPlayerHomeMap;
            if (map == null || cost <= 0)
                return false;
            List<Thing> live = map.listerThings.ThingsOfDef(ThingDefOf.Silver);
            var silver = new List<Thing>(live.Count);
            int have = 0;
            for (int i = 0; i < live.Count; i++)
            {
                Thing stack = live[i];
                if (stack == null || stack.Destroyed || !stack.Spawned) continue;
                if (!stack.IsInAnyStorage() && !map.areaManager.Home[stack.Position]) continue;
                silver.Add(stack);
                have += stack.stackCount;
            }
            if (have < cost)
                return false;
            int left = cost;
            for (int i = 0; i < silver.Count && left > 0; i++)
            {
                Thing t = silver[i];
                if (t == null || t.Destroyed) continue;
                int take = Mathf.Min(t.stackCount, left);
                t.SplitOff(take).Destroy(DestroyMode.Vanish);
                left -= take;
            }
            return left <= 0;
        }
    }
}
