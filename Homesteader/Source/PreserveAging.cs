using System.Collections.Generic;
using RimWorld;
using UnityEngine;
using Verse;

namespace Homesteader
{
    public class CompProperties_PreserveAging : CompProperties
    {
        /// <summary>Days in cool storage to reach aged / well-aged / vintage.</summary>
        public List<float> tierDays = new List<float> { 5f, 15f, 30f };

        /// <summary>Market value multiplier per tier, starting with the young (tier 0) value.</summary>
        public List<float> tierValueFactors = new List<float> { 1f, 1.1f, 1.25f, 1.5f };

        /// <summary>Cooling ceilings at or below this (icehouse) age at half speed.</summary>
        public float slowBelowCeiling = 0f;

        public CompProperties_PreserveAging()
        {
            compClass = typeof(CompPreserveAging);
        }
    }

    /// <summary>
    /// Ages while spawned in a passively cooled cell (root cellar, springhouse, icehouse).
    /// Stacks merge as a count-weighted average, like rot progress.
    /// </summary>
    public class CompPreserveAging : ThingComp
    {
        private int agedTicks;

        public CompProperties_PreserveAging Props => (CompProperties_PreserveAging)props;

        public float AgedDays => agedTicks / (float)GenDate.TicksPerDay;

        public int Tier
        {
            get
            {
                int tier = 0;
                float days = AgedDays;
                for (int i = 0; i < Props.tierDays.Count; i++)
                {
                    if (days >= Props.tierDays[i])
                    {
                        tier = i + 1;
                    }
                }

                return tier;
            }
        }

        public float ValueFactor
        {
            get
            {
                List<float> factors = Props.tierValueFactors;
                if (factors == null || factors.Count == 0)
                {
                    return 1f;
                }

                return factors[Mathf.Min(Tier, factors.Count - 1)];
            }
        }

        public override void CompTickRare()
        {
            base.CompTickRare();
            if (!parent.Spawned || !RootCellarUtility.TryGetCoolingCeiling(parent, out float ceiling))
            {
                return;
            }

            agedTicks += ceiling <= Props.slowBelowCeiling
                ? GenTicks.TickRareInterval / 2
                : GenTicks.TickRareInterval;
        }

        public override void PreAbsorbStack(Thing otherStack, int count)
        {
            base.PreAbsorbStack(otherStack, count);
            CompPreserveAging other = otherStack.TryGetComp<CompPreserveAging>();
            int total = parent.stackCount + count;
            if (other == null || total <= 0)
            {
                return;
            }

            agedTicks = Mathf.RoundToInt(
                (agedTicks * (float)parent.stackCount + other.agedTicks * (float)count) / total);
        }

        public override void PostSplitOff(Thing piece)
        {
            base.PostSplitOff(piece);
            CompPreserveAging other = piece.TryGetComp<CompPreserveAging>();
            if (other != null)
            {
                other.agedTicks = agedTicks;
            }
        }

        public override void PostExposeData()
        {
            base.PostExposeData();
            Scribe_Values.Look(ref agedTicks, "hsAgedTicks", 0);
        }

        public override string TransformLabel(string label)
        {
            int tier = Tier;
            return tier == 0 ? label : label + " (" + TierLabel(tier) + ")";
        }

        public override string CompInspectStringExtra()
        {
            int tier = Tier;
            if (tier == 0 && agedTicks <= 0)
            {
                return "Homesteader_AgingNotStarted".Translate();
            }

            return "Homesteader_AgingInspect".Translate(
                TierLabel(tier),
                AgedDays.ToString("F1"),
                ValueFactor.ToStringPercent());
        }

        internal static string TierLabel(int tier)
        {
            switch (tier)
            {
                case 1:
                    return "Homesteader_AgingTier1".Translate();
                case 2:
                    return "Homesteader_AgingTier2".Translate();
                case 3:
                    return "Homesteader_AgingTier3".Translate();
                default:
                    return "Homesteader_AgingTier0".Translate();
            }
        }
    }

    public class StatPart_PreserveAging : StatPart
    {
        public override void TransformValue(StatRequest req, ref float val)
        {
            CompPreserveAging comp = Comp(req);
            if (comp != null)
            {
                val *= comp.ValueFactor;
            }
        }

        public override string ExplanationPart(StatRequest req)
        {
            CompPreserveAging comp = Comp(req);
            if (comp == null || comp.Tier == 0)
            {
                return null;
            }

            return "Homesteader_AgingStatExplain".Translate(
                CompPreserveAging.TierLabel(comp.Tier),
                comp.ValueFactor.ToString("0.##"));
        }

        private static CompPreserveAging Comp(StatRequest req)
        {
            return req.HasThing ? req.Thing.TryGetComp<CompPreserveAging>() : null;
        }

        internal static void Register()
        {
            StatDef stat = StatDefOf.MarketValue;
            if (stat == null)
            {
                return;
            }

            if (stat.parts == null)
            {
                stat.parts = new List<StatPart>();
            }

            for (int i = 0; i < stat.parts.Count; i++)
            {
                if (stat.parts[i] is StatPart_PreserveAging)
                {
                    return;
                }
            }

            stat.parts.Add(new StatPart_PreserveAging { parentStat = stat });
        }
    }
}
