using System;
using System.Collections;
using System.Collections.Generic;
using System.Reflection;
using HarmonyLib;
using Verse;

namespace Strata
{
    // VEF PipeSystem only links cardinal neighbours (not same-cell pipes on another
    // layer). Shaft fluid junctions sit on Building; ducts/pipes use Conduits.
    internal static class VefPipeNetworkUtil
    {
        private static readonly string[] ShaftChannels =
        {
            "vhge_helixien",
            VteAcPipeBackend.Channel,
            "vef_chemfuel",
        };

        private static readonly string[] ShaftNetDefNames =
        {
            "VHGE_HelixienNet",
            VteAcPipeBackend.PipeNetDefName,
            "VCHE_ChemfuelNet",
        };

        private static Type compResourceType;

        private static Type pipeNetType;

        private static PropertyInfo pipeNetProp;

        private static PropertyInfo propsProp;

        private static FieldInfo parentField;

        private static FieldInfo connectorsField;

        private static MethodInfo registerCompMethod;

        private static MethodInfo unregisterCompMethod;

        private static MethodInfo mergeMethod;

        private static bool bindLogged;

        internal static bool IsAvailable => TryBind();

        internal static void ReconnectJunction(Thing thing)
        {
            if (!TryBind() || thing?.Spawned != true || !IsShaftVefJunction(thing))
            {
                return;
            }
            if (thing is not ThingWithComps twc)
            {
                return;
            }
            foreach (object comp in GetMatchingResourceComps(twc))
            {
                TryJoinTouchingNets(comp);
            }
        }

        internal static void OnConnectorRegistered(object comp)
        {
            if (!TryBind() || comp == null || !compResourceType.IsInstanceOfType(comp))
            {
                return;
            }
            if (!IsShaftManagedComp(comp))
            {
                return;
            }

            Thing parent = GetParent(comp);
            if (parent == null || !parent.Spawned)
            {
                return;
            }

            // Junction placed/loaded: join same-cell or adjacent pipe nets.
            if (IsShaftVefJunction(parent))
            {
                TryJoinTouchingNets(comp);
                return;
            }

            // Ordinary pipe segment: pull a same-cell shaft junction into this net.
            object net = GetPipeNet(comp);
            if (net == null)
            {
                return;
            }
            Map map = parent.Map;
            foreach (IntVec3 cell in parent.OccupiedRect())
            {
                PullSameCellJunctions(map, cell, net, GetCompPipeNetDef(comp));
            }
        }

        private static bool IsShaftVefJunction(Thing thing)
        {
            if (thing is not ThingWithComps twc)
            {
                return false;
            }
            for (int i = 0; i < twc.AllComps.Count; i++)
            {
                if (twc.AllComps[i] is not CompShaftFluidTie tie || tie.Props.channel.NullOrEmpty())
                {
                    continue;
                }
                for (int c = 0; c < ShaftChannels.Length; c++)
                {
                    if (tie.Props.channel == ShaftChannels[c])
                    {
                        return true;
                    }
                }
            }
            return false;
        }

        private static void PullSameCellJunctions(Map map, IntVec3 cell, object sourceNet, object expectedNetDef)
        {
            if (map == null || sourceNet == null || !cell.InBounds(map))
            {
                return;
            }
            foreach (Thing thing in cell.GetThingList(map))
            {
                if (thing == null || !thing.Spawned || !IsShaftVefJunction(thing))
                {
                    continue;
                }
                object junctionComp = GetMatchingResourceComp(thing, expectedNetDef);
                if (junctionComp != null)
                {
                    JoinCompToNet(junctionComp, sourceNet);
                }
            }
        }

        private static void TryJoinTouchingNets(object comp)
        {
            object targetNet = FindBestTouchingNet(comp);
            if (targetNet == null)
            {
                return;
            }
            JoinCompToNet(comp, targetNet);
        }

        private static object FindBestTouchingNet(object comp)
        {
            Thing parent = GetParent(comp);
            if (parent?.Map == null)
            {
                return null;
            }

            object ownNet = GetPipeNet(comp);
            object pipeNetDef = GetCompPipeNetDef(comp);
            object bestNet = null;
            int bestSize = -1;

            foreach (IntVec3 cell in TouchingCells(parent))
            {
                if (!cell.InBounds(parent.Map))
                {
                    continue;
                }
                foreach (Thing thing in cell.GetThingList(parent.Map))
                {
                    if (thing is not ThingWithComps twc || twc == parent)
                    {
                        continue;
                    }
                    foreach (object other in GetMatchingResourceComps(twc, pipeNetDef))
                    {
                        object net = GetPipeNet(other);
                        if (net == null || ReferenceEquals(net, ownNet))
                        {
                            continue;
                        }
                        int size = ConnectorCount(net);
                        if (size > bestSize)
                        {
                            bestSize = size;
                            bestNet = net;
                        }
                    }
                }
            }

            return bestNet;
        }

        private static void JoinCompToNet(object comp, object targetNet)
        {
            if (comp == null || targetNet == null)
            {
                return;
            }
            object currentNet = GetPipeNet(comp);
            if (ReferenceEquals(currentNet, targetNet))
            {
                return;
            }
            if (currentNet != null)
            {
                if (ConnectorCount(currentNet) <= 1 && mergeMethod != null)
                {
                    mergeMethod.Invoke(targetNet, new[] { currentNet });
                    return;
                }
                unregisterCompMethod.Invoke(currentNet, new[] { comp });
            }
            if (!ReferenceEquals(GetPipeNet(comp), targetNet))
            {
                registerCompMethod.Invoke(targetNet, new[] { comp });
            }
        }

        private static IEnumerable GetMatchingResourceComps(ThingWithComps thing, object expectedNetDef = null)
        {
            foreach (ThingComp comp in thing.AllComps)
            {
                if (comp == null || !compResourceType.IsInstanceOfType(comp) || !IsShaftManagedComp(comp))
                {
                    continue;
                }
                if (expectedNetDef != null && !PipeNetDefMatches(GetCompPipeNetDef(comp), expectedNetDef))
                {
                    continue;
                }
                yield return comp;
            }
        }

        private static object GetMatchingResourceComp(Thing thing, object expectedNetDef = null)
        {
            if (thing is not ThingWithComps twc)
            {
                return null;
            }
            foreach (object comp in GetMatchingResourceComps(twc, expectedNetDef))
            {
                return comp;
            }
            return null;
        }

        private static bool IsShaftManagedComp(object comp)
        {
            object pipeNetDef = GetCompPipeNetDef(comp);
            for (int i = 0; i < ShaftNetDefNames.Length; i++)
            {
                if (PipeNetDefMatches(pipeNetDef, ShaftNetDefNames[i]))
                {
                    return true;
                }
            }
            return false;
        }

        private static object GetCompPipeNetDef(object comp)
        {
            if (comp == null)
            {
                return null;
            }
            object props = propsProp.GetValue(comp, null);
            return ReflectionUtil.GetMember(props, "pipeNet");
        }

        private static bool PipeNetDefMatches(object pipeNetDef, object expected)
        {
            if (pipeNetDef == null || expected == null)
            {
                return false;
            }
            if (ReferenceEquals(pipeNetDef, expected))
            {
                return true;
            }
            if (pipeNetDef is Def def && expected is string name)
            {
                return def.defName == name;
            }
            if (expected is Def expectedDef && pipeNetDef is Def actualDef)
            {
                return actualDef.defName == expectedDef.defName;
            }
            string defName = ReflectionUtil.GetMember(pipeNetDef, "defName") as string;
            if (expected is string expectedName)
            {
                return defName == expectedName;
            }
            return false;
        }

        private static Thing GetParent(object comp)
        {
            return parentField.GetValue(comp) as Thing;
        }

        private static object GetPipeNet(object comp)
        {
            return pipeNetProp.GetValue(comp, null);
        }

        private static int ConnectorCount(object net)
        {
            IList list = connectorsField.GetValue(net) as IList;
            return list?.Count ?? 0;
        }

        private static IEnumerable<IntVec3> TouchingCells(Thing thing)
        {
            var cells = new HashSet<IntVec3>();
            CellRect rect = thing.OccupiedRect();
            foreach (IntVec3 cell in rect)
            {
                cells.Add(cell);
                cells.Add(cell + IntVec3.North);
                cells.Add(cell + IntVec3.South);
                cells.Add(cell + IntVec3.East);
                cells.Add(cell + IntVec3.West);
            }
            return cells;
        }

        private static bool TryBind()
        {
            if (compResourceType != null)
            {
                return true;
            }
            // PipeSystem lives in PipeSystem.dll (with VEF), not inside VEF.dll.
            Assembly pipeSystem = ReflectionUtil.FindAssembly("PipeSystem")
                ?? ReflectionUtil.FindAssembly("VEF");
            compResourceType = ReflectionUtil.TypeIn("PipeSystem.CompResource", pipeSystem);
            pipeNetType = ReflectionUtil.TypeIn("PipeSystem.PipeNet", pipeSystem);
            if (compResourceType == null || pipeNetType == null)
            {
                LogBindOnce("PipeSystem types not found.");
                return false;
            }

            parentField = AccessTools.Field(typeof(ThingComp), "parent");
            pipeNetProp = compResourceType.GetProperty("PipeNet", BindingFlags.Instance | BindingFlags.Public);
            propsProp = compResourceType.GetProperty("Props", BindingFlags.Instance | BindingFlags.Public);
            connectorsField = pipeNetType.GetField("connectors", BindingFlags.Instance | BindingFlags.Public | BindingFlags.NonPublic);
            registerCompMethod = pipeNetType.GetMethod("RegisterComp", BindingFlags.Instance | BindingFlags.Public);
            unregisterCompMethod = pipeNetType.GetMethod("UnregisterComp", BindingFlags.Instance | BindingFlags.Public);
            mergeMethod = pipeNetType.GetMethod("Merge", BindingFlags.Instance | BindingFlags.Public);

            if (parentField == null || pipeNetProp == null || propsProp == null
                || connectorsField == null || registerCompMethod == null || unregisterCompMethod == null)
            {
                LogBindOnce("PipeSystem reflection bind incomplete.");
                compResourceType = null;
                return false;
            }
            return true;
        }

        private static void LogBindOnce(string message)
        {
            if (bindLogged)
            {
                return;
            }
            bindLogged = true;
            Log.Warning("[Strata] VEF pipe net util: " + message);
        }
    }
}
