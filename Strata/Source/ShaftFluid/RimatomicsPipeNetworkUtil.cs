using System;
using System.Reflection;
using HarmonyLib;
using Verse;

namespace Strata
{
    // Rimatomics pipe nets rebuild from a grid map comp; nudge regen when shaft
    // coolant junctions spawn or load so CompPipe joins the local cooling loop.
    internal static class RimatomicsPipeNetworkUtil
    {
        private const string RimatomicsChannel = "rimatomics_coolant";

        private static MethodInfo rimatomicsMapMethod;

        private static MethodInfo dirtyPipeGridMethod;

        private static MethodInfo regenGridsMethod;

        private static Type pipeTypeEnum;

        private static object coolingPipeType;

        private static bool bindLogged;

        internal static void ReconnectJunction(Thing thing)
        {
            if (!TryBind() || thing?.Spawned != true || !IsRimatomicsJunction(thing))
            {
                return;
            }
            object mapComp = rimatomicsMapMethod.Invoke(null, new object[] { thing.Map });
            if (mapComp == null)
            {
                return;
            }
            dirtyPipeGridMethod.Invoke(mapComp, new[] { coolingPipeType });
            regenGridsMethod.Invoke(mapComp, null);
        }

        private static bool IsRimatomicsJunction(Thing thing)
        {
            return CompShaftFluidTie.HasChannel(thing, RimatomicsChannel);
        }

        private static bool TryBind()
        {
            if (rimatomicsMapMethod != null)
            {
                return true;
            }
            Assembly asm = ReflectionUtil.FindAssembly("Rimatomics");
            Type dubUtilsType = ReflectionUtil.TypeIn("Rimatomics.DubUtils", asm);
            pipeTypeEnum = ReflectionUtil.TypeIn("Rimatomics.PipeType", asm);
            Type mapCompType = ReflectionUtil.TypeIn("Rimatomics.MapComponent_Rimatomics", asm);
            if (dubUtilsType == null || pipeTypeEnum == null || mapCompType == null)
            {
                LogBindOnce("Rimatomics pipe util types not found.");
                return false;
            }

            rimatomicsMapMethod = dubUtilsType.GetMethod("Rimatomics", BindingFlags.Static | BindingFlags.Public, null, new[] { typeof(Map) }, null);
            dirtyPipeGridMethod = mapCompType.GetMethod("DirtyPipeGrid", BindingFlags.Instance | BindingFlags.Public);
            regenGridsMethod = mapCompType.GetMethod("RegenGrids", BindingFlags.Instance | BindingFlags.Public);
            coolingPipeType = Enum.Parse(pipeTypeEnum, "Cooling");

            if (rimatomicsMapMethod == null || dirtyPipeGridMethod == null || regenGridsMethod == null || coolingPipeType == null)
            {
                LogBindOnce("Rimatomics pipe util reflection bind incomplete.");
                rimatomicsMapMethod = null;
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
            Log.Warning("[Strata] Rimatomics pipe util: " + message);
        }
    }
}
