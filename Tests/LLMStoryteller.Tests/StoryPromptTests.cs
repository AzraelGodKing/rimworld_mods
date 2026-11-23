using System.Collections.Generic;
using Xunit;

namespace LLMStoryteller.Tests
{
    public class StoryPromptTests
    {
        [Fact]
        public void User_prompt_lists_every_option_and_the_quiet_choice()
        {
            var menu = new List<MenuOption>
            {
                new MenuOption("RaidEnemy", "enemy raid", "ThreatBig"),
                new MenuOption("Eclipse", "eclipse", "Misc"),
            };
            var snap = new ColonySnapshot { Day = 14, Season = "Summer", Biome = "temperate forest", Colonists = 5, AverageMoodPercent = 62 };
            snap.RecentEvents.Add("Day 13: Trade caravan");
            string user = StoryPrompt.User(snap, menu, new List<string> { "Day 12: a quiet day - rest" });

            Assert.Contains("day 14", user);
            Assert.Contains("- RaidEnemy: enemy raid, ThreatBig", user);
            Assert.Contains("- Eclipse: eclipse, Misc", user);
            Assert.Contains("- none: a quiet day", user);
            Assert.Contains("Day 13: Trade caravan", user);
            Assert.Contains("Day 12: a quiet day - rest", user);
            Assert.Contains("Last major threat: none yet", user);
        }

        [Fact]
        public void Blank_personality_uses_the_default()
        {
            string system = StoryPrompt.System("the Oracle", "  ", 0.5f, 1.5f);
            Assert.Contains(StoryPrompt.DefaultPersonality, system);
            Assert.Contains("1.5 is the hardest allowed", system);
        }
    }
}
