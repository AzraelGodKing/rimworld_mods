using System.Collections.Generic;
using RimWorld;
using Verse;

namespace DeepColony
{
    /// <summary>A08 scars + B06 resilience / post-traumatic growth.</summary>
    public static class TraumaRecoveryUtility
    {
        private const int RecoveriesForSeasoned = 2;

        public static void NotifyRecovered(Pawn pawn, TraumaDef def, bool fromTherapy)
        {
            if (pawn == null || def == null) return;
            if (!DeepColonySettings.Get.enableTrauma) return;

            var comp = pawn.TryGetComp<Comp_DeepColony>();
            if (comp == null) return;

            int count = comp.RecordTraumaRecovery(def);
            if (comp.trackedTraumaDefNames != null)
                comp.trackedTraumaDefNames.Remove(def.defName);
            TryApplyScar(pawn, def);

            // B04 — untreated natural expiry can leave chronic stress.
            if (!fromTherapy && DeepColonySettings.Get.enableChronicTrauma)
                TryApplyChronic(pawn);


            if (fromTherapy && !comp.seasonedGrowthGranted && comp.TotalTraumaRecoveries() >= RecoveriesForSeasoned)
            {
                comp.seasonedGrowthGranted = true;
                if (DC_DefOf.DC_Thought_Seasoned != null && pawn.needs?.mood?.thoughts != null)
                {
                    var thought = (Thought_Memory)ThoughtMaker.MakeThought(DC_DefOf.DC_Thought_Seasoned);
                    pawn.needs.mood.thoughts.memories.TryGainMemory(thought);
                }
                Messages.Message(
                    "DC_SeasonedGrowth".Translate(pawn.LabelShort.Named("PAWN")),
                    pawn,
                    MessageTypeDefOf.PositiveEvent,
                    false);
            }
            else if (fromTherapy)
            {
                Messages.Message(
                    "DC_TraumaRecovered".Translate(
                        pawn.LabelShort.Named("PAWN"),
                        def.LabelCap.Named("TRAUMA")),
                    pawn,
                    MessageTypeDefOf.PositiveEvent,
                    false);
            }

            _ = count;
        }

        private static void TryApplyScar(Pawn pawn, TraumaDef def)
        {
            if (DC_DefOf.DC_Thought_TraumaScar == null) return;
            if (pawn.needs?.mood?.thoughts == null) return;

            // Renew mild scar rather than stacking.
            foreach (Thought_Memory mem in pawn.needs.mood.thoughts.memories.Memories)
            {
                if (mem.def == DC_DefOf.DC_Thought_TraumaScar)
                {
                    mem.Renew();
                    return;
                }
            }

            var scar = (Thought_Memory)ThoughtMaker.MakeThought(DC_DefOf.DC_Thought_TraumaScar);
            pawn.needs.mood.thoughts.memories.TryGainMemory(scar);
        }

        private static void TryApplyChronic(Pawn pawn)
        {
            if (DC_DefOf.DC_Hediff_ChronicStress == null || pawn.health == null) return;
            Hediff existing = pawn.health.hediffSet.GetFirstHediffOfDef(DC_DefOf.DC_Hediff_ChronicStress);
            if (existing != null)
            {
                existing.Severity = UnityEngine.Mathf.Min(1f, existing.Severity + 0.25f);
            }
            else
            {
                Hediff h = HediffMaker.MakeHediff(DC_DefOf.DC_Hediff_ChronicStress, pawn);
                h.Severity = 0.35f;
                pawn.health.AddHediff(h);
            }
            Messages.Message(
                "DC_ChronicTrauma".Translate(pawn.LabelShort.Named("PAWN")),
                pawn, MessageTypeDefOf.NegativeEvent, false);
        }

        public static void HealChronicFromTherapy(Pawn pawn)
        {
            if (DC_DefOf.DC_Hediff_ChronicStress == null || pawn?.health == null) return;
            Hediff existing = pawn.health.hediffSet.GetFirstHediffOfDef(DC_DefOf.DC_Hediff_ChronicStress);
            if (existing == null) return;
            existing.Severity -= 0.15f;
            if (existing.Severity <= 0.05f)
                pawn.health.RemoveHediff(existing);
        }

        /// <summary>
        /// Detect traumas that expired naturally (no therapy) and award scar/resilience.
        /// </summary>
        public static void TickNaturalRecovery(Pawn pawn)
        {
            if (pawn == null || !DeepColonySettings.Get.enableTrauma) return;
            var comp = pawn.TryGetComp<Comp_DeepColony>();
            if (comp == null) return;

            var current = new List<string>();
            if (pawn.needs?.mood?.thoughts != null)
            {
                foreach (Thought_Memory mem in pawn.needs.mood.thoughts.memories.Memories)
                {
                    if (mem is Thought_Trauma tt && tt.traumaDef != null)
                        current.Add(tt.traumaDef.defName);
                }
            }

            if (comp.trackedTraumaDefNames == null)
                comp.trackedTraumaDefNames = new List<string>();

            if (IdeologyCounselUtility.CounselingIsStoic
                && pawn.needs?.mood?.thoughts != null)
            {
                // Undo 35% of natural aging so stoic fade is slower.
                foreach (Thought_Memory mem in pawn.needs.mood.thoughts.memories.Memories)
                {
                    if (mem is Thought_Trauma tt && tt.age > 0)
                        tt.age = System.Math.Max(0, tt.age - 875);
                }
            }

            comp.ClearUntreatedTraumaIfHealed();

            for (int i = comp.trackedTraumaDefNames.Count - 1; i >= 0; i--)
            {
                string name = comp.trackedTraumaDefNames[i];
                if (current.Contains(name)) continue;
                comp.trackedTraumaDefNames.RemoveAt(i);
                TraumaDef def = DefDatabase<TraumaDef>.GetNamedSilentFail(name);
                if (def != null)
                    NotifyRecovered(pawn, def, fromTherapy: false);
            }

            for (int i = 0; i < current.Count; i++)
            {
                if (!comp.trackedTraumaDefNames.Contains(current[i]))
                    comp.trackedTraumaDefNames.Add(current[i]);
            }
        }
    }
}
