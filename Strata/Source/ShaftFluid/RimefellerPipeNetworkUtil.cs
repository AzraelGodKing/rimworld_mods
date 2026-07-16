using System;
using System.Reflection;
using RimWorld;
using Verse;

namespace Strata
{
    internal static class RimefellerPipeNetworkUtil
    {
        private static MethodInfo regenPipeGridsMethod;

        private static bool bindLogged;

        public static void ReconnectJunction(Thing thing)
        {
            if (!TryBind() || thing?.Map == null || !IsRimefellerJunction(thing))
            {
                return;
            }
            object mapComp = thing.Map.GetComponent(mapCompType);
            regenPipeGridsMethod?.Invoke(mapComp, null);
        }

        private static bool IsRimefellerJunction(Thing thing)
        {
            CompShaftFluidTie tie = thing.TryGetComp<CompShaftFluidTie>();
            if (tie?.Props.channel == null)
            {
                return false;
            }
            return tie.Props.channel.StartsWith("rimefeller_");
        }

        private static Type mapCompType;

        private static bool TryBind()
        {
            if (regenPipeGridsMethod != null)
            {
                return true;
            }
            Assembly asm = ReflectionUtil.FindAssembly("Rimefeller");
            mapCompType = ReflectionUtil.TypeIn("Rimefeller.UniversalPipeMapComp", asm);
            if (mapCompType == null)
            {
                LogBindOnce("UniversalPipeMapComp not found.");
                return false;
            }
            regenPipeGridsMethod = mapCompType.GetMethod("RegenPipeGrids", BindingFlags.Instance | BindingFlags.Public);
            if (regenPipeGridsMethod == null)
            {
                LogBindOnce("RegenPipeGrids not found.");
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
            Log.Warning("[Strata] Rimefeller pipe util: " + message);
        }
    }
}
