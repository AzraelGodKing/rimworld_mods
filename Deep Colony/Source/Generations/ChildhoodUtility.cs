using RimWorld;
using Verse;

namespace DeepColony
{
    /// <summary>C17 — colony-born kids keep a short "I grew up here" thought into adulthood.</summary>
    public static class ChildhoodUtility
    {
        public static void TryGrant(Pawn pawn)
        {
            if (!DeepColonySettings.Get.enableInheritance) return;
            if (!ModsConfig.BiotechActive) return;
            if (pawn == null || !pawn.RaceProps.Humanlike) return;
            if (!pawn.IsColonistPlayerControlled) return;

            var comp = pawn.TryGetComp<Comp_DeepColony>();
            if (comp == null || !comp.bornInColony) return;
            if (comp.childhoodMemoryGranted) return;
            if (comp.grewInGrowthVat) return;
            if (pawn.DevelopmentalStage < DevelopmentalStage.Adult) return;

            comp.childhoodMemoryGranted = true;
            if (DC_DefOf.DC_Thought_GrewUpHere == null || pawn.needs?.mood?.thoughts == null)
                return;

            var thought = (Thought_Memory)ThoughtMaker.MakeThought(DC_DefOf.DC_Thought_GrewUpHere);
            pawn.needs.mood.thoughts.memories.TryGainMemory(thought);
            FamilyLetterUtility.NotifyComingOfAge(pawn);
            Messages.Message(
                "DC_GrewUpHere".Translate(pawn.LabelShort.Named("PAWN")),
                pawn, MessageTypeDefOf.PositiveEvent, false);
        }

        public static void NoteBirth(Pawn baby)
        {
            if (baby == null) return;
            var comp = baby.TryGetComp<Comp_DeepColony>();
            if (comp == null) return;
            if (baby.Faction != null && baby.Faction.IsPlayer)
                comp.bornInColony = true;
        }

        public static void NoteGrowthVat(Pawn pawn)
        {
            var comp = pawn?.TryGetComp<Comp_DeepColony>();
            if (comp != null) comp.grewInGrowthVat = true;
        }
    }
}
