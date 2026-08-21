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

            foreach (Map map in Find.Maps)
            {
                foreach (Pawn colonist in map.mapPawns.FreeColonistsSpawned)
                {
                    if (colonist.Dead || colonist == dead) continue;
                    EaseLoss(colonist, dead);
                }
            }

            Messages.Message(
                (reasonKey ?? "DC_FuneralEase").Translate(dead.LabelShort.Named("PAWN")),
                MessageTypeDefOf.PositiveEvent);
        }

        private static void EaseLoss(Pawn mourner, Pawn dead)
        {
            if (mourner.needs?.mood?.thoughts == null) return;
            bool eased = false;
            foreach (Thought_Memory mem in mourner.needs.mood.thoughts.memories.Memories)
            {
                if (mem is not Thought_Trauma tt) continue;
                if (tt.traumaDef != DC_DefOf.DC_Trauma_ViolentLoss
                    && tt.traumaDef != DC_DefOf.DC_Trauma_BereavementShock)
                    continue;
                if (tt.otherPawn != null && tt.otherPawn != dead) continue;

                int duration = tt.DurationTicks;
                if (duration <= 0) continue;
                tt.age = System.Math.Min(duration, tt.age + EaseTicks);
                eased = true;
            }

            if (!eased) return;
            MoteMaker.ThrowText(mourner.DrawPos, mourner.Map,
                "DC_FuneralEaseMote".Translate(), 3f);
        }
    }
}
