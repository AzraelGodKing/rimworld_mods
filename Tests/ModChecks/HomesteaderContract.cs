using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Xml.Linq;
using Xunit;

namespace Azrael.ModChecks
{
    /// <summary>
    /// Homesteader's frozen names (AZR-562, AZR-544). Sibling mods find Homesteader
    /// by these defNames and class names, and saves keep spawner progress under these
    /// saveKeysPrefix values. Renaming any of them breaks a sibling or resets saves.
    /// The same list, with who uses each name, is in Homesteader/COMPATIBILITY.md.
    /// </summary>
    public class HomesteaderContract
    {
        static readonly (string Type, string DefName)[] FrozenDefs =
        {
            ("ThingDef", "Homesteader_WoodGenerator"),
            ("ThingDef", "Homesteader_PortableGenerator"),
            ("ThingDef", "Homesteader_RootCellar"),
            ("ThingDef", "Homesteader_Icehouse"),
            ("ThingDef", "Homesteader_Springhouse"),
            ("ThingDef", "Homesteader_Smokehouse"),
            ("ThingDef", "Homesteader_PreservesShelf"),
            ("ThingDef", "Homesteader_IngredientBarrel"),
            ("ThingDef", "Homesteader_StorageBarrel"),
            ("ThingDef", "Homesteader_StorageCrate"),
            ("ThingDef", "Homesteader_LargeStorageCrate"),
            ("ThingDef", "Homesteader_HarvestMaypole"),
            ("ThingDef", "Wellspring_HandDugWell"),
            ("TerrainDef", "Homesteader_PackedGravel"),
            ("TerrainDef", "Wellspring_IrrigatedSoil"),
            ("RecipeDef", "Wellspring_DrawWater"),
            ("RecipeDef", "Wellspring_DrawWaterDeep"),
            ("ScenPartDef", "Homesteader_ForcedStoryteller"),
            ("StorytellerDef", "Azrael"),
            ("ScenarioDef", "Homesteader_Homesteaders"),
        };

        static readonly (string DefName, string Prefix)[] FrozenSpawnerKeys =
        {
            ("Homesteader_ChickenCoop", "eggs"),
            ("Homesteader_Beehive", "honey"),
            ("Homesteader_Beehive", "beeswax"),
            ("Wellspring_RainBarrel", "rainwater"),
            ("Wellspring_Cistern", "cistern"),
            ("Wellspring_SolarStill", "solarstill"),
            ("Wellspring_WaterTower", "watertower"),
        };

        // Sibling scenarios name this class in XML (Class="Homesteader.ScenPart_ForcedStoryteller").
        const string ForcedStorytellerClass = "public class ScenPart_ForcedStoryteller";

        [Fact]
        public void FrozenNamesStay()
        {
            string repo = FindRepoRoot();
            Assert.False(repo == null, "Could not find repo root (no Common/SafePatchAll.cs).");
            string mod = Path.Combine(repo, "Homesteader");

            List<XElement> defs = Directory.GetFiles(mod, "*.xml", SearchOption.AllDirectories)
                .Where(f => f.Replace('\\', '/').Contains("/Defs/"))
                .Select(XDocument.Load)
                .Where(d => d.Root?.Name.LocalName == "Defs")
                .SelectMany(d => d.Root.Elements())
                .ToList();

            var errors = new List<string>();
            foreach ((string type, string defName) in FrozenDefs)
            {
                if (!defs.Any(d => d.Name.LocalName == type && (string)d.Element("defName") == defName))
                    errors.Add("frozen " + type + " renamed or removed: " + defName);
            }

            foreach ((string defName, string prefix) in FrozenSpawnerKeys)
            {
                XElement def = defs.FirstOrDefault(d => (string)d.Element("defName") == defName);
                bool found = def?.Element("comps")?.Elements("li")
                    .Any(li => (string)li.Element("saveKeysPrefix") == prefix) == true;
                if (!found)
                    errors.Add("frozen spawner saveKeysPrefix missing: " + defName + " '" + prefix + "'");
            }

            string scenPart = Path.Combine(mod, "Source", "ScenPart_ForcedStoryteller.cs");
            if (!File.Exists(scenPart) || !File.ReadAllText(scenPart).Contains("namespace Homesteader")
                || !File.ReadAllText(scenPart).Contains(ForcedStorytellerClass))
                errors.Add("Homesteader.ScenPart_ForcedStoryteller moved or renamed (Source/ScenPart_ForcedStoryteller.cs)");

            Assert.True(errors.Count == 0, string.Join("\n  ", errors));
        }

        static string FindRepoRoot()
        {
            string dir = AppContext.BaseDirectory;
            for (int i = 0; i < 10 && dir != null; i++)
            {
                if (File.Exists(Path.Combine(dir, "Common", "SafePatchAll.cs")))
                    return dir;
                dir = Directory.GetParent(dir)?.FullName;
            }
            return null;
        }
    }
}
