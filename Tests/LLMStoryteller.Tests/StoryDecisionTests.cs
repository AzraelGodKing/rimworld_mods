using System.Collections.Generic;
using Xunit;

namespace LLMStoryteller.Tests
{
    public class StoryDecisionTests
    {
        static readonly List<MenuOption> Menu = new List<MenuOption>
        {
            new MenuOption("RaidEnemy", "enemy raid", "ThreatBig"),
            new MenuOption("ResourcePodCrash", "cargo pods", "Misc"),
        };

        static StoryDecision Parse(string content)
        {
            Assert.True(StoryDecision.TryParse(content, Menu, 0.5f, 1.5f, out StoryDecision d, out string error), error);
            return d;
        }

        [Fact]
        public void Plain_answer()
        {
            var d = Parse("{\"incident\":\"RaidEnemy\",\"intensity\":1.2,\"reason\":\"They grew fat.\"}");
            Assert.Equal("RaidEnemy", d.Option.DefName);
            Assert.Equal(1.2f, d.Intensity, 3);
            Assert.Equal("They grew fat.", d.Reason);
        }

        [Fact]
        public void Code_fence_and_chatter_around_the_object()
        {
            var d = Parse("Sure! Here is my pick:\n```json\n{\"incident\": \"ResourcePodCrash\", \"intensity\": 1, \"reason\": \"A gift.\"}\n```");
            Assert.Equal("ResourcePodCrash", d.Option.DefName);
        }

        [Fact]
        public void Think_block_is_skipped_even_with_braces_inside()
        {
            var d = Parse("<think>Maybe {\"incident\":\"nope\"}? No.</think>{\"incident\":\"RaidEnemy\",\"reason\":\"r\"}");
            Assert.Equal("RaidEnemy", d.Option.DefName);
        }

        [Fact]
        public void Matches_label_and_ignores_case()
        {
            Assert.Equal("ResourcePodCrash", Parse("{\"incident\":\"Cargo Pods\"}").Option.DefName);
            Assert.Equal("RaidEnemy", Parse("{\"incident\":\"raidenemy\"}").Option.DefName);
        }

        [Theory]
        [InlineData("none")]
        [InlineData("None")]
        [InlineData("quiet")]
        public void Quiet_day(string word)
        {
            Assert.True(Parse("{\"incident\":\"" + word + "\",\"reason\":\"Rest.\"}").Quiet);
        }

        [Theory]
        [InlineData("{\"incident\":\"RaidEnemy\",\"intensity\":9}", 1.5f)]
        [InlineData("{\"incident\":\"RaidEnemy\",\"intensity\":0.1}", 0.5f)]
        [InlineData("{\"incident\":\"RaidEnemy\",\"intensity\":\"1.25\"}", 1.25f)]
        [InlineData("{\"incident\":\"RaidEnemy\"}", 1f)]
        public void Intensity_is_clamped(string json, float expected)
        {
            Assert.Equal(expected, Parse(json).Intensity, 3);
        }

        [Theory]
        [InlineData("")]
        [InlineData("I think a raid would be fun.")]
        [InlineData("{\"reason\":\"no pick\"}")]
        [InlineData("{\"incident\":\"Meteorite\"}")]
        [InlineData("{\"incident\":\"RaidEnemy\"")]
        public void Bad_answers_fail(string content)
        {
            Assert.False(StoryDecision.TryParse(content, Menu, 0.5f, 1.5f, out StoryDecision d, out string error));
            Assert.Null(d);
            Assert.False(string.IsNullOrEmpty(error));
        }
    }
}
