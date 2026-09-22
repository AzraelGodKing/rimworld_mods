using RimWorld;
using Verse;

namespace LivingWorld
{
    public static class LivingWorldLetters
    {
        public static void TrySend(WorldEvent ev)
        {
            if (ev == null || !LivingWorldHearRules.PlayerShouldHear(ev))
            {
                return;
            }

            GameComponent_LivingWorld comp = GameComponent_LivingWorld.Get;
            if (comp != null && !comp.TryConsumeLetterBudget())
            {
                return;
            }

            string label = LabelFor(ev.kind);
            string text = PrefixFor(ev) + "\n\n" + TextFor(ev);
            LetterDef letterDef = LetterDefFor(ev.severity);
            Find.LetterStack.ReceiveLetter(label, text, letterDef);
            ev.seenByPlayer = true;
        }

        private static LetterDef LetterDefFor(NewsSeverity severity)
        {
            switch (severity)
            {
                case NewsSeverity.Major:
                    return LetterDefOf.NegativeEvent;
                case NewsSeverity.Minor:
                    return LetterDefOf.NeutralEvent;
                default:
                    return LetterDefOf.PositiveEvent;
            }
        }

        public static string LabelFor(WorldEventKind kind)
        {
            return ("LivingWorld_LetterLabel_" + kind).Translate();
        }

        public static string TextFor(WorldEvent ev)
        {
            string a = string.IsNullOrEmpty(ev.factionAName)
                ? "LivingWorld_UnknownFaction".Translate().ToString()
                : ev.factionAName;
            string b = string.IsNullOrEmpty(ev.factionBName)
                ? "LivingWorld_UnknownFaction".Translate().ToString()
                : ev.factionBName;
            string place = string.IsNullOrEmpty(ev.settlementLabel)
                ? "LivingWorld_UnknownPlace".Translate().ToString()
                : ev.settlementLabel;

            return ("LivingWorld_LetterText_" + ev.kind).Translate(a, b, place);
        }

        public static string PrefixFor(WorldEvent ev)
        {
            if (ev == null)
            {
                return string.Empty;
            }
            if (ev.distorted && !ev.corrected)
            {
                return "LivingWorld_RumourPrefix".Translate();
            }
            switch (ev.channel)
            {
                case HearChannel.Radio:
                    return "LivingWorld_Channel_Radio".Translate();
                case HearChannel.Proximity:
                    return "LivingWorld_Channel_Proximity".Translate();
                default:
                    return "LivingWorld_Channel_Rumour".Translate();
            }
        }
    }
}
