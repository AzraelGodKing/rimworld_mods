using System.Collections.Generic;
using RimWorld;
using Verse;

namespace Stormproof
{
    public class StormproofSettings : ModSettings
    {
        public bool enableBrownout = true;
        public float brownoutSeverity = 1f;
        public bool enableStormWear = true;
        public bool enableAlmanac = true;
        public bool enableFulgurite = true;

        public bool incidentIonStorm = true;
        public bool incidentHeatDome = true;
        public bool incidentPolarFront = true;
        public bool incidentToxicSurge = true;
        public bool incidentDryLightning = true;
        public float incidentFrequency = 1f;

        public bool allowAtmosphericBarrier = true;
        public bool allowClimateStabilizer = true;
        public bool allowSkyRestorer = true;
        public bool allowFireSuppressor = true;
        public bool allowDroughtCondenser = true;

        public float zzztChanceFactor = 1f;

        // Storm caller tuning (AZR-718); defaults match the building's 5 days / 12 hours.
        public float stormCallerCooldownDays = 5f;
        public float stormCallerHours = 12f;

        // A one-time nudge when a thunderstorm hits a grid with no spire or surge protector.
        public bool warnUndefendedGrid = true;

        // Off by default: the dampener's fantasy is buildings only.
        public bool dampenerCoversMechs = false;

        // Def baseChance as loaded from XML, before any setting scaled it.
        private static readonly Dictionary<IncidentDef, float> OriginalIncidentChance =
            new Dictionary<IncidentDef, float>();

        public override void ExposeData()
        {
            Scribe_Values.Look(ref enableBrownout, "enableBrownout", true);
            Scribe_Values.Look(ref brownoutSeverity, "brownoutSeverity", 1f);
            Scribe_Values.Look(ref enableStormWear, "enableStormWear", true);
            Scribe_Values.Look(ref enableAlmanac, "enableAlmanac", true);
            Scribe_Values.Look(ref enableFulgurite, "enableFulgurite", true);
            Scribe_Values.Look(ref incidentIonStorm, "incidentIonStorm", true);
            Scribe_Values.Look(ref incidentHeatDome, "incidentHeatDome", true);
            Scribe_Values.Look(ref incidentPolarFront, "incidentPolarFront", true);
            Scribe_Values.Look(ref incidentToxicSurge, "incidentToxicSurge", true);
            Scribe_Values.Look(ref incidentDryLightning, "incidentDryLightning", true);
            Scribe_Values.Look(ref incidentFrequency, "incidentFrequency", 1f);
            Scribe_Values.Look(ref allowAtmosphericBarrier, "allowAtmosphericBarrier", true);
            Scribe_Values.Look(ref allowClimateStabilizer, "allowClimateStabilizer", true);
            Scribe_Values.Look(ref allowSkyRestorer, "allowSkyRestorer", true);
            Scribe_Values.Look(ref allowFireSuppressor, "allowFireSuppressor", true);
            Scribe_Values.Look(ref allowDroughtCondenser, "allowDroughtCondenser", true);
            Scribe_Values.Look(ref zzztChanceFactor, "zzztChanceFactor", 1f);
            Scribe_Values.Look(ref stormCallerCooldownDays, "stormCallerCooldownDays", 5f);
            Scribe_Values.Look(ref stormCallerHours, "stormCallerHours", 12f);
            Scribe_Values.Look(ref warnUndefendedGrid, "warnUndefendedGrid", true);
            Scribe_Values.Look(ref dampenerCoversMechs, "dampenerCoversMechs", false);
            if (Scribe.mode == LoadSaveMode.PostLoadInit)
            {
                // Incident chances are applied by StormproofInit once defs exist.
                Clamp();
            }
        }

        public void ResetToDefaults()
        {
            enableBrownout = true;
            brownoutSeverity = 1f;
            enableStormWear = true;
            enableAlmanac = true;
            enableFulgurite = true;
            incidentIonStorm = true;
            incidentHeatDome = true;
            incidentPolarFront = true;
            incidentToxicSurge = true;
            incidentDryLightning = true;
            incidentFrequency = 1f;
            allowAtmosphericBarrier = true;
            allowClimateStabilizer = true;
            allowSkyRestorer = true;
            allowFireSuppressor = true;
            allowDroughtCondenser = true;
            zzztChanceFactor = 1f;
            stormCallerCooldownDays = 5f;
            stormCallerHours = 12f;
            warnUndefendedGrid = true;
            dampenerCoversMechs = false;
            ApplyIncidentChances();
        }

        public void ApplySoft()
        {
            ResetToDefaults();
            enableStormWear = false;
            brownoutSeverity = 0.6f;
            incidentFrequency = 0.6f;
            zzztChanceFactor = 0.5f;
            ApplyIncidentChances();
        }

        public void ApplyHard()
        {
            ResetToDefaults();
            brownoutSeverity = 1.35f;
            incidentFrequency = 1.5f;
            zzztChanceFactor = 1.5f;
            ApplyIncidentChances();
        }

        public void Clamp()
        {
            brownoutSeverity = brownoutSeverity < 0.25f ? 0.25f : (brownoutSeverity > 2f ? 2f : brownoutSeverity);
            incidentFrequency = incidentFrequency < 0f ? 0f : (incidentFrequency > 2.5f ? 2.5f : incidentFrequency);
            zzztChanceFactor = zzztChanceFactor < 0f ? 0f : (zzztChanceFactor > 3f ? 3f : zzztChanceFactor);
            stormCallerCooldownDays = stormCallerCooldownDays < 1f ? 1f : (stormCallerCooldownDays > 15f ? 15f : stormCallerCooldownDays);
            stormCallerHours = stormCallerHours < 2f ? 2f : (stormCallerHours > 24f ? 24f : stormCallerHours);
        }

        // Cheap dirty check so the settings window only Write()s when values change.
        public int ContentFingerprint()
        {
            unchecked
            {
                int h = enableBrownout ? 1 : 0;
                h = (h * 397) ^ brownoutSeverity.GetHashCode();
                h = (h * 397) ^ (enableStormWear ? 1 : 0);
                h = (h * 397) ^ (enableAlmanac ? 1 : 0);
                h = (h * 397) ^ (enableFulgurite ? 1 : 0);
                h = (h * 397) ^ (incidentIonStorm ? 1 : 0);
                h = (h * 397) ^ (incidentHeatDome ? 1 : 0);
                h = (h * 397) ^ (incidentPolarFront ? 1 : 0);
                h = (h * 397) ^ (incidentToxicSurge ? 1 : 0);
                h = (h * 397) ^ (incidentDryLightning ? 1 : 0);
                h = (h * 397) ^ incidentFrequency.GetHashCode();
                h = (h * 397) ^ (allowAtmosphericBarrier ? 1 : 0);
                h = (h * 397) ^ (allowClimateStabilizer ? 1 : 0);
                h = (h * 397) ^ (allowSkyRestorer ? 1 : 0);
                h = (h * 397) ^ (allowFireSuppressor ? 1 : 0);
                h = (h * 397) ^ (allowDroughtCondenser ? 1 : 0);
                h = (h * 397) ^ zzztChanceFactor.GetHashCode();
                h = (h * 397) ^ stormCallerCooldownDays.GetHashCode();
                h = (h * 397) ^ stormCallerHours.GetHashCode();
                h = (h * 397) ^ (warnUndefendedGrid ? 1 : 0);
                h = (h * 397) ^ (dampenerCoversMechs ? 1 : 0);
                return h;
            }
        }

        public int StormCallerCooldownTicks => (int)(stormCallerCooldownDays * GenDate.TicksPerDay);

        public int StormCallerDurationTicks => (int)(stormCallerHours * GenDate.TicksPerHour);

        public static void CaptureOriginalChances()
        {
            foreach (IncidentDef def in StormproofIncidentDefOf.All)
            {
                if (def != null && !OriginalIncidentChance.ContainsKey(def))
                {
                    OriginalIncidentChance[def] = def.baseChance;
                }
            }
        }

        public void ApplyIncidentChances()
        {
            Clamp();
            // Settings load in the mod constructor, before defs exist; StormproofInit
            // applies them again once the DefOfs are bound.
            ApplyOne(StormproofIncidentDefOf.Stormproof_IonStorm, incidentIonStorm);
            ApplyOne(StormproofIncidentDefOf.Stormproof_HeatDome, incidentHeatDome);
            ApplyOne(StormproofIncidentDefOf.Stormproof_PolarFront, incidentPolarFront);
            ApplyOne(StormproofIncidentDefOf.Stormproof_ToxicSurge, incidentToxicSurge);
            ApplyOne(StormproofIncidentDefOf.Stormproof_DryLightning, incidentDryLightning);
        }

        private void ApplyOne(IncidentDef def, bool enabled)
        {
            if (def == null)
            {
                return;
            }
            if (!OriginalIncidentChance.TryGetValue(def, out float original))
            {
                original = def.baseChance;
                OriginalIncidentChance[def] = original;
            }
            def.baseChance = enabled ? original * incidentFrequency : 0f;
        }
    }
}
