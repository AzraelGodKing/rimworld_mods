using System.Collections.Generic;
using RimWorld;
using Verse;
using Verse.AI.Group;

namespace DeepColony
{
    /// <summary>C18 — burying or burning the body ages violent-loss trauma faster.</summary>
    public static class FuneralUtility
    {
        private const int EaseTicks = 90000; // 1.5 days of therapy-equivalent aging

        public static void NotifyBodyLaidToRest(Pawn dead, string reasonKey)
        {
            NotifyBodyLaidToRest(dead, reasonKey, dead?.thingIDNumber ?? 0);
        }

        // The funeral ritual uses its own key (the bitwise complement of the id), so the
        // burial that comes first doesn't use up the funeral's ease.
        private static void NotifyBodyLaidToRest(Pawn dead, string reasonKey, int processedKey)
        {
            if (!DeepColonySettings.Get.enableTrauma) return;
            if (dead == null || !dead.RaceProps.Humanlike) return;

            var gc = GameComp_DeepColony.Instance;
            if (gc == null) return;
            if (gc.funeralProcessedCorpses == null)
                gc.funeralProcessedCorpses = new HashSet<int>();
            if (!gc.funeralProcessedCorpses.Add(processedKey)) return;

            int easedCount = EaseAllMourners(dead);
            if (easedCount <= 0) return;

            Messages.Message(
                (reasonKey ?? "DC_FuneralEase").Translate(dead.LabelShort.Named("PAWN")),
                MessageTypeDefOf.PositiveEvent);
        }

        /// <summary>C18 leftover / D03 — Ideology funeral ritual (fail-open; Lord.Cleanup).</summary>
        public static void NotifyIdeologyRitual(LordJob lordJob)
        {
            if (!ModsConfig.IdeologyActive) return;
            if (!LooksLikeFuneralRitual(lordJob, out Pawn dead)) return;
            NotifyBodyLaidToRest(dead, "DC_FuneralRitual", ~dead.thingIDNumber);
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

        // Typed: Precept.def is a field, so the old GetProperty("def") never matched (AZR-623).
        // A funeral's target is the grave or sarcophagus holding the corpse.
        private static bool LooksLikeFuneralRitual(LordJob lordJob, out Pawn dead)
        {
            dead = null;
            if (lordJob is not LordJob_Ritual ritualJob) return false;
            Precept_Ritual ritual = ritualJob.Ritual;
            if (!IsFuneral(ritual?.def) && !IsFuneral(ritual?.behavior?.def)) return false;

            Thing target = ritualJob.selectedTarget.Thing;
            dead = target switch
            {
                Building_CorpseCasket casket => casket.Corpse?.InnerPawn,
                Corpse corpse => corpse.InnerPawn,
                Pawn p when p.Dead => p,
                _ => null
            };
            return dead != null;
        }

        private static bool IsFuneral(Def def)
        {
            return def?.defName != null
                && def.defName.IndexOf("Funeral", System.StringComparison.OrdinalIgnoreCase) >= 0;
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
