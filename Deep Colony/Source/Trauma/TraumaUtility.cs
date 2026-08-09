using System.Collections.Generic;
using RimWorld;
using UnityEngine;
using Verse;

namespace DeepColony
{
    public static class TraumaUtility
    {
        /// <summary>
        /// Applies a trauma thought to <paramref name="victim"/>, optionally linked to
        /// <paramref name="source"/> (e.g. the pawn who died) and/or a remembered faction.
        /// Existing matching trauma is renewed rather than stacked.
        /// </summary>
        public static void ApplyTrauma(Pawn victim, TraumaDef def, Pawn source = null, Faction sourceFaction = null)
        {
            if (!DeepColonySettings.Get.enableTrauma) return;
            if (victim?.needs?.mood?.thoughts == null) return;
            if (def?.thoughtDef == null) return;

            var comp = victim.TryGetComp<Comp_DeepColony>();
            if (comp != null && !comp.RollTraumaApplyChance(def))
                return;

            Faction faction = sourceFaction ?? source?.Faction;

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
                if (faction != null && !faction.IsPlayer)
                    existing.RememberFaction(faction);
                return;
            }

            var thought = (Thought_Trauma)ThoughtMaker.MakeThought(def.thoughtDef);
            thought.traumaDef = def;
            thought.otherPawn = source;
            if (faction != null && !faction.IsPlayer)
                thought.RememberFaction(faction);
            victim.needs.mood.thoughts.memories.TryGainMemory(thought, source);

            // Resilience: start partway healed on re-occurrence.
            if (comp != null)
            {
                float skip = comp.TraumaDurationSkipFraction(def);
                if (skip > 0f && thought.DurationTicks > 0)
                    thought.age = Mathf.RoundToInt(thought.DurationTicks * skip);
            }

            if (faction != null && !faction.IsPlayer)
                GrudgeUtility.RememberFaction(victim, faction);

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
        /// Advances trauma memory age toward expiry. Scales by counselor Social,
        /// patient opinion, room impressiveness, confidant bond, and settings.
        /// </summary>
        public static void ApplyTherapy(Pawn counselor, Pawn patient)
        {
            if (!DeepColonySettings.Get.enableTrauma) return;
            if (patient?.needs?.mood?.thoughts == null) return;

            bool healed = false;
            float scale = DeepColonySettings.Get.therapyHealScale
                * ConfidantUtility.TherapyBonusBetween(counselor, patient)
                * TherapyQualityMultiplier(counselor, patient);

            var recovered = new List<TraumaDef>();
            foreach (Thought_Memory mem in patient.needs.mood.thoughts.memories.Memories)
            {
                if (mem is not Thought_Trauma tt) continue;

                int healing = tt.traumaDef?.therapyHealingPerSession ?? 60000;
                healing = Mathf.RoundToInt(healing * scale);
                int duration = tt.DurationTicks;
                if (duration <= 0) continue;
                tt.age = System.Math.Min(duration, tt.age + healing);
                healed = true;

                if (tt.age >= duration && tt.traumaDef != null)
                    recovered.Add(tt.traumaDef);
            }

            for (int i = 0; i < recovered.Count; i++)
            {
                TraumaDef def = recovered[i];
                RemoveTrauma(patient, def);
                TraumaRecoveryUtility.NotifyRecovered(patient, def, fromTherapy: true);
            }

            if (healed)
            {
                MoteMaker.ThrowText(patient.DrawPos, patient.Map,
                    "DC_TherapyProgress".Translate(), 3f);
            }

            // Sustained counseling also eases chronic stress (B04).
            if (DeepColonySettings.Get.enableChronicTrauma)
                TraumaRecoveryUtility.HealChronicFromTherapy(patient);
        }

        /// <summary>B07 — Social skill, opinion, and room quality scale therapy.</summary>
        public static float TherapyQualityMultiplier(Pawn counselor, Pawn patient)
        {
            float mult = 1f;

            if (counselor?.skills != null)
            {
                int social = counselor.skills.GetSkill(SkillDefOf.Social)?.Level ?? 0;
                mult *= 0.70f + social * 0.03f; // 0.70 @0 → 1.30 @20
            }

            if (counselor != null && patient?.relations != null)
            {
                int opinion = patient.relations.OpinionOf(counselor);
                mult *= 1f + Mathf.Clamp(opinion, -100, 100) * 0.0015f;
            }

            Room room = counselor?.GetRoom();
            if (room == null || room.PsychologicallyOutdoors)
                room = patient?.GetRoom();
            if (room != null && !room.PsychologicallyOutdoors)
            {
                float impress = room.GetStat(RoomStatDefOf.Impressiveness);
                if (impress >= 80f) mult *= 1.15f;
                else if (impress >= 50f) mult *= 1.10f;
                else if (impress >= 30f) mult *= 1.05f;

                float beauty = room.GetStat(RoomStatDefOf.Beauty);
                if (beauty >= 25f) mult *= 1.05f;
            }

            return Mathf.Clamp(mult, 0.50f, 1.85f);
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

        public static void CollectActiveTraumas(Pawn pawn, List<TraumaDef> into)
        {
            into.Clear();
            if (pawn?.needs?.mood?.thoughts == null) return;
            foreach (Thought_Memory mem in pawn.needs.mood.thoughts.memories.Memories)
            {
                if (mem is Thought_Trauma tt && tt.traumaDef != null && !into.Contains(tt.traumaDef))
                    into.Add(tt.traumaDef);
            }
        }

        public static int CountTraumatizedInRoom(Pawn pawn)
        {
            if (pawn?.Map == null || !pawn.Spawned) return 0;
            Room room = pawn.GetRoom();
            if (room == null || room.PsychologicallyOutdoors) return 0;
            int n = 0;
            foreach (Pawn p in pawn.Map.mapPawns.FreeColonistsSpawned)
            {
                if (p.Dead || p.Downed || p.InMentalState) continue;
                if (!HasAnyTrauma(p)) continue;
                if (p.GetRoom() == room) n++;
            }
            return n;
        }

        public static void ApplyTherapyToRoom(Pawn counselor, Pawn focal)
        {
            if (focal?.Map == null) return;
            Room room = focal.GetRoom();
            if (room == null || room.PsychologicallyOutdoors)
            {
                ApplyTherapy(counselor, focal);
                return;
            }

            foreach (Pawn p in focal.Map.mapPawns.FreeColonistsSpawned)
            {
                if (p.Dead || !HasAnyTrauma(p)) continue;
                if (p.GetRoom() != room) continue;
                ApplyTherapy(counselor, p);
            }
        }
    }
}
