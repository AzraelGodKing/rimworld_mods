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
                if (!SoftCompat.IsOdysseyNonSurface(map)) continue;
                int count = 0;
                Pawn lone = null;
                foreach (Pawn p in map.mapPawns.FreeColonistsSpawned)
                {
                    if (p.Dead) continue;
                    count++;
                    lone = p;
                }
                if (count != 1 || lone == null) continue;

                var comp = lone.TryGetComp<Comp_DeepColony>();
                if (comp == null) continue;
                if (comp.isolationSinceTick < 0)
                    comp.isolationSinceTick = Find.TickManager.TicksGame;
                else if (Find.TickManager.TicksGame - comp.isolationSinceTick >= IsolationDays * 60000)
                {
                    if (!TraumaUtility.HasTrauma(lone, DC_DefOf.DC_Trauma_Isolation))
                        TraumaUtility.ApplyTrauma(lone, DC_DefOf.DC_Trauma_Isolation);
                    comp.isolationSinceTick = Find.TickManager.TicksGame;
                }
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
