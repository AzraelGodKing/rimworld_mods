using System;
using System.Collections.Generic;
using System.Reflection;
using HarmonyLib;
using Verse;

namespace Strata
{
    // Soft VEF PipeSystem registration: only adds shaft backends when the
    // target PipeNetDef resolves at startup via GetNamedSilentFail.
    internal static class VefPipeNetDiscovery
    {
        // Common VEF pipe net def names — unknown names are skipped silently.
        private static readonly string[] CandidateNetDefNames =
        {
            "VCHE_ChemfuelNet",
            "VNPE_NutrientPasteNet",
            "VNPE_PipeNet",
            "VFE_PipeNet",
        };

        internal static IEnumerable<VefPipeSystemBackend> DiscoverBackends()
        {
            if (!ModsConfig.IsActive("OskarPotocki.VanillaFactionsExpanded.Core"))
            {
                yield break;
            }

            var seenChannels = new HashSet<string>();
            var seenNetDefs = new HashSet<string>();

            foreach (string defName in CandidateNetDefNames)
            {
                VefPipeSystemBackend backend = TryMakeBackend(defName, ChannelFor(defName), LabelFor(defName));
                if (backend != null && seenChannels.Add(backend.ChannelId))
                {
                    seenNetDefs.Add(defName);
                    yield return backend;
                }
            }

            foreach (Def def in AllPipeNetDefs())
            {
                if (def?.defName == null || seenNetDefs.Contains(def.defName))
                {
                    continue;
                }
                string name = def.defName;
                if (!name.Contains("Chemfuel") && !name.Contains("Nutrient"))
                {
                    continue;
                }
                VefPipeSystemBackend backend = TryMakeBackend(name, ChannelFor(name), def.LabelCap);
                if (backend != null && seenChannels.Add(backend.ChannelId))
                {
                    yield return backend;
                }
            }
        }

        private static VefPipeSystemBackend TryMakeBackend(string netDefName, string channelId, string label)
        {
            var backend = new VefPipeSystemBackend(channelId, label, netDefName);
            return backend.IsActive ? backend : null;
        }

        private static string ChannelFor(string netDefName)
        {
            if (netDefName == "VCHE_ChemfuelNet")
            {
                return "vef_chemfuel";
            }
            if (netDefName.Contains("Nutrient"))
            {
                return "vef_nutrient";
            }
            if (netDefName.Contains("Chemfuel"))
            {
                return "vef_chemfuel";
            }
            return "vef_" + netDefName.ToLowerInvariant();
        }

        private static string LabelFor(string netDefName)
        {
            if (netDefName == "VCHE_ChemfuelNet")
            {
                return "VEF chemfuel";
            }
            if (netDefName.Contains("Nutrient"))
            {
                return "VEF nutrient paste";
            }
            return netDefName;
        }

        private static IEnumerable<Def> AllPipeNetDefs()
        {
            Type pipeNetDefType = ReflectionUtil.TypeIn("PipeSystem.PipeNetDef", ReflectionUtil.FindAssembly("VEF"));
            if (pipeNetDefType == null)
            {
                yield break;
            }
            Type databaseType = typeof(DefDatabase<>).MakeGenericType(pipeNetDefType);
            object allDefs = databaseType.GetProperty("AllDefs", BindingFlags.Public | BindingFlags.Static)
                ?.GetValue(null, null);
            if (!(allDefs is System.Collections.IEnumerable defs))
            {
                yield break;
            }
            foreach (object def in defs)
            {
                if (def is Def typed)
                {
                    yield return typed;
                }
            }
        }
    }
}
