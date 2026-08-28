using System;
using System.Reflection;
using HarmonyLib;
using Verse;

namespace Strata
{
    // Soft bridge to Vanilla Helixien Gas Expanded (the VEF PipeSystem).
    // Entirely reflection-based - no hard reference, nothing breaks when the
    // mod is absent, and nothing is patched inside the framework. When VHGE is
    // loaded, the gas well gains a PipeSystem resource comp wired to the
    // helixien gas net, and each extraction pushes units into the connected
    // network's storage instead of ejecting a canister. If any part of the
    // framework's surface has changed, the adapter logs once and the well
    // falls back to canisters.
    public static class GasNetAdapter
    {
        private static bool available;
        private static Type compResourceType;
        private static PropertyInfo pipeNetProperty;
        private static bool pushWarned;

        public static string Status { get; private set; } = "not initialized";

        // Called once from mod startup, after defs load.
        public static void Inject()
        {
            try
            {
                compResourceType = GenTypes.GetTypeInAnyAssembly("PipeSystem.CompResource");
                Type propsType = GenTypes.GetTypeInAnyAssembly("PipeSystem.CompProperties_Resource");
                Type netDefType = GenTypes.GetTypeInAnyAssembly("PipeSystem.PipeNetDef");
                if (compResourceType == null || propsType == null || netDefType == null)
                {
                    Status = "VEF PipeSystem not loaded - canisters only";
                    return;
                }
                Def gasNet = null;
                foreach (Def def in GenDefDatabase.GetAllDefsInDatabaseForDef(netDefType))
                {
                    if (def.defName.IndexOf("helixien", StringComparison.OrdinalIgnoreCase) >= 0)
                    {
                        gasNet = def;
                        break;
                    }
                }
                if (gasNet == null)
                {
                    Status = "PipeSystem loaded but no helixien gas net found - canisters only";
                    return;
                }
                FieldInfo pipeNetField = AccessTools.Field(propsType, "pipeNet");
                pipeNetProperty = AccessTools.Property(compResourceType, "PipeNet");
                if (pipeNetField == null || pipeNetProperty == null)
                {
                    Status = "PipeSystem surface changed (pipeNet/PipeNet missing) - canisters only";
                    Log.Warning("[Strata] " + Status);
                    return;
                }
                var resourceProps = (CompProperties)Activator.CreateInstance(propsType);
                resourceProps.compClass = compResourceType;
                pipeNetField.SetValue(resourceProps, gasNet);
                StrataThingDefOf.Strata_GasWell.comps.Add(resourceProps);
                available = true;
                Status = $"feeding '{gasNet.defName}'";
                StrataLog.Verbose("[Strata] Helixien gas network detected - gas wells can feed its pipes.");
            }
            catch (Exception e)
            {
                available = false;
                Status = "adapter failed: " + e.Message;
                Log.Warning("[Strata] Gas network adapter disabled: " + e);
            }
        }

        private static object ConnectedNet(ThingWithComps well)
        {
            if (!available || well?.AllComps == null)
            {
                return null;
            }
            for (int i = 0; i < well.AllComps.Count; i++)
            {
                if (compResourceType.IsInstanceOfType(well.AllComps[i]))
                {
                    return pipeNetProperty.GetValue(well.AllComps[i]);
                }
            }
            return null;
        }

        public static bool IsConnected(ThingWithComps well)
        {
            try
            {
                object net = ConnectedNet(well);
                // A net of one lonely well stores nothing; only count real
                // networks with connectors beyond ourselves.
                return net != null && NetHasStorage(net);
            }
            catch
            {
                return false;
            }
        }

        private static bool NetHasStorage(object net)
        {
            object capacity = AccessTools.Property(net.GetType(), "AvailableCapacity")?.GetValue(net);
            return capacity is float f && f >= 1f;
        }

        // Push extracted gas into the connected network's storage. Returns
        // false (fall back to a canister) when there is no network, no spare
        // capacity, or the framework refuses.
        public static bool TryPush(ThingWithComps well, float amount)
        {
            if (!available)
            {
                return false;
            }
            try
            {
                object net = ConnectedNet(well);
                if (net == null || !NetHasStorage(net))
                {
                    return false;
                }
                MethodInfo distribute = AccessTools.Method(net.GetType(), "DistributeAmongStorage");
                if (distribute == null)
                {
                    return false;
                }
                ParameterInfo[] pars = distribute.GetParameters();
                if (pars.Length == 1)
                {
                    distribute.Invoke(net, new object[] { amount });
                }
                else if (pars.Length == 2 && pars[1].IsOut)
                {
                    distribute.Invoke(net, new object[] { amount, null });
                }
                else
                {
                    return false;
                }
                return true;
            }
            catch (Exception e)
            {
                if (!pushWarned)
                {
                    pushWarned = true;
                    Log.Warning("[Strata] Pushing gas into the pipe network failed; falling back to canisters: " + e);
                }
                return false;
            }
        }
    }
}
