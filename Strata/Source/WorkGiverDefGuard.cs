using System;
using System.Collections.Generic;
using RimWorld;
using Verse;

namespace Strata
{
    /// <summary>
    /// WorkGiverDef.get_Worker() calls Activator.CreateInstance(giverClass) and throws
    /// ArgumentNullException when giverClass is null (XML named a type the loaded DLL
    /// does not contain). AZR-747: Workshop Strata 4.2.0 on 1.7 shipped a stale DLL, so
    /// Strata_DeepSurvey and vanilla/Hospitality job search threw every tick. Scrub those
    /// defs from work-type lists once, and never touch .Worker without a giverClass check.
    /// </summary>
    public static class WorkGiverDefGuard
    {
        private static readonly HashSet<string> Logged = new HashSet<string>();

        public static void ScrubMissingGiverClasses()
        {
            List<WorkGiverDef> defs = DefDatabase<WorkGiverDef>.AllDefsListForReading;
            for (int i = 0; i < defs.Count; i++)
            {
                WorkGiverDef def = defs[i];
                if (def == null || def.giverClass != null)
                {
                    continue;
                }

                LogMissingOnce(def);
                WorkTypeDef workType = def.workType;
                if (workType?.workGiversByPriority != null)
                {
                    workType.workGiversByPriority.Remove(def);
                }
            }
        }

        public static WorkGiver TryGetWorker(WorkGiverDef def)
        {
            if (def == null)
            {
                return null;
            }

            if (def.giverClass == null)
            {
                LogMissingOnce(def);
                return null;
            }

            try
            {
                return def.Worker;
            }
            catch (ArgumentNullException)
            {
                LogMissingOnce(def);
                return null;
            }
        }

        private static void LogMissingOnce(WorkGiverDef def)
        {
            if (def == null || !Logged.Add(def.defName))
            {
                return;
            }

            string pack = def.modContentPack?.Name ?? "unknown mod";
            Log.Warning("[Strata] WorkGiverDef '" + def.defName + "' from " + pack
                + " has no giverClass (type missing from the loaded DLL). Skipping it so job"
                + " search does not throw. If this is Strata_DeepSurvey on RimWorld 1.7,"
                + " update Strata past Workshop 4.2.0 — that upload shipped a stale 1.7 DLL.");
        }
    }
}
