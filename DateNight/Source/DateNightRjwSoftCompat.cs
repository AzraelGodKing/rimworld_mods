#nullable enable
using System;
using System.Reflection;
using HarmonyLib;
using RimWorld;
using Verse;
using Verse.AI;

namespace DateNight
{
    /// <summary>
    /// Fail-open RimJobWorld hooks. No project reference — package id, JobDefs,
    /// and <c>rjw.xxx</c> / <c>rjw.RJWSettings</c> via reflection. Missing RJW is a no-op.
    /// </summary>
    public static class DateNightRjwSoftCompat
    {
        public const string PackageId = "rim.job.world";

        private const string JoinInBedDefName = "JoinInBed";
        private const string MasturbateDefName = "RJW_Masturbate";

        private static bool bound;
        private static bool logged;
        private static bool active;
        // Bound once to delegates: no per-call object[] or boxing (AZR-557).
        // Not [UnsafeAccessorType]: that throws when RJW is not installed.
        private static Func<Pawn, bool>? canFuck;
        private static Func<Pawn, bool>? canBeFucked;
        private static Func<Pawn, bool>? canMasturbate;
        private static AccessTools.FieldRef<bool>? overrideLovin;
        private static JobDef? joinInBedDef;
        private static JobDef? masturbateDef;

        public static bool Active
        {
            get
            {
                Bind();
                return active;
            }
        }

        public static JobDef? JoinInBedDef
        {
            get
            {
                Bind();
                return joinInBedDef;
            }
        }

        public static JobDef? MasturbateDef
        {
            get
            {
                Bind();
                return masturbateDef;
            }
        }

        /// <summary>
        /// RJW default is <c>override_lovin = true</c>. Missing field still starts
        /// JoinInBed when the def exists so we do not fight the Harmony prefix.
        /// </summary>
        public static bool OverrideVanillaLovin
        {
            get
            {
                Bind();
                if (!active)
                {
                    return false;
                }
                if (overrideLovin == null)
                {
                    return JoinInBedDef != null;
                }
                return overrideLovin();
            }
        }

        public static JobDef ScheduledCoupleJobDef
        {
            get
            {
                if (OverrideVanillaLovin)
                {
                    JobDef? join = JoinInBedDef;
                    if (join != null)
                    {
                        return join;
                    }
                }
                return JobDefOf.Lovin;
            }
        }

        public static bool IsScheduledCoupleJob(JobDef def)
        {
            if (def == null)
            {
                return false;
            }
            if (def == JobDefOf.Lovin)
            {
                return true;
            }
            JobDef? join = JoinInBedDef;
            return join != null && def == join;
        }

        public static bool IsRjwSexJob(Pawn pawn)
        {
            if (!Active || pawn?.CurJobDef == null)
            {
                return false;
            }

            JobDef def = pawn.CurJobDef;
            if (def == JoinInBedDef || def == MasturbateDef)
            {
                return true;
            }

            string name = def.defName;
            if (name == "GettinLoved" || name == "GettinLicked" || name == "GettinSucked"
                || name == "Quickie" || name == "GettingQuickie")
            {
                return true;
            }

            Type? driverType = pawn.jobs?.curDriver?.GetType();
            while (driverType != null && driverType != typeof(JobDriver))
            {
                string typeName = driverType.Name;
                if (!string.IsNullOrEmpty(typeName)
                    && (typeName.IndexOf("Sex", StringComparison.OrdinalIgnoreCase) >= 0
                        || typeName.IndexOf("Masturbate", StringComparison.OrdinalIgnoreCase) >= 0
                        || typeName.IndexOf("JoinInBed", StringComparison.OrdinalIgnoreCase) >= 0))
                {
                    string? ns = driverType.Namespace;
                    if (ns == "rjw" || (ns != null && ns.StartsWith("rjw.", StringComparison.Ordinal)))
                    {
                        return true;
                    }
                }
                driverType = driverType.BaseType;
            }
            return false;
        }

        /// <summary>Fail-open: unknown RJW still allows the couple job.</summary>
        public static bool CoupleCanDoCasualSex(Pawn pawn, Pawn partner)
        {
            if (!Active)
            {
                return true;
            }

            return CanFuckOrBeFucked(pawn) && CanFuckOrBeFucked(partner);
        }

        public static bool TryStartJoinInBed(Pawn pawn, Pawn partner, Building_Bed bed)
        {
            if (pawn?.jobs == null || partner == null || bed == null)
            {
                return false;
            }

            JobDef? def = JoinInBedDef;
            if (def == null)
            {
                return false;
            }

            Job job = JobMaker.MakeJob(def, partner, bed);
            job.ignoreForbidden = true;
            pawn.jobs.StartJob(job, JobCondition.InterruptForced, null, resumeCurJobAfterwards: false);
            return pawn.CurJobDef == def;
        }

        public static bool TryStartMasturbate(Pawn pawn, Building_Bed? bed)
        {
            if (!Active || pawn?.jobs == null || bed == null)
            {
                return false;
            }

            JobDef? def = MasturbateDef;
            if (def == null)
            {
                return false;
            }
            if (!CanMasturbate(pawn))
            {
                return false;
            }

            Job job = JobMaker.MakeJob(def, pawn, bed, bed.Position);
            job.ignoreForbidden = true;
            pawn.jobs.StartJob(job, JobCondition.InterruptForced, null, resumeCurJobAfterwards: false);
            return pawn.CurJobDef == def;
        }

        public static void NotifyMasturbateCleanup(JobDriver driver, JobCondition condition)
        {
            if (!Active || driver?.pawn == null || condition != JobCondition.Succeeded)
            {
                return;
            }
            if (driver.job?.def != MasturbateDef)
            {
                return;
            }
            if (!DateNightUtility.IsLovinSchedule(driver.pawn))
            {
                return;
            }

            DateNightUtility.NotifySelfLovinFinished(driver.pawn);
        }

        private static bool CanFuckOrBeFucked(Pawn pawn)
        {
            if (pawn == null)
            {
                return false;
            }
            return InvokeBool(canFuck, pawn) || InvokeBool(canBeFucked, pawn);
        }

        private static bool CanMasturbate(Pawn pawn)
        {
            if (pawn == null)
            {
                return false;
            }
            return InvokeBool(canMasturbate, pawn);
        }

        private static bool InvokeBool(Func<Pawn, bool>? method, Pawn pawn)
        {
            if (method == null)
            {
                return true;
            }
            try
            {
                return method(pawn);
            }
            catch
            {
                return true;
            }
        }

        private static Func<Pawn, bool>? BindPredicate(Type type, string name)
        {
            MethodInfo method = AccessTools.Method(type, name, new[] { typeof(Pawn) });
            if (method == null || !method.IsStatic || method.ReturnType != typeof(bool))
            {
                return null;
            }
            return AccessTools.MethodDelegate<Func<Pawn, bool>>(method);
        }

        private static void Bind()
        {
            if (bound)
            {
                return;
            }

            bound = true;
            try
            {
                bool packed = ModLister.GetActiveModWithIdentifier(PackageId, ignorePostfix: true) != null;
                Type xxx = AccessTools.TypeByName("rjw.xxx");
                Type settings = AccessTools.TypeByName("rjw.RJWSettings");
                active = packed || xxx != null;
                if (!active)
                {
                    return;
                }

                joinInBedDef = DefDatabase<JobDef>.GetNamedSilentFail(JoinInBedDefName);
                masturbateDef = DefDatabase<JobDef>.GetNamedSilentFail(MasturbateDefName);

                if (xxx != null)
                {
                    canFuck = BindPredicate(xxx, "can_fuck");
                    canBeFucked = BindPredicate(xxx, "can_be_fucked");
                    canMasturbate = BindPredicate(xxx, "can_masturbate");
                }

                if (settings != null)
                {
                    FieldInfo field = AccessTools.Field(settings, "override_lovin");
                    if (field != null && field.IsStatic && field.FieldType == typeof(bool))
                    {
                        overrideLovin = AccessTools.StaticFieldRefAccess<bool>(field);
                    }
                }

                if (!logged)
                {
                    logged = true;
                    Log.Message("[DateNight] RimJobWorld soft-compat ready (JoinInBed / RJW_Masturbate, fail-open).");
                }
            }
            catch (Exception e)
            {
                active = false;
                Log.Warning("[DateNight] RimJobWorld soft-compat failed to bind: " + e.Message);
            }
        }
    }

    [HarmonyPatch(typeof(JobDriver), nameof(JobDriver.Cleanup))]
    public static class Patch_JobDriver_Cleanup_RjwPrivateTime
    {
        // Every job end for every pawn runs through Cleanup; skip it without RJW (AZR-542).
        public static bool Prepare()
        {
            return DateNightRjwSoftCompat.Active;
        }

        public static void Postfix(JobDriver __instance, JobCondition condition)
        {
            DateNightRjwSoftCompat.NotifyMasturbateCleanup(__instance, condition);
        }
    }
}
