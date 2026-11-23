using System.Collections.Generic;
using System.Globalization;
using System.Text;

namespace LLMStoryteller
{
    /// <summary>Plain facts about one colony map, read on the main thread before a request.</summary>
    public sealed class ColonySnapshot
    {
        public int Day;
        public string Season = "";
        public string Biome = "";
        public float OutdoorTempC;
        public int Colonists;
        public int AverageMoodPercent;
        public int Downed;
        public int Prisoners;
        public float Wealth;
        public float ThreatPoints;
        public float FoodDays;
        public bool HostilesOnMap;
        /// <summary>Days since the last big threat on this map, or -1 if there has not been one.</summary>
        public float DaysSinceBigThreat = -1f;
        /// <summary>Recent letters, oldest first, already labelled with their day.</summary>
        public List<string> RecentEvents = new List<string>();
    }

    /// <summary>Builds the system and user messages. Pure text, tested headless.</summary>
    public static class StoryPrompt
    {
        public const string DefaultPersonality =
            "You are a dramatic but fair storyteller. You build tension, let the colony breathe after hard hits, " +
            "and like stories with twists. Aim for a major threat roughly every 5 to 10 days once the colony is settled.";

        public static string System(string storytellerName, string personality, float minIntensity, float maxIntensity)
        {
            var sb = new StringBuilder();
            sb.Append("You are ").Append(storytellerName).Append(", the AI storyteller of a RimWorld colony. ");
            sb.Append(string.IsNullOrWhiteSpace(personality) ? DefaultPersonality : personality.Trim());
            sb.Append("\n\nOnce a day you read a short report on the colony and choose what happens next, ");
            sb.Append("from the list of incidents that can happen right now, or nothing.\n");
            sb.Append("Answer with ONE JSON object and nothing else, in this shape:\n");
            sb.Append("{\"incident\": \"<defName from the list, or none>\", \"intensity\": <number>, \"reason\": \"<one or two sentences>\"}\n");
            sb.Append("intensity scales threat size: ").Append(F(minIntensity)).Append(" is mild, 1 is normal, ")
              .Append(F(maxIntensity)).Append(" is the hardest allowed. It is ignored for incidents that have no size.\n");
            sb.Append("reason is shown to the player, so write it in character as the storyteller.");
            return sb.ToString();
        }

        public static string User(ColonySnapshot s, IList<MenuOption> menu, IList<string> recentDecisions)
        {
            var sb = new StringBuilder();
            sb.Append("Colony report, day ").Append(s.Day).Append(" (").Append(s.Season).Append(", ").Append(s.Biome)
              .Append(", ").Append(s.OutdoorTempC.ToString("0", CultureInfo.InvariantCulture)).Append("C outside)\n");
            sb.Append("- Colonists: ").Append(s.Colonists).Append(", average mood ").Append(s.AverageMoodPercent).Append("%");
            if (s.Downed > 0) sb.Append(", ").Append(s.Downed).Append(" downed");
            if (s.Prisoners > 0) sb.Append(", ").Append(s.Prisoners).Append(" prisoners");
            sb.Append('\n');
            sb.Append("- Wealth: ").Append(s.Wealth.ToString("0", CultureInfo.InvariantCulture))
              .Append(", raid strength now: ").Append(s.ThreatPoints.ToString("0", CultureInfo.InvariantCulture)).Append(" points\n");
            sb.Append("- Food: about ").Append(s.FoodDays.ToString("0.#", CultureInfo.InvariantCulture)).Append(" days\n");
            sb.Append("- Enemies on the map right now: ").Append(s.HostilesOnMap ? "yes" : "no").Append('\n');
            sb.Append("- Last major threat: ")
              .Append(s.DaysSinceBigThreat < 0 ? "none yet" : s.DaysSinceBigThreat.ToString("0.#", CultureInfo.InvariantCulture) + " days ago")
              .Append('\n');

            if (s.RecentEvents.Count > 0)
            {
                sb.Append("\nRecent events:\n");
                foreach (string e in s.RecentEvents) sb.Append("- ").Append(e).Append('\n');
            }
            if (recentDecisions != null && recentDecisions.Count > 0)
            {
                sb.Append("\nYour recent choices:\n");
                foreach (string d in recentDecisions) sb.Append("- ").Append(d).Append('\n');
            }

            sb.Append("\nIncidents that can happen now (defName: label, category):\n");
            foreach (MenuOption o in menu)
                sb.Append("- ").Append(o.DefName).Append(": ").Append(o.Label).Append(", ").Append(o.Category).Append('\n');
            sb.Append("- ").Append(StoryDecision.QuietWord).Append(": a quiet day\n");
            sb.Append("\nChoose one. Reply with the JSON object only.");
            return sb.ToString();
        }

        static string F(float v) => v.ToString("0.##", CultureInfo.InvariantCulture);
    }
}
