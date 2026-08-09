using RimWorld;
using Verse;

namespace DeepColony
{
    public static class HeirloomUtility
    {
        public static void TryCreateFromDeath(Pawn victim)
        {
            if (!DeepColonySettings.Get.enableHeirlooms) return;
            if (victim == null || !victim.RaceProps.Humanlike) return;
            if (victim.Faction != Faction.OfPlayer && !victim.IsColonist) return;

            var comp = victim.TryGetComp<Comp_DeepColony>();
            Thing gear = FindBestGear(victim);
            if (gear == null) return;

            string echoPerk = null;
            if (comp?.unlockedPerkDefNames != null && comp.unlockedPerkDefNames.Count > 0)
                echoPerk = comp.unlockedPerkDefNames[comp.unlockedPerkDefNames.Count - 1];

            string owner = victim.Name?.ToStringShort ?? victim.LabelShort;
            GameComp_DeepColony.Instance?.RegisterHeirloom(gear.thingIDNumber, owner, echoPerk);

            Messages.Message(
                "DC_HeirloomCreated".Translate(owner.Named("PAWN"), gear.LabelNoCount.Named("ITEM")),
                gear,
                MessageTypeDefOf.PositiveEvent,
                false);
        }

        private static Thing FindBestGear(Pawn pawn)
        {
            Thing best = null;
            int bestScore = -1;

            void Consider(Thing t)
            {
                if (t == null || t.Destroyed) return;
                if (!t.def.IsWeapon && !t.def.IsApparel) return;
                int score = 1;
                CompQuality q = t.TryGetComp<CompQuality>();
                if (q != null) score += (int)q.Quality;
                if (t.def.IsWeapon) score += 2;
                if (score > bestScore)
                {
                    bestScore = score;
                    best = t;
                }
            }

            if (pawn.equipment?.Primary != null)
                Consider(pawn.equipment.Primary);
            if (pawn.apparel?.WornApparel != null)
            {
                foreach (Apparel a in pawn.apparel.WornApparel)
                    Consider(a);
            }
            if (pawn.inventory?.innerContainer != null)
            {
                foreach (Thing t in pawn.inventory.innerContainer)
                    Consider(t);
            }
            return best;
        }

        public static void TickCarrier(Pawn pawn)
        {
            if (!DeepColonySettings.Get.enableHeirlooms) return;
            var gc = GameComp_DeepColony.Instance;
            if (gc == null || pawn == null) return;

            bool carrying = false;
            if (pawn.equipment?.Primary != null
                && gc.IsHeirloom(pawn.equipment.Primary.thingIDNumber))
                carrying = true;
            else if (pawn.apparel?.WornApparel != null)
            {
                foreach (Apparel a in pawn.apparel.WornApparel)
                {
                    if (gc.IsHeirloom(a.thingIDNumber))
                    {
                        carrying = true;
                        break;
                    }
                }
            }

            if (!carrying)
            {
                RemoveEcho(pawn);
                return;
            }

            if (DC_DefOf.DC_Thought_Heirloom != null && pawn.needs?.mood?.thoughts != null)
            {
                bool has = false;
                foreach (Thought_Memory mem in pawn.needs.mood.thoughts.memories.Memories)
                {
                    if (mem.def == DC_DefOf.DC_Thought_Heirloom)
                    {
                        has = true;
                        break;
                    }
                }
                if (!has)
                {
                    var thought = (Thought_Memory)ThoughtMaker.MakeThought(DC_DefOf.DC_Thought_Heirloom);
                    pawn.needs.mood.thoughts.memories.TryGainMemory(thought);
                }
            }

            if (DC_DefOf.DC_Hediff_HeirloomEcho != null && pawn.health != null
                && !pawn.health.hediffSet.HasHediff(DC_DefOf.DC_Hediff_HeirloomEcho))
            {
                pawn.health.AddHediff(DC_DefOf.DC_Hediff_HeirloomEcho);
            }
        }

        private static void RemoveEcho(Pawn pawn)
        {
            if (pawn?.health == null || DC_DefOf.DC_Hediff_HeirloomEcho == null) return;
            Hediff h = pawn.health.hediffSet.GetFirstHediffOfDef(DC_DefOf.DC_Hediff_HeirloomEcho);
            if (h != null) pawn.health.RemoveHediff(h);
        }
    }
}
