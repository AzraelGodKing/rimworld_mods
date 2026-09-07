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

        public static bool TryBuyLead(Map map, out string message)
        {
            GameComponent_Nemesis comp = GameComponent_Nemesis.Instance;
            NemesisData data = comp?.Data;
            if (!(NemesisMod.Settings?.enableInformants ?? true))
            {
                message = "Nemesis_Intel_Disabled".Translate();
                return false;
            }
            if (comp == null || data == null || !data.active)
            {
                message = "Nemesis_Intel_NoHunt".Translate();
                return false;
            }

            int cost = LeadCost(data);
            if (!TryChargeSilver(map, cost))
            {
                message = "Nemesis_Intel_NoSilver".Translate(cost);
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
                Pawn pawn = comp.FindNemesisPawn();
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

        public static void SetBounty(int silver)
        {
            GameComponent_Nemesis comp = GameComponent_Nemesis.Instance;
            if (comp == null)
                return;
            comp.bountySilver = Mathf.Max(0, silver);
        }

        public static bool TryPostBounty(Map map, int silver, out string message)
        {
            GameComponent_Nemesis comp = GameComponent_Nemesis.Instance;
            if (comp?.Data == null || !comp.Data.active)
            {
                message = "Nemesis_Intel_NoHunt".Translate();
                return false;
            }
            if (silver <= 0)
            {
                SetBounty(0);
                message = "Nemesis_Bounty_Cleared".Translate();
                Messages.Message(message, MessageTypeDefOf.NeutralEvent);
                return true;
            }
            if (!TryChargeSilver(map, silver))
            {
                message = "Nemesis_Intel_NoSilver".Translate(silver);
                return false;
            }
            SetBounty(comp.bountySilver + silver);
            message = "Nemesis_Bounty_Posted".Translate(comp.bountySilver);
            Messages.Message(message, MessageTypeDefOf.PositiveEvent);
            return true;
        }

        static bool TryChargeSilver(Map map, int cost)
        {
            if (map == null)
                map = Find.AnyPlayerHomeMap;
            if (map == null || cost <= 0)
                return false;
            List<Thing> silver = map.listerThings.ThingsOfDef(ThingDefOf.Silver);
            int have = 0;
            for (int i = 0; i < silver.Count; i++)
                have += silver[i].stackCount;
            if (have < cost)
                return false;
            int left = cost;
            for (int i = 0; i < silver.Count && left > 0; i++)
            {
                Thing t = silver[i];
                int take = Mathf.Min(t.stackCount, left);
                t.SplitOff(take).Destroy(DestroyMode.Vanish);
                left -= take;
            }
            return true;
        }
    }
}
