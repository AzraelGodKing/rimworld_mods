using System.Collections.Generic;
using RimWorld;
using Verse;

namespace DeepColony
{
    /// <summary>C18 — burying or burning the body ages violent-loss trauma faster.</summary>
    public static class FuneralUtility
    {
        private const int EaseTicks = 90000; // 1.5 days of therapy-equivalent aging

        public static void NotifyBodyLaidToRest(Pawn dead, string reasonKey)
        {
            if (!DeepColonySettings.Get.enableTrauma) return;
            if (dead == null || !dead.RaceProps.Humanlike) return;

            var gc = GameComp_DeepColony.Instance;
            if (gc == null) return;
            if (gc.funeralProcessedCorpses == null)
                gc.funeralProcessedCorpses = new HashSet<int>();
            if (!gc.funeralProcessedCorpses.Add(dead.thingIDNumber)) return;

            int easedCount = EaseAllMourners(dead);
            if (easedCount <= 0) return;

            Messages.Message(
                (reasonKey ?? "DC_FuneralEase").Translate(dead.LabelShort.Named("PAWN")),
                MessageTypeDefOf.PositiveEvent);
        }

        /// <summary>C18 leftover / D03 — Ideology funeral ritual (fail-open; Lord.Cleanup).</summary>
        public static void NotifyIdeologyRitual(object lordJob)
        {
            if (!ModsConfig.IdeologyActive) return;
            if (!LooksLikeFuneralRitual(lordJob, out Pawn dead)) return;
            NotifyBodyLaidToRest(dead, "DC_FuneralRitual");
        }

        private static int EaseAllMourners(Pawn dead)
        {
            int easedCount = 0;
            foreach (Map map in Find.Maps)
            {
                foreach (Pawn colonist in map.mapPawns.FreeColonistsSpawned)
                {
                    if (colonist.Dead || colonist == dead) continue;
                    if (EaseLoss(colonist, dead)) easedCount++;
                }
            }
            return easedCount;
        }

        private static bool LooksLikeFuneralRitual(object lordJob, out Pawn dead)
        {
            dead = null;
            if (lordJob == null) return false;
            System.Type t = lordJob.GetType();
            if (t.Name.IndexOf("Ritual", System.StringComparison.OrdinalIgnoreCase) < 0)
                return false;

            object ritual = t.GetProperty("Ritual")?.GetValue(lordJob);
            string defName = null;
            if (ritual != null)
            {
                object def = ritual.GetType().GetProperty("def")?.GetValue(ritual);
                defName = (def as Def)?.defName;
                object behavior = ritual.GetType().GetProperty("behaviorDef")?.GetValue(ritual)
                    ?? ritual.GetType().GetProperty("ritualDef")?.GetValue(ritual);
                if (defName.NullOrEmpty())
                    defName = (behavior as Def)?.defName;
            }

            bool funeral = !defName.NullOrEmpty()
                && defName.IndexOf("Funeral", System.StringComparison.OrdinalIgnoreCase) >= 0;
            if (!funeral)
            {
                object assignments = t.GetProperty("assignments")?.GetValue(lordJob)
                    ?? t.GetProperty("ritual")?.GetValue(lordJob);
                string blob = (ritual?.ToString() ?? "") + (lordJob.ToString() ?? "");
                funeral = blob.IndexOf("Funeral", System.StringComparison.OrdinalIgnoreCase) >= 0;
            }
            if (!funeral) return false;

            dead = t.GetField("corpse")?.GetValue(lordJob) as Pawn
                ?? (t.GetField("corpse")?.GetValue(lordJob) as Corpse)?.InnerPawn
                ?? t.GetProperty("Organizer")?.GetValue(lordJob) as Pawn;
            if (dead == null)
            {
                object corpseObj = t.GetProperty("selectedTarget")?.GetValue(lordJob)
                    ?? t.GetField("selectedTarget")?.GetValue(lordJob);
                if (corpseObj is Corpse c) dead = c.InnerPawn;
                else if (corpseObj is Pawn p && p.Dead) dead = p;
            }
            if (dead == null)
            {
                // Last resort: most recently processed is skipped by HashSet; scan selected ritual pawn.
                var selected = t.GetProperty("PawnWithMostPresence")?.GetValue(lordJob) as Pawn;
                if (selected != null && selected.Dead) dead = selected;
            }
            return dead != null;
        }

        private static bool EaseLoss(Pawn mourner, Pawn dead)
        {
            if (mourner.needs?.mood?.thoughts == null) return false;
            bool eased = false;
            int extra = IsSpouseOrLover(mourner, dead) ? EaseTicks / 2 : 0;
            int ticks = EaseTicks + extra;
            foreach (Thought_Memory mem in mourner.needs.mood.thoughts.memories.Memories)
            {
                if (mem is not Thought_Trauma tt) continue;
                if (tt.traumaDef != DC_DefOf.DC_Trauma_ViolentLoss
                    && tt.traumaDef != DC_DefOf.DC_Trauma_BereavementShock)
                    continue;
                if (tt.otherPawn != null && tt.otherPawn != dead) continue;

                int duration = tt.DurationTicks;
                if (duration <= 0) continue;
                tt.age = System.Math.Min(duration, tt.age + ticks);
                eased = true;
            }

            if (!eased) return false;
            MoteMaker.ThrowText(mourner.DrawPos, mourner.Map,
                extra > 0
                    ? "DC_FuneralSpouseMote".Translate()
                    : "DC_FuneralEaseMote".Translate(), 3f);

            if (extra > 0 && DC_DefOf.DC_Thought_SpouseRemembrance != null)
            {
                var thought = (Thought_Memory)ThoughtMaker.MakeThought(DC_DefOf.DC_Thought_SpouseRemembrance);
                mourner.needs.mood.thoughts.memories.TryGainMemory(thought);
            }
            return true;
        }

        private static bool IsSpouseOrLover(Pawn mourner, Pawn dead)
        {
            if (mourner?.relations == null || dead == null) return false;
            if (LovePartnerRelationUtility.LovePartnerRelationExists(mourner, dead)) return true;
            return mourner.relations.DirectRelationExists(PawnRelationDefOf.Spouse, dead)
                || mourner.relations.DirectRelationExists(PawnRelationDefOf.Lover, dead)
                || mourner.relations.DirectRelationExists(PawnRelationDefOf.Fiance, dead);
        }
    }
}
