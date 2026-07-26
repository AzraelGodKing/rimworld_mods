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
            // Soft no-op when Rimefeller isn't loaded (Omni / multi-channel junctions still call us).
            if (ModLister.GetActiveModWithIdentifier("Dubwise.Rimefeller") == null)
            {
                return;
            }
            if (thing?.Map == null || !IsRimefellerJunction(thing))
            {
                return;
            }
            if (!TryBind())
            {
                return;
            }
            object mapComp = thing.Map.GetComponent(mapCompType);
            regenPipeGridsMethod?.Invoke(mapComp, null);
        }

        private static bool IsRimefellerJunction(Thing thing)
        {
            return CompShaftFluidTie.HasChannelPrefix(thing, "rimefeller_");
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
