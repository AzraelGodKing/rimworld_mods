using System.Collections;
using System.Collections.Generic;
using System.Reflection;
using HarmonyLib;
using UnityEngine;
using Verse;

namespace Strata
{
    // Junctions carry CompWaterStorage so fixtures see WaterTowers. This postfix only
    // covers leftover CompShaftFluidTie.ShaftResourceBuffer when PullWater still fails
    // (do not inflate WaterStorage itself — that skews AASB fill equalize).
    [HarmonyPatch]
    public static class Patch_DbhPullWaterFromShaftBuffer
    {
        private static bool Prepare()
        {
            return TargetMethod() != null;
        }

        private static MethodBase TargetMethod()
        {
            return AccessTools.Method(AccessTools.TypeByName("DubsBadHygiene.PlumbingNet"), "PullWater");
        }

        public static void Postfix(object __instance, float waterUsed, ref bool __result)
        {
            if (__result || __instance == null || waterUsed <= 0.0001f)
            {
                return;
            }
            if (DbhShaftWaterBuffer.TryPull(__instance, waterUsed))
            {
                __result = true;
            }
        }
    }

    internal static class DbhShaftWaterBuffer
    {
        private const string Channel = "dbh_plumbing";

        private static FieldInfo pipedThingsField;

        private static FieldInfo mapCompField;

        private static FieldInfo pipeCompField;

        private static FieldInfo netIdField;

        private static bool fieldsResolved;

        public static float BufferOnNet(object plumbingNet)
        {
            float sum = 0f;
            foreach (CompShaftFluidTie tie in TiesOnNet(plumbingNet))
            {
                sum += tie.ShaftResourceBuffer;
            }
            return sum;
        }

        public static bool TryPull(object plumbingNet, float amount)
        {
            if (amount <= 0f || plumbingNet == null)
            {
                return false;
            }
            float need = amount;
            foreach (CompShaftFluidTie tie in TiesOnNet(plumbingNet))
            {
                if (need <= 0.0001f)
                {
                    break;
                }
                float take = Mathf.Min(need, tie.ShaftResourceBuffer);
                if (take <= 0f)
                {
                    continue;
                }
                tie.TakeShaftResourceBuffer(take);
                need -= take;
            }
            return need <= 0.0001f;
        }

        private static IEnumerable<CompShaftFluidTie> TiesOnNet(object plumbingNet)
        {
            Map map = TryGetMap(plumbingNet);
            if (map == null)
            {
                yield break;
            }
            ShaftFluidBackend backend = ShaftFluidRegistry.Get(Channel);
            if (backend == null || !backend.IsActive)
            {
                yield break;
            }
            ResolveFields();
            int wantId = NetId(plumbingNet);
            List<Thing> buildings = map.listerThings.ThingsInGroup(ThingRequestGroup.BuildingArtificial);
            for (int i = 0; i < buildings.Count; i++)
            {
                Thing building = buildings[i];
                CompShaftFluidTie tie = CompShaftFluidTie.FindOn(building, Channel);
                if (tie == null || tie.ShaftResourceBuffer < 0.0001f)
                {
                    continue;
                }
                object net = backend.GetNetFromJunction(building);
                if (net == null)
                {
                    continue;
                }
                if (ReferenceEquals(net, plumbingNet) || (wantId >= 0 && NetId(net) == wantId))
                {
                    yield return tie;
                }
            }
        }

        private static Map TryGetMap(object plumbingNet)
        {
            ResolveFields();
            object pipeComp = pipeCompField?.GetValue(plumbingNet);
            if (pipeComp is MapComponent pipeMc && pipeMc.map != null)
            {
                return pipeMc.map;
            }
            object mapComp = mapCompField?.GetValue(plumbingNet);
            if (mapComp is MapComponent mc && mc.map != null)
            {
                return mc.map;
            }
            object piped = pipedThingsField?.GetValue(plumbingNet);
            if (piped is IEnumerable enumerable)
            {
                foreach (object entry in enumerable)
                {
                    if (entry is Thing thing && thing.Map != null)
                    {
                        return thing.Map;
                    }
                }
            }
            return null;
        }

        private static int NetId(object plumbingNet)
        {
            ResolveFields();
            if (netIdField == null || plumbingNet == null)
            {
                return -1;
            }
            object value = netIdField.GetValue(plumbingNet);
            return value is int id ? id : -1;
        }

        private static void ResolveFields()
        {
            if (fieldsResolved)
            {
                return;
            }
            fieldsResolved = true;
            System.Type netType = AccessTools.TypeByName("DubsBadHygiene.PlumbingNet");
            if (netType == null)
            {
                return;
            }
            pipedThingsField = AccessTools.Field(netType, "PipedThings");
            mapCompField = AccessTools.Field(netType, "MapComp");
            pipeCompField = AccessTools.Field(netType, "PipeComp");
            netIdField = AccessTools.Field(netType, "NetID");
        }
    }
}
