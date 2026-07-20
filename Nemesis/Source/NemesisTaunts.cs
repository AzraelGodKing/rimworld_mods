using RimWorld;
using Verse;

namespace Nemesis
{
    /// <summary>Personal taunt / letter copy. Names the nemesis and optional fixation target.</summary>
    public static class NemesisTaunts
    {
        public static string TargetPhrase(NemesisData data)
        {
            if (data.targetMode == NemesisTargetMode.Pawn && !string.IsNullOrEmpty(data.targetPawnName))
                return data.targetPawnName;
            return "Nemesis_Phrase_YourColony".Translate();
        }

        public static string PickCommsLine(NemesisData data)
        {
            string target = TargetPhrase(data);
            string name = data.nemesisName ?? "Nemesis_Phrase_Someone".Translate();
            int escapes = data.escapeCount;
            float agg = data.EffectiveAggression;

            if (agg < 3f)
            {
                return Rand.RangeInclusive(0, 5) switch
                {
                    0 => "Nemesis_Taunt_Low0".Translate(name, target),
                    1 => "Nemesis_Taunt_Low1".Translate(name, target),
                    2 => "Nemesis_Taunt_Low2".Translate(name, target),
                    3 => "Nemesis_Taunt_Low3".Translate(name, target),
                    4 => "Nemesis_Taunt_Low4".Translate(name, target),
                    _ => SoftHomesteaderTaunt(data, name, target)
                        ?? "Nemesis_Taunt_Low5".Translate(name, target),
                };
            }

            return Rand.RangeInclusive(0, 5) switch
            {
                0 => "Nemesis_Taunt_High0".Translate(name, target),
                1 => "Nemesis_Taunt_High1".Translate(name, target),
                2 => "Nemesis_Taunt_High2".Translate(name, target),
                3 => "Nemesis_Taunt_High3".Translate(name, target, escapes),
                4 => "Nemesis_Taunt_High4".Translate(name, target),
                _ => SoftStormFlavor(data, name, target)
                    ?? "Nemesis_Taunt_High5".Translate(name, target),
            };
        }

        private static string SoftHomesteaderTaunt(NemesisData data, string name, string target)
        {
            if (!SoftCompat.HomesteaderActive) return null;
            Pawn victim = GameComponent_Nemesis.Instance?.FindTargetPawn();
            string fav = SoftCompat.TryFavoriteFoodLabel(victim);
            if (fav != null)
                return "Nemesis_Taunt_FavoriteFood".Translate(name, target, fav);
            if (data != null)
                return "Nemesis_Taunt_Cellar".Translate(name);
            return null;
        }

        private static string SoftStormFlavor(NemesisData data, string name, string target)
        {
            if (!SoftCompat.StormproofActive) return null;
            return "Nemesis_Taunt_Ion".Translate(name, target);
        }

        public static string EscapeLetterBody(NemesisData data)
        {
            int n = data.escapeCount;
            string name = data.nemesisName;
            if (n == 1) return "Nemesis_Letter_Escape1".Translate(name);
            if (n == 2) return "Nemesis_Letter_Escape2".Translate(name);
            if (n <= 4) return "Nemesis_Letter_EscapeN".Translate(name, n);
            return "Nemesis_Letter_EscapeMany".Translate(name, n);
        }
    }
}
