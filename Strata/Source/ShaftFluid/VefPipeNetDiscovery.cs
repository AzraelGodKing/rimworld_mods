using System;
using System.Collections.Generic;
using System.Reflection;
using HarmonyLib;
using RimWorld;
using Verse;

namespace Strata
{
    // Every VEF PipeSystem net crosses shafts, including nets from mods
    // nobody wrote a Strata patch for. Two halves:
    //   1) Before defs resolve, each PipeNetDef without a dedicated junction
    //      gets a PipeSystem CompProperties_Resource plus a CompShaftFluidTie
    //      on the Omni connector and on the generic pipe-network junction.
    //   2) ShaftFluidRegistry registers one VefPipeSystemBackend per net.
    // Each net keeps its own tie (and its own link toggle), so one network can
    // be shut off at a junction without touching the others.
    //
    // Dedicated junctions stay for nets that have one: helixien (storage
    // junction), VTE AC (efficiency, own backend) and VCE chemfuel.
    internal static class VefPipeNetDiscovery
    {
        public const string AutoJunctionDefName = "Strata_ShaftFluid_VefAuto";
        public const string OmniDefName = "Strata_ShaftFluid_Omni";
        public const string ChemfuelNetDefName = "VCHE_ChemfuelNet";

        private static readonly HashSet<string> DedicatedNetDefNames = new HashSet<string>
        {
            "VHGE_HelixienNet",
            VteAcPipeBackend.PipeNetDefName,
            ChemfuelNetDefName,
        };

        private static readonly List<string> autoNetDefNames = new List<string>();

        // Nets that got an auto junction this session (for the inspect text / logs).
        public static IReadOnlyList<string> AutoNetDefNames => autoNetDefNames;

        public static bool IsDedicated(string netDefName) => DedicatedNetDefNames.Contains(netDefName);

        public static string AutoChannelFor(string netDefName) => "vef_auto_" + netDefName.ToLowerInvariant();

        // Called from the StrataMod constructor, before any defs load.
        public static void PatchDefInjection(Harmony harmony)
        {
            try
            {
                harmony.Patch(
                    AccessTools.Method(typeof(DefGenerator), nameof(DefGenerator.GenerateImpliedDefs_PreResolve)),
                    postfix: new HarmonyMethod(typeof(VefPipeNetDiscovery), nameof(InjectJunctionComps)));
            }
            catch (Exception e)
            {
                Log.Warning("[Strata] Could not hook VEF pipe-net discovery: " + e.Message);
            }
        }

        public static void InjectJunctionComps()
        {
            try
            {
                Inject();
            }
            catch (Exception e)
            {
                StrataFaultGuard.Report(StrataFaultGuard.System.ShaftFluid, "adding auto pipe-net junctions", e);
            }
        }

        private static void Inject()
        {
            autoNetDefNames.Clear();
            ThingDef auto = DefDatabase<ThingDef>.GetNamedSilentFail(AutoJunctionDefName);
            Type propsType = AccessTools.TypeByName("PipeSystem.CompProperties_Resource");
            FieldInfo pipeNetField = propsType != null ? AccessTools.Field(propsType, "pipeNet") : null;
            if (pipeNetField == null)
            {
                if (auto != null)
                {
                    auto.designationCategory = null;
                }
                return;
            }
            ThingDef omni = DefDatabase<ThingDef>.GetNamedSilentFail(OmniDefName);
            foreach (Def net in AllPipeNetDefs())
            {
                if (net?.defName == null || IsDedicated(net.defName))
                {
                    continue;
                }
                string channel = AutoChannelFor(net.defName);
                AddComps(omni, propsType, pipeNetField, net, channel);
                AddComps(auto, propsType, pipeNetField, net, channel);
                autoNetDefNames.Add(net.defName);
            }
            if (autoNetDefNames.Count == 0 && auto != null)
            {
                // Nothing to connect: keep the generic junction out of the build menu.
                auto.designationCategory = null;
            }
            else if (autoNetDefNames.Count > 0)
            {
                Log.Message("[Strata] Shaft junctions auto-linked " + autoNetDefNames.Count
                    + " VEF pipe network(s): " + string.Join(", ", autoNetDefNames));
            }
        }

        private static void AddComps(ThingDef target, Type propsType, FieldInfo pipeNetField, Def net, string channel)
        {
            if (target == null)
            {
                return;
            }
            target.comps ??= new List<CompProperties>();
            for (int i = 0; i < target.comps.Count; i++)
            {
                if (target.comps[i] is CompProperties_ShaftFluidTie existing && existing.channel == channel)
                {
                    return;
                }
            }
            var resource = (CompProperties)Activator.CreateInstance(propsType);
            pipeNetField.SetValue(resource, net);
            target.comps.Add(resource);
            target.comps.Add(new CompProperties_ShaftFluidTie { channel = channel });
        }

        internal static IEnumerable<VefPipeSystemBackend> DiscoverBackends()
        {
            if (!ModsConfig.IsActive("OskarPotocki.VanillaFactionsExpanded.Core"))
            {
                yield break;
            }

            VefPipeSystemBackend chemfuel = TryMakeBackend(ChemfuelNetDefName, "vef_chemfuel", "VEF chemfuel");
            if (chemfuel != null)
            {
                yield return chemfuel;
            }

            foreach (Def def in AllPipeNetDefs())
            {
                if (def?.defName == null || IsDedicated(def.defName))
                {
                    continue;
                }
                VefPipeSystemBackend backend = TryMakeBackend(def.defName, AutoChannelFor(def.defName), def.LabelCap);
                if (backend != null)
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

        private static IEnumerable<Def> AllPipeNetDefs()
        {
            Type pipeNetDefType = AccessTools.TypeByName("PipeSystem.PipeNetDef");
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
