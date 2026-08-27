using System.Collections.Generic;
using System.Text;
using RimWorld;
using Verse;

namespace DeepColony
{
    public static class PerkTipUtility
    {
        public static string TipFor(PerkDef perk)
        {
            if (perk == null) return string.Empty;

            var sb = new StringBuilder();
            if (!perk.description.NullOrEmpty())
                sb.AppendLine(perk.description);
            else
                sb.AppendLine(perk.LabelCap);

            string effects = FormatHediffEffects(perk.hediff);
            if (!effects.NullOrEmpty())
            {
                sb.AppendLine();
                sb.Append(effects);
            }

            return sb.ToString().TrimEnd();
        }

        public static string FormatHediffEffects(HediffDef hediff)
        {
            if (hediff?.stages == null || hediff.stages.Count == 0) return null;

            HediffStage stage = hediff.stages[hediff.stages.Count - 1];
            var lines = new List<string>();

            if (stage.statOffsets != null)
            {
                foreach (StatModifier mod in stage.statOffsets)
                {
                    if (mod?.stat == null) continue;
                    lines.Add(FormatOffset(mod.stat, mod.value));
                }
            }

            if (stage.statFactors != null)
            {
                foreach (StatModifier mod in stage.statFactors)
                {
                    if (mod?.stat == null) continue;
                    lines.Add(FormatFactor(mod.stat, mod.value));
                }
            }

            return lines.Count == 0 ? null : string.Join("\n", lines);
        }

        private static string FormatOffset(StatDef stat, float value)
        {
            string sign = value >= 0f ? "+" : "";
            // Hit/dodge chances are 0–1 fractions. ShootingAccuracyPawn is a rating (vanilla traits use +5).
            if (LooksLikeChanceOrOffsetPercent(stat))
                return $"{stat.LabelCap}: {sign}{(value * 100f):0.#}%";
            return $"{stat.LabelCap}: {sign}{value:0.##}";
        }

        private static string FormatFactor(StatDef stat, float value)
        {
            float pct = (value - 1f) * 100f;
            string sign = pct >= 0f ? "+" : "";
            return $"{stat.LabelCap}: {sign}{pct:0.#}%";
        }

        private static bool LooksLikeChanceOrOffsetPercent(StatDef stat)
        {
            string n = stat.defName ?? "";
            if (n.Contains("Accuracy")) return false;
            return n.Contains("Chance")
                || n.Contains("Hit")
                || n.Contains("Dodge")
                || n.Contains("Yield")
                || n.Contains("Improvement")
                || n.Contains("Impact")
                || n.Contains("Threshold")
                || n.Contains("Success");
        }
    }
}
