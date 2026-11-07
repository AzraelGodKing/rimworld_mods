using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Text.RegularExpressions;
using System.Xml.Linq;
using System.Xml.XPath;
using Xunit;

namespace Homesteader.Tests
{
    public class AllergyTokenTests
    {
        [Theory]
        [InlineData("EggChickenUnfertilized", "Egg", true)]
        [InlineData("Homesteader_ScrambledEggs", "Egg", true)]
        [InlineData("RawVeggies", "Egg", false)]
        [InlineData("VeggieSoup", "Egg", false)]
        [InlineData("Eggplant", "Egg", false)]
        [InlineData("RawEggplant", "Egg", false)]
        [InlineData("Donuts", "Nuts", false)]
        [InlineData("MixedNuts", "Nuts", true)]
        [InlineData("Crabapple", "Crab", false)]
        [InlineData("CrabMeat", "Crab", true)]
        [InlineData("Homesteader_Butter", "Butter", true)]
        [InlineData("Homesteader_Buttermilk", "Butter", false)]
        [InlineData("Walnuts", "Walnut", true)]
        [InlineData("", "Egg", false)]
        public void TokenMatchesOnWordBoundaries(string defName, string token, bool expected)
        {
            Assert.Equal(expected, HomesteaderPure.DefNameContainsToken(defName, token));
        }

        [Fact]
        public void NullsNeverMatch()
        {
            Assert.False(HomesteaderPure.DefNameContainsToken(null, "Egg"));
            Assert.False(HomesteaderPure.DefNameContainsToken("Egg", null));
            Assert.False(HomesteaderPure.NameMatchesAllergy("Eggs", "Egg", null));
        }

        [Theory]
        [InlineData("RawPeanuts")]
        [InlineData("PeanutButter")]
        [InlineData("Peanut")]
        public void TreeNutsLeavesPeanutsAlone(string defName)
        {
            Assert.False(HomesteaderPure.NameMatchesAllergy("TreeNuts", defName, AllergyTokens.TreeNuts));
            Assert.True(HomesteaderPure.NameMatchesAllergy("Peanuts", defName, AllergyTokens.Peanuts));
        }

        [Theory]
        [InlineData("Eggs", "Homesteader_DeviledEggs", true)]
        [InlineData("Eggs", "RawVeggies", false)]
        [InlineData("TreeNuts", "Almonds", true)]
        [InlineData("TreeNuts", "Donut", false)]
        [InlineData("Wheat", "Homesteader_Bread", true)]
        [InlineData("Shellfish", "Crabapple", false)]
        [InlineData("Fish", "Homesteader_SmokedFish", true)]
        [InlineData("Milk", "Homesteader_Cheese", true)]
        [InlineData("Soy", "Tofu", true)]
        [InlineData("Sesame", "Tahini", true)]
        public void CatalogTokens(string allergyId, string defName, bool expected)
        {
            string[] tokens = (string[])typeof(AllergyTokens).GetField(allergyId,
                System.Reflection.BindingFlags.NonPublic | System.Reflection.BindingFlags.Static)!.GetValue(null)!;
            Assert.Equal(expected, HomesteaderPure.NameMatchesAllergy(allergyId, defName, tokens));
        }
    }

    public class PackTests
    {
        [Fact]
        public void RoundTrips()
        {
            var list = new List<string> { "Homesteader_Cheese", "Milk", "Homesteader_SmokedFish" };
            Assert.Equal(list, HomesteaderPure.Unpack(HomesteaderPure.Pack(list)));
        }

        [Fact]
        public void EmptyAndNull()
        {
            Assert.Equal(string.Empty, HomesteaderPure.Pack(null));
            Assert.Equal(string.Empty, HomesteaderPure.Pack(new List<string>()));
            Assert.Empty(HomesteaderPure.Unpack(null));
            Assert.Empty(HomesteaderPure.Unpack(""));
        }

        [Fact]
        public void SkipsEmptyParts()
        {
            Assert.Equal(new List<string> { "A", "B" }, HomesteaderPure.Unpack("|A||B|"));
        }
    }

    public class AgingTests
    {
        static readonly List<float> Days = new List<float> { 10f, 30f, 60f };
        static readonly List<float> Factors = new List<float> { 1f, 1.15f, 1.35f, 1.6f };

        [Theory]
        [InlineData(0f, 0)]
        [InlineData(9.99f, 0)]
        [InlineData(10f, 1)]
        [InlineData(29f, 1)]
        [InlineData(30f, 2)]
        [InlineData(60f, 3)]
        [InlineData(500f, 3)]
        public void TierFromDays(float days, int tier)
        {
            Assert.Equal(tier, HomesteaderPure.AgingTier(days, Days));
        }

        [Fact]
        public void NoThresholdsIsTierZero()
        {
            Assert.Equal(0, HomesteaderPure.AgingTier(100f, null));
            Assert.Equal(0, HomesteaderPure.AgingTier(100f, new List<float>()));
        }

        [Fact]
        public void ValueFactorClampsToLast()
        {
            Assert.Equal(1f, HomesteaderPure.AgingValueFactor(0, Factors));
            Assert.Equal(1.6f, HomesteaderPure.AgingValueFactor(3, Factors));
            Assert.Equal(1.15f, HomesteaderPure.AgingValueFactor(9, new List<float> { 1f, 1.15f }));
            Assert.Equal(1f, HomesteaderPure.AgingValueFactor(2, null));
        }
    }

    public class SeasonAndStageTests
    {
        [Theory]
        [InlineData("Spring", 5f, 1.45f)]
        [InlineData("Spring", 15f, 1.15f)]
        [InlineData("Winter", 0f, 0.85f)]
        [InlineData("Winter", 8f, 0.55f)]
        [InlineData("Fall", 10f, 0.7f)]
        [InlineData("Summer", 18f, 0.28f)]
        [InlineData("Summer", 30f, 0.12f)]
        public void MapleYield(string season, float temp, float expected)
        {
            Assert.Equal(expected, HomesteaderPure.MapleYieldFactor(Enum.Parse<SapSeason>(season), temp));
        }

        [Theory]
        [InlineData(0, -1)]
        [InlineData(2, -1)]
        [InlineData(3, 0)]
        [InlineData(6, 1)]
        [InlineData(8, 1)]
        [InlineData(9, 2)]
        [InlineData(40, 2)]
        public void LarderStage(int kinds, int stage)
        {
            Assert.Equal(stage, HomesteaderPure.LarderStage(kinds));
        }

        [Theory]
        [InlineData(0, false, 0)]
        [InlineData(0, true, 1)]
        [InlineData(3, false, 1)]
        [InlineData(3, true, 2)]
        [InlineData(6, false, 2)]
        [InlineData(6, true, 2)]
        public void FestivalStage(int kinds, bool seasonal, int stage)
        {
            Assert.Equal(stage, HomesteaderPure.FestivalStage(kinds, seasonal));
        }
    }

    /// <summary>Every patch that names a Homesteader def finds it in Homesteader's own defs.</summary>
    public class PatchTargetTests
    {
        static readonly Regex OwnDefSelector = new Regex(
            @"^/Defs/(\w+)\[defName=""((?:Homesteader|Wellspring)_\w+)""\]", RegexOptions.Compiled);

        [Fact]
        public void PatchesFindOwnDefs()
        {
            string mod = ModRoot();
            var defs = new XDocument(new XElement("Defs",
                Directory.GetFiles(mod, "*.xml", SearchOption.AllDirectories)
                    .Where(f => f.Replace('\\', '/').Contains("/Defs/"))
                    .Select(f => XDocument.Load(f).Root!)
                    .Where(r => r.Name.LocalName == "Defs")
                    .SelectMany(r => r.Elements())));

            var missing = new List<string>();
            int checkedCount = 0;
            foreach (string file in Directory.GetFiles(mod, "*.xml", SearchOption.AllDirectories)
                         .Where(f => f.Replace('\\', '/').Contains("/Patches/")))
            {
                foreach (XElement xpath in XDocument.Load(file).Descendants("xpath"))
                {
                    Match m = OwnDefSelector.Match(xpath.Value.Trim());
                    if (!m.Success)
                    {
                        continue;
                    }

                    checkedCount++;
                    if (!defs.XPathSelectElements(m.Value).Any())
                    {
                        missing.Add(Path.GetFileName(file) + ": " + m.Value);
                    }
                }
            }

            Assert.True(checkedCount > 0, "no patch xpaths found");
            Assert.True(missing.Count == 0, string.Join("\n", missing));
        }

        static string ModRoot()
        {
            string? dir = AppContext.BaseDirectory;
            for (int i = 0; i < 10 && dir != null; i++)
            {
                string candidate = Path.Combine(dir, "Homesteader", "About", "About.xml");
                if (File.Exists(candidate))
                {
                    return Path.Combine(dir, "Homesteader");
                }

                dir = Directory.GetParent(dir)?.FullName;
            }

            throw new DirectoryNotFoundException("Homesteader mod folder not found above " + AppContext.BaseDirectory);
        }
    }
}
