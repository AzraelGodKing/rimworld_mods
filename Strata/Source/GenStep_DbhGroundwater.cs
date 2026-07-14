using System.Reflection;
using HarmonyLib;
using RimWorld;
using UnityEngine;
using Verse;

namespace Strata
{
    // DBH wells need groundwater on Strata underground levels. MapComponent_Hygiene
    // normally finalizes grids on load; we force a regen after Strata carves rock.
    public static class DbhGroundwaterUtility
    {
        private static MethodInfo regenWater;

        private static MethodInfo regenDeep;

        private static bool bindLogged;

        public static bool IsActive => ModsConfig.IsActive("dubwise.dubsbadhygiene");

        public static void SeedIfNeeded(Map map)
        {
            if (!IsActive || map == null || !StrataMapUtility.IsUnderground(map) || !TryBind())
            {
                return;
            }
            object hygiene = map.GetComponent(AccessTools.TypeByName("DubsBadHygiene.MapComponent_Hygiene"));
            if (hygiene == null)
            {
                LogBindOnce("MapComponent_Hygiene missing on underground map — DBH may not have patched this pocket map yet.");
                return;
            }
            try
            {
                regenWater.Invoke(hygiene, null);
                regenDeep.Invoke(hygiene, null);
            }
            catch
            {
                LogBindOnce("RegenWaterGrid/RegenDeepWaterGrid failed — DBH API may have changed.");
            }
        }

        private static bool TryBind()
        {
            if (regenWater != null)
            {
                return true;
            }
            System.Type hygieneType = AccessTools.TypeByName("DubsBadHygiene.MapComponent_Hygiene");
            if (hygieneType == null)
            {
                LogBindOnce("MapComponent_Hygiene type not found.");
                return false;
            }
            regenWater = AccessTools.Method(hygieneType, "RegenWaterGrid");
            regenDeep = AccessTools.Method(hygieneType, "RegenDeepWaterGrid");
            if (regenWater == null || regenDeep == null)
            {
                LogBindOnce("DBH water grid regen methods not found.");
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
            Log.Warning("[Strata] DBH groundwater: " + message);
        }
    }

    public class GenStep_DbhGroundwater : GenStep
    {
        public override int SeedPart => 918273645;

        public override void Generate(Map map, GenStepParams parms)
        {
            DbhGroundwaterUtility.SeedIfNeeded(map);
        }
    }
}
