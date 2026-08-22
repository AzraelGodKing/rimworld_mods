using System;
using RimWorld;
using Verse;

namespace DeepColony
{
    /// <summary>C05 Anomaly horror + C06 Odyssey isolation/crash. DLC-gated defs.</summary>
    public static class AnomalyOdysseyTraumaUtility
    {
        private const int IsolationDays = 5;
        private const int IsolationCheck = 2500;

        public static void NotifyDowned(Pawn victim, DamageInfo info)
        {
            if (!DeepColonySettings.Get.enableTrauma) return;
            if (victim == null || !victim.IsColonistPlayerControlled) return;

            if (SoftCompat.IsAnomalyEntity(info.Instigator) && DC_DefOf.DC_Trauma_Horror != null)
            {
                TraumaUtility.ApplyTrauma(victim, DC_DefOf.DC_Trauma_Horror, info.Instigator as Pawn);
                return;
            }

            if (SoftCompat.OdysseyActive && IsCrashDamage(info) && DC_DefOf.DC_Trauma_Isolation != null)
                TraumaUtility.ApplyTrauma(victim, DC_DefOf.DC_Trauma_Isolation);
        }

        public static void NotifyIncident(IncidentDef def, IncidentParms parms)
        {
            if (!DeepColonySettings.Get.enableTrauma) return;
            if (!SoftCompat.OdysseyActive || def == null) return;
            if (DC_DefOf.DC_Trauma_Isolation == null) return;

            string n = def.defName ?? "";
            bool crash = n.IndexOf("Gravship", System.StringComparison.OrdinalIgnoreCase) >= 0
                && (n.IndexOf("Crash", System.StringComparison.OrdinalIgnoreCase) >= 0
                    || n.IndexOf("Wreck", System.StringComparison.OrdinalIgnoreCase) >= 0
                    || n.IndexOf("Destroyed", System.StringComparison.OrdinalIgnoreCase) >= 0);
            if (!crash && n.IndexOf("Crash", System.StringComparison.OrdinalIgnoreCase) < 0)
                return;

            Map map = parms?.target as Map;
            if (map == null) return;
            foreach (Pawn p in map.mapPawns.FreeColonistsSpawned)
            {
                if (p.Dead) continue;
                TraumaUtility.ApplyTrauma(p, DC_DefOf.DC_Trauma_Isolation);
            }
        }

        public static void GameTick()
        {
            if (!DeepColonySettings.Get.enableTrauma) return;
            if (!SoftCompat.OdysseyActive) return;
            if (DC_DefOf.DC_Trauma_Isolation == null) return;
            if (Find.TickManager.TicksGame % IsolationCheck != 0) return;

            foreach (Map map in Find.Maps)
            {
                if (SoftCompat.IsOdysseyNonSurface(map))
                {
                    TickIsolationOnMap(map);
                    continue;
                }
                EaseIsolationOnSurface(map);
            }
        }

        private static void TickIsolationOnMap(Map map)
        {
            int count = 0;
            Pawn lone = null;
            foreach (Pawn p in map.mapPawns.FreeColonistsSpawned)
            {
                if (p.Dead) continue;
                count++;
                lone = p;
            }
            if (count != 1 || lone == null) return;

            var comp = lone.TryGetComp<Comp_DeepColony>();
            if (comp == null) return;
            if (comp.isolationSinceTick < 0)
                comp.isolationSinceTick = Find.TickManager.TicksGame;
            else if (Find.TickManager.TicksGame - comp.isolationSinceTick >= IsolationDays * 60000)
            {
                if (!TraumaUtility.HasTrauma(lone, DC_DefOf.DC_Trauma_Isolation))
                    TraumaUtility.ApplyTrauma(lone, DC_DefOf.DC_Trauma_Isolation);
                comp.isolationSinceTick = Find.TickManager.TicksGame;
            }
        }

        /// <summary>D06 — returning to a surface map eases Odyssey isolation once.</summary>
        private static void EaseIsolationOnSurface(Map map)
        {
            if (DC_DefOf.DC_Trauma_Isolation == null) return;
            const int EaseTicks = 180000; // 3 days
            foreach (Pawn p in map.mapPawns.FreeColonistsSpawned)
            {
                if (p.Dead) continue;
                var comp = p.TryGetComp<Comp_DeepColony>();
                if (comp == null) continue;
                if (comp.isolationSinceTick < 0) continue;
                if (!TraumaUtility.HasTrauma(p, DC_DefOf.DC_Trauma_Isolation))
                {
                    comp.isolationSinceTick = -1;
                    continue;
                }

                foreach (Thought_Memory mem in p.needs.mood.thoughts.memories.Memories)
                {
                    if (mem is not Thought_Trauma tt) continue;
                    if (tt.traumaDef != DC_DefOf.DC_Trauma_Isolation) continue;
                    int duration = tt.DurationTicks;
                    if (duration <= 0) continue;
                    tt.age = System.Math.Min(duration, tt.age + EaseTicks);
                }
                comp.isolationSinceTick = -1;
                Messages.Message(
                    "DC_SurfaceReturnEase".Translate(p.LabelShort.Named("PAWN")),
                    p, MessageTypeDefOf.PositiveEvent, false);
            }
        }

        private static bool IsCrashDamage(DamageInfo info)
        {
            if (info.Def == null || info.Def.defName == null) return false;
            string n = info.Def.defName;
            return n.IndexOf("Crash", System.StringComparison.OrdinalIgnoreCase) >= 0
                || n.IndexOf("Wreck", System.StringComparison.OrdinalIgnoreCase) >= 0
                || n.IndexOf("Gravship", System.StringComparison.OrdinalIgnoreCase) >= 0;
        }
    }
}
