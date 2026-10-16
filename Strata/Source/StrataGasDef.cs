using RimWorld;
using UnityEngine;
using Verse;

namespace Strata
{
    public class StrataGasDef : Def
    {
        public Color overlayColor = new Color(0.04f, 0.04f, 0.05f);

        public float overlayThreshold = 0.03f;

        public string overlayLabel;

        public bool buoyant;

        public float passiveLeak;

        public HediffDef harmHediff;
        public float harmThreshold = 0.15f;
        public float severityGain = 0.006f;
        public float severityDecay = 0.03f;
        public bool harmWhenBelow;

        public bool flammable;
        public float ignitionDensity = 0.35f;

        public bool extractable;

        public float displacesOxygen;

        public bool throwsMotes;
        public float moteThreshold = 0.2f;
    }

    [DefOf]
    public static class StrataGasDefOf
    {
        public static StrataGasDef Strata_Smoke;

        public static StrataGasDef Strata_DeepGas;

        public static StrataGasDef Strata_Nitrogen;

        public static StrataGasDef Strata_Oxygen;

        public static StrataGasDef Strata_Argon;

        public static StrataGasDef Strata_CarbonDioxide;

        public static StrataGasDef Strata_Methane;

        public static StrataGasDef Strata_BlackDamp;

        public static StrataGasDef Strata_Spores;

        public static StrataGasDef Strata_Steam;

        static StrataGasDefOf()
        {
            DefOfHelper.EnsureInitializedInCtor(typeof(StrataGasDefOf));
        }
    }
}
