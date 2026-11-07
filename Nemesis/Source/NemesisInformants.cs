#nullable enable
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

        public static bool TryBuyLead(NemesisData data, Map? map)
        {
            string message;
            GameComponent_Nemesis? comp = GameComponent_Nemesis.Instance;
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

            int roll = RollLead(data);
            Map? home = SoftCompat.PreferHarassmentMap(map ?? Find.AnyPlayerHomeMap);
            if (roll == 0)
            {
                // A real lead: a camp where the nemesis waits (AZR-555).
                if (NemesisHuntSites.TryCreateCamp(data, real: true, home))
                    message = "Nemesis_Intel_Camp".Translate(data.lastKnownTileLabel ?? "—", cost);
                else
                {
                    NemesisHuntSites.RecordHeading(data, home);
                    message = "Nemesis_Intel_Tile".Translate(data.lastKnownTileLabel ?? "—", cost);
                }
            }
            else if (roll == 1)
            {
                Pawn? pawn = comp.FindNemesisPawnFor(data);
                NemesisTells.RecordGear(data, pawn);
                message = "Nemesis_Intel_Gear".Translate(
                    data.lastGearSeen ?? NemesisTells.WeaponLabel(data), cost);
            }
            else if (roll == 2)
            {
                NemesisTells.RecordNote(data, "Nemesis_Note_RaidWarning");
                message = "Nemesis_Intel_Warning".Translate(cost);
                data.aggressionLevel = Mathf.Min(data.aggressionLevel + 0.15f, 10f);
            }
            else if (NemesisHuntSites.TryCreateCamp(data, real: false, home))
            {
                // A false lead looks exactly like a real one until a caravan gets there.
                message = "Nemesis_Intel_Camp".Translate(data.lastKnownTileLabel ?? "—", cost);
            }
            else
            {
                NemesisTells.RecordNote(data, "Nemesis_Note_FalseLead");
                data.aggressionLevel = Mathf.Min(data.aggressionLevel + 0.25f, 10f);
                message = "Nemesis_Intel_False".Translate(cost);
            }

            Messages.Message(message, MessageTypeDefOf.NeutralEvent);
            return true;
        }

        public static bool TryPostBounty(NemesisData data, Map? map, int silver)
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

        /// <summary>
        /// 0 real camp, 1 gear, 2 raid warning, 3 false lead (a camp without the nemesis). A posted bounty buys better informants:
        /// a 600-silver bounty cuts the false-lead weight from 1 to 0.2 and doubles the tile
        /// weight (AZR-662).
        /// </summary>
        private static int RollLead(NemesisData data)
        {
            float b = Mathf.Clamp01(data.bountySilver / 600f);
            float tile = 1f + b;
            float gear = 1f;
            float warning = 1f;
            float falseLead = Mathf.Lerp(1f, 0.2f, b);
            float roll = Rand.Value * (tile + gear + warning + falseLead);
            if ((roll -= tile) < 0f) return 0;
            if ((roll -= gear) < 0f) return 1;
            if ((roll -= warning) < 0f) return 2;
            return 3;
        }

        /// <summary>
        /// A hunt that ends in the colony's favour pays the bounty out to the informants who
        /// helped; any other end returns it by drop pod (AZR-657). Called before the hunt's
        /// bounty is cleared.
        /// </summary>
        public static void SettleBounty(NemesisData? data, bool won)
        {
            if (data == null) return;
            int silver = data.bountySilver;
            if (silver <= 0) return;
            data.bountySilver = 0;
            string name = data.nemesisName ?? "Nemesis_Phrase_Someone".Translate();
            if (won)
            {
                Messages.Message("Nemesis_Bounty_PaidOut".Translate(silver, name), MessageTypeDefOf.PositiveEvent);
                return;
            }

            Map? map = SoftCompat.PreferHarassmentMap(Find.AnyPlayerHomeMap);
            if (map == null) return;
            var stacks = new List<Thing>();
            int left = silver;
            while (left > 0)
            {
                Thing stack = ThingMaker.MakeThing(ThingDefOf.Silver);
                stack.stackCount = Mathf.Min(left, ThingDefOf.Silver.stackLimit);
                left -= stack.stackCount;
                stacks.Add(stack);
            }
            IntVec3 spot = DropCellFinder.TradeDropSpot(map);
            DropPodUtility.DropThingsNear(spot, map, stacks);
            Messages.Message("Nemesis_Bounty_Refunded".Translate(silver, name),
                new TargetInfo(spot, map), MessageTypeDefOf.NeutralEvent);
        }

        static bool TryChargeSilver(Map? map, int cost)
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
