using System;
using System.Reflection;
using HarmonyLib;
using RimWorld;
using Verse;
using Verse.AI;

namespace DateNight
{
    /// <summary>
    /// Fail-open Romance on the Rim hooks. No project reference — package id
    /// <c>telardo.RomanceOnTheRim</c> and JobDef / JobDriver names via reflection.
    /// Missing RotR is a no-op.
    /// </summary>
    public static class DateNightRotrSoftCompat
    {
        public const string PackageId = "telardo.RomanceOnTheRim";
        public const string NeedDefName = "RomanceOnTheRim_Need_Romance";

        private const float DefaultNiceGain = 0.40f;
        private const float DefaultWonderfulGain = 0.50f;
        private const float DefaultAwkwardGain = 0.20f;
        private const float AnniversaryBonus = 0.10f;

        private static bool bound;
        private static bool logged;
        private static bool active;
        private static MethodInfo affectNeedForPawn;
        private static float niceGain = DefaultNiceGain;
        private static float wonderfulGain = DefaultWonderfulGain;
        private static float awkwardGain = DefaultAwkwardGain;

        public static bool Active
        {
            get
            {
                Bind();
                return active;
            }
        }

        /// <summary>
        /// Yield Date Night dates when RotR is already running a kiss, stroll,
        /// proposal, or other romance job on either pawn.
        /// </summary>
        public static bool ShouldYieldDate(Pawn pawn, Pawn partner)
        {
            return IsBusyWithRomance(pawn) || IsBusyWithRomance(partner);
        }

        public static bool IsBusyWithRomance(Pawn pawn)
        {
            if (!Active || pawn?.CurJobDef == null)
            {
                return false;
            }

            string name = pawn.CurJobDef.defName;
            if (!string.IsNullOrEmpty(name)
                && (name.StartsWith("RomanceOnTheRim_", StringComparison.Ordinal)
                    || name.StartsWith("RotR_Job_", StringComparison.Ordinal)))
            {
                return true;
            }

            Type driverType = pawn.jobs?.curDriver?.GetType();
            while (driverType != null && driverType != typeof(JobDriver))
            {
                string ns = driverType.Namespace;
                if (ns == "RomanceOnTheRim"
                    || (ns != null && ns.StartsWith("RomanceOnTheRim.", StringComparison.Ordinal)))
                {
                    return true;
                }
                driverType = driverType.BaseType;
            }

            return false;
        }

        /// <summary>
        /// Fill RotR's romance need after a finished Date Night date. No-op if
        /// RotR is missing or the pawn has no romance need (usually not lovers yet).
        /// Amount matches RotR stroll / lovin / kiss-scale tuning when those fields bind.
        /// </summary>
        public static void NotifyDateFinished(Pawn pawn, ThoughtDef quality, bool anniversary = false)
        {
            Bind();
            if (!active || pawn == null)
            {
                return;
            }

            float amount = AmountFor(quality, anniversary);
            if (amount <= 0f)
            {
                return;
            }

            if (affectNeedForPawn != null)
            {
                try
                {
                    affectNeedForPawn.Invoke(null, new object[] { pawn, amount });
                    return;
                }
                catch
                {
                }
            }

            TryFillNeedDirect(pawn, amount);
        }

        private static float AmountFor(ThoughtDef quality, bool anniversary)
        {
            float amount = niceGain;
            if (quality == DateNightDefOf.DateNight_DateWonderful)
            {
                amount = wonderfulGain;
            }
            else if (quality == DateNightDefOf.DateNight_DateAwkward)
            {
                amount = awkwardGain;
            }
            if (anniversary)
            {
                amount += AnniversaryBonus;
            }
            return amount;
        }

        private static void TryFillNeedDirect(Pawn pawn, float amount)
        {
            if (pawn?.needs == null)
            {
                return;
            }

            NeedDef def = DefDatabase<NeedDef>.GetNamedSilentFail(NeedDefName);
            if (def == null)
            {
                return;
            }

            Need need = pawn.needs.TryGetNeed(def);
            if (need == null)
            {
                return;
            }

            need.CurLevel += amount;
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
                Type sample = AccessTools.TypeByName("RomanceOnTheRim.JobDriver_InitiateKiss")
                    ?? AccessTools.TypeByName("RomanceOnTheRim.JobDriver_StrollTogether");
                Type utility = AccessTools.TypeByName("RomanceOnTheRim.RomanceUtility");
                Type tuning = AccessTools.TypeByName("RomanceOnTheRim.Tuning");
                active = packed || sample != null || utility != null;
                if (!active)
                {
                    return;
                }

                if (utility != null)
                {
                    affectNeedForPawn = AccessTools.Method(utility, "TryAffectRomanceNeedLevelForPawn",
                        new[] { typeof(Pawn), typeof(float) });
                    if (affectNeedForPawn == null)
                    {
                        affectNeedForPawn = AccessTools.Method(utility, "TryAffectRomanceNeedLevel",
                            new[] { typeof(Pawn), typeof(float) });
                    }
                }

                niceGain = ReadTuning(tuning, "StrollTogetherRomanceNeedGain", DefaultNiceGain);
                wonderfulGain = ReadTuning(tuning, "RomanceGainFromLovin", DefaultWonderfulGain);
                awkwardGain = ReadTuning(tuning, "CommonKissRomanceNeedGain", DefaultAwkwardGain);

                if (!logged)
                {
                    logged = true;
                    Log.Message("[DateNight] Romance on the Rim soft-compat ready (yield on RotR jobs; dates fill romance need, fail-open).");
                }
            }
            catch (Exception e)
            {
                active = false;
                Log.Warning("[DateNight] Romance on the Rim soft-compat failed to bind: " + e.Message);
            }
        }

        private static float ReadTuning(Type tuning, string field, float fallback)
        {
            if (tuning == null)
            {
                return fallback;
            }

            try
            {
                FieldInfo info = AccessTools.Field(tuning, field);
                if (info == null || info.FieldType != typeof(float))
                {
                    return fallback;
                }
                object value = info.GetValue(null);
                if (value is float)
                {
                    return (float)value;
                }
            }
            catch
            {
            }

            return fallback;
        }
    }
}
