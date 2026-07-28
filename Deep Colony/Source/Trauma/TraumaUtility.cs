using System.Collections.Generic;
using RimWorld;
using Verse;

namespace DeepColony
{
    public static class TraumaUtility
    {
        /// <summary>
        /// Applies a trauma thought to <paramref name="victim"/>, optionally linked to
        /// <paramref name="source"/> (e.g. the pawn who died). Existing matching trauma
        /// is renewed (age reset) rather than stacked.
        /// </summary>
        public static void ApplyTrauma(Pawn victim, TraumaDef def, Pawn source = null)
        {
            if (victim?.needs?.mood?.thoughts == null) return;
            if (def?.thoughtDef == null) return;

            Thought_Trauma existing = null;
            foreach (Thought_Memory mem in victim.needs.mood.thoughts.memories.Memories)
            {
                if (mem is Thought_Trauma tt && tt.traumaDef == def)
                {
                    existing = tt;
                    break;
                }
            }

            if (existing != null)
            {
                existing.Renew();
                return;
            }

            var thought = (Thought_Trauma)ThoughtMaker.MakeThought(def.thoughtDef);
            thought.traumaDef = def;
            thought.otherPawn = source;
            victim.needs.mood.thoughts.memories.TryGainMemory(thought, source);

            if (!def.triggerMessage.NullOrEmpty())
            {
                Messages.Message(
                    "DC_TraumaApplied".Translate(
                        victim.LabelShort.Named("PAWN"),
                        def.triggerMessage.Named("EVENT")),
                    victim,
                    MessageTypeDefOf.NegativeEvent,
                    historical: false);
            }
        }

        /// <summary>Removes a specific trauma memory (e.g. upgrade Violent Loss → Bereavement).</summary>
        public static void RemoveTrauma(Pawn pawn, TraumaDef def)
        {
            if (pawn?.needs?.mood?.thoughts == null || def == null) return;

            List<Thought_Memory> memories = pawn.needs.mood.thoughts.memories.Memories;
            for (int i = memories.Count - 1; i >= 0; i--)
            {
                if (memories[i] is Thought_Trauma tt && tt.traumaDef == def)
                    pawn.needs.mood.thoughts.memories.RemoveMemory(tt);
            }
        }

        /// <summary>
        /// Advances trauma memory age toward expiry. (Vanilla memories expire when
        /// age reaches DurationTicks — increasing age heals faster; decreasing it
        /// would prolong the trauma.)
        /// </summary>
        public static void ApplyTherapy(Pawn counselor, Pawn patient)
        {
            if (patient?.needs?.mood?.thoughts == null) return;

            bool healed = false;
            foreach (Thought_Memory mem in patient.needs.mood.thoughts.memories.Memories)
            {
                if (mem is Thought_Trauma tt)
                {
                    int healing = tt.traumaDef?.therapyHealingPerSession ?? 60000;
                    int duration = tt.DurationTicks;
                    if (duration <= 0) continue;
                    tt.age = System.Math.Min(duration, tt.age + healing);
                    healed = true;
                }
            }

            if (healed)
            {
                MoteMaker.ThrowText(patient.DrawPos, patient.Map,
                    "DC_TherapyProgress".Translate(), 3f);
            }
        }

        public static bool HasAnyTrauma(Pawn pawn)
        {
            if (pawn?.needs?.mood?.thoughts == null) return false;
            foreach (Thought_Memory mem in pawn.needs.mood.thoughts.memories.Memories)
            {
                if (mem is Thought_Trauma) return true;
            }
            return false;
        }

        public static bool HasTrauma(Pawn pawn, TraumaDef def)
        {
            if (pawn?.needs?.mood?.thoughts == null || def == null) return false;
            foreach (Thought_Memory mem in pawn.needs.mood.thoughts.memories.Memories)
            {
                if (mem is Thought_Trauma tt && tt.traumaDef == def) return true;
            }
            return false;
        }
    }
}
