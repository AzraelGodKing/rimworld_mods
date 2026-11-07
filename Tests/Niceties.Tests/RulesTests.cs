using Xunit;

namespace Niceties.Tests
{
    public class WearTests
    {
        [Theory]
        [InlineData(0, 1f)]
        [InlineData(1, 0.8f)]
        [InlineData(2, 0.45f)]
        [InlineData(3, 0.2f)]
        [InlineData(4, 0.08f)]
        [InlineData(5, 0f)]
        [InlineData(6, 0f)]
        public void QualityTable(int quality, float expected)
        {
            Assert.Equal(expected, NicetiesRules.DailyWearChance(true, false, quality, -1, false), 4);
        }

        [Fact]
        public void CraftingTwentyHalvesWear()
        {
            Assert.Equal(0.225f, NicetiesRules.DailyWearChance(true, false, 2, 20, false), 4);
            Assert.Equal(0.225f, NicetiesRules.DailyWearChance(true, false, 2, 25, false), 4);
        }

        [Fact]
        public void NoQualityScalingMeansNoWear()
        {
            Assert.Equal(0f, NicetiesRules.DailyWearChance(false, false, 0, -1, false));
        }

        [Fact]
        public void CorpsesWearAsVanillaUnlessProtected()
        {
            Assert.Equal(1f, NicetiesRules.DailyWearChance(true, false, 6, 20, true));
            Assert.Equal(0f, NicetiesRules.DailyWearChance(true, true, 6, 20, true));
        }
    }

    public class SettingsTests
    {
        [Fact]
        public void PresetsNeverTouchMasterToggles()
        {
            foreach (NicetiesPreset preset in new[] { NicetiesPreset.Soft, NicetiesPreset.Default, NicetiesPreset.Hard })
            {
                NicetiesSimFlags sim = NicetiesSimFlags.Defaults;
                sim.enableApparelCare = false;
                sim.meleeHunting = false;
                sim.enableLeaveAWayOut = false;
                sim.wearAnyGenderGenerated = true;
                sim.leaveAWayOutMaxRoom = 200;
                sim.ApplyKnobsFrom(NicetiesPresets.Knobs(preset));
                Assert.False(sim.enableApparelCare);
                Assert.False(sim.meleeHunting);
                Assert.False(sim.enableLeaveAWayOut);
                Assert.True(sim.wearAnyGenderGenerated);
                Assert.Equal(200, sim.leaveAWayOutMaxRoom);
                Assert.Equal(preset, NicetiesPresets.Active(sim));
            }
        }

        [Fact]
        public void DefaultsAreTheDefaultPreset()
        {
            Assert.Equal(NicetiesPreset.Default, NicetiesPresets.Active(NicetiesSimFlags.Defaults));
        }

        [Fact]
        public void DefaultPresetMatchesFullResetKnobs()
        {
            NicetiesSimFlags sim = NicetiesPresets.Knobs(NicetiesPreset.Hard);
            sim.ApplyKnobsFrom(NicetiesPresets.Knobs(NicetiesPreset.Default));
            Assert.Equal(NicetiesSimFlags.Defaults, sim);
        }

        [Fact]
        public void EditedKnobIsCustom()
        {
            NicetiesSimFlags sim = NicetiesSimFlags.Defaults;
            sim.meleeHuntMaxBodySize = 3.3f;
            Assert.Null(NicetiesPresets.Active(sim));
        }

        [Fact]
        public void ResetFeatureOnlyResetsThatFeature()
        {
            NicetiesSimFlags sim = NicetiesSimFlags.Defaults;
            sim.meleeHunting = false;
            sim.meleeHuntMaxBodySize = 4f;
            sim.enableApparelCare = false;
            sim.ResetFeature(NicetiesFeature.MeleeHunting);
            Assert.True(sim.meleeHunting);
            Assert.Equal(NicetiesLimits.DefaultMeleeCap, sim.meleeHuntMaxBodySize);
            Assert.False(sim.enableApparelCare);
        }

        [Fact]
        public void EveryFeatureResetReturnsToDefaults()
        {
            NicetiesSimFlags sim = NicetiesPresets.Knobs(NicetiesPreset.Hard);
            sim.enableApparelCare = sim.allowThroneAltars = sim.wearAnyGender = sim.hideCryptosleep = false;
            sim.meleeHunting = sim.enableSharedRooms = sim.enableLeaveAWayOut = false;
            sim.wearAnyGenderGenerated = true;
            sim.skipDisturbedSleepWhenSharing = false;
            sim.leaveAWayOutMaxRoom = 300;
            NicetiesUiPrefs ui = default;
            for (int i = 0; i <= (int)NicetiesFeature.LeaveAWayOut; i++)
            {
                sim.ResetFeature((NicetiesFeature)i);
                ui.ResetFeature((NicetiesFeature)i);
            }

            Assert.Equal(NicetiesSimFlags.Defaults, sim);
            Assert.Equal(NicetiesUiPrefs.Defaults, ui);
            Assert.True(ui.showCryptosleepCount);
        }

        [Fact]
        public void ClampKeepsSlidersInRange()
        {
            NicetiesSimFlags sim = NicetiesSimFlags.Defaults;
            sim.meleeHuntMaxBodySize = 99f;
            sim.meleeHuntMaxRevengeChance = -1f;
            sim.leaveAWayOutMaxRoom = 1;
            sim.Clamp();
            Assert.Equal(NicetiesLimits.MaxMeleeCap, sim.meleeHuntMaxBodySize);
            Assert.Equal(0f, sim.meleeHuntMaxRevengeChance);
            Assert.Equal(NicetiesLimits.MinMaxRoom, sim.leaveAWayOutMaxRoom);
        }
    }
}
