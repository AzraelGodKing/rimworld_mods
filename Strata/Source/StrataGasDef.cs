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

        // Rises through unsealed stairwell / elevator shafts (and updraft
        // filters boost it). Heavy gases pool on the level they leak into.
        public bool buoyant;

        // Fraction that seeps away per cycle in an enclosed room with no
        // outlet. Zero = persistent: a sealed pocket keeps its gas forever.
        public float passiveLeak;

        // Harm to fleshy pawns breathing it, if any.
        public HediffDef harmHediff;
        public float harmThreshold = 0.15f;
        public float severityGain = 0.006f;
        public float severityDecay = 0.03f;

        // Rooms above ignitionDensity explode when they contain an open
        // flame - torches become dangerous mining equipment.
        public bool flammable;
        public float ignitionDensity = 0.35f;

        // A gas well can pump it out of a deep vent.
        public bool extractable;

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

        static StrataGasDefOf()
        {
            DefOfHelper.EnsureInitializedInCtor(typeof(StrataGasDefOf));
        }
    }
}
