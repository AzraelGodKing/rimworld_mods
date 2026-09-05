using RimWorld;
using Verse;

namespace DeepColony
{
    public static class ArchetypeUtility
    {
        public static void TryRefresh(Pawn pawn)
        {
            if (pawn == null) return;
            if (Scribe.mode == LoadSaveMode.LoadingVars || Scribe.mode == LoadSaveMode.PostLoadInit)
                return;
            var comp = pawn.TryGetComp<Comp_DeepColony>();
            if (comp == null) return;

            if (!DeepColonySettings.Get.enablePerks
                || !DeepColonySettings.Get.enableCrossSkillArchetypes)
            {
                ClearArchetype(pawn, comp);
                return;
            }

            ArchetypeDef best = null;
            foreach (ArchetypeDef def in DefDatabase<ArchetypeDef>.AllDefs)
            {
                if (def.skillA == null || def.skillB == null) continue;
                if (comp.HighestUnlockedPerkTierForSkill(def.skillA) < def.requiredTier) continue;
                if (comp.HighestUnlockedPerkTierForSkill(def.skillB) < def.requiredTier) continue;
                best = def;
                break; // first match is enough; defs ordered by author intent
            }

            if (best == null)
            {
                ClearArchetype(pawn, comp);
                return;
            }

            if (comp.activeArchetypeDefName == best.defName) return;

            ClearArchetype(pawn, comp);
            comp.activeArchetypeDefName = best.defName;
            if (best.hediff != null && pawn.health != null
                && !pawn.health.hediffSet.HasHediff(best.hediff))
            {
                pawn.health.AddHediff(best.hediff);
            }

            if (Comp_DeepColony.ShouldAnnounceAutoPerk(pawn))
            {
                Messages.Message(
                    "DC_ArchetypeGained".Translate(pawn.LabelShort.Named("PAWN"), best.LabelCap.Named("ARCH")),
                    pawn, MessageTypeDefOf.PositiveEvent, false);
            }
        }

        private static void ClearArchetype(Pawn pawn, Comp_DeepColony comp)
        {
            if (!comp.activeArchetypeDefName.NullOrEmpty())
            {
                ArchetypeDef old = DefDatabase<ArchetypeDef>.GetNamedSilentFail(comp.activeArchetypeDefName);
                if (old?.hediff != null && pawn.health != null)
                {
                    Hediff h = pawn.health.hediffSet.GetFirstHediffOfDef(old.hediff);
                    if (h != null) pawn.health.RemoveHediff(h);
                }
            }
            comp.activeArchetypeDefName = null;
        }
    }
}
