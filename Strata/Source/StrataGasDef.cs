using RimWorld;
using UnityEngine;
using Verse;

namespace Strata
{
    // One channel of the room-density atmosphere simulation. Smoke is a gas
    // like any other; new gases are pure data. Every movement mechanic (vents,
    // louvers, ducts, doors, shafts, seals, the ventilation guarantee) applies
    // to every gas automatically - only the flags below differ.
    public class StrataGasDef : Def
    {
        // Tint of the drawn overlay where this gas fills a room.
        public Color overlayColor = new Color(0.04f, 0.04f, 0.05f);

        // Minimum room density before this gas contributes to the overlay tint.
        public float overlayThreshold = 0.03f;

        // Short label for the gas overlay readout (e.g. O₂, CO₂).
        public string overlayLabel;

        // Rises through unsealed stairwell / elevator shafts (and updraft
        // filters boost it). Heavy gases pool on the level they leak into.
        public bool buoyant;

        // Fraction that seeps away per cycle in an enclosed room with no
        // outlet. Zero = persistent: a sealed pocket keeps its gas forever.
        public float passiveLeak;

        // Harm to fleshy pawns breathing it, if any. Most gases harm when
        // density rises above harmThreshold; oxygen harms when it falls below
        // (hypoxia) via harmWhenBelow.
        public HediffDef harmHediff;
        public float harmThreshold = 0.15f;
        public float severityGain = 0.006f;
        public float severityDecay = 0.03f;
        public bool harmWhenBelow;

        // Rooms above ignitionDensity explode when they contain an open
        // flame - torches become dangerous mining equipment.
        public bool flammable;
        public float ignitionDensity = 0.35f;

        // A gas well can pump it out of a deep vent.
        public bool extractable;

        // When density is above harmThreshold, each cycle removes this much
        // oxygen from the same room (black damp displacing breathable air).
        public float displacesOxygen;

        // Throw vanilla smoke puffs in thick clouds (smoke only; other gases
        // read through the overlay instead).
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
