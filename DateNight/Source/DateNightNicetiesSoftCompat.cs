#nullable enable
using System;
using System.Collections.Generic;
using System.Reflection;
using HarmonyLib;
using RimWorld;
using Verse;

namespace DateNight
{
    /// <summary>
    /// Niceties shared rooms (AZR-574). Niceties owns bed assignment in a room it
    /// marked as shared, so Date Night's Lovin-window bed claim stays out of those
    /// rooms; the couple still uses the bed, it just is not reassigned.
    /// No-op without Niceties.
    /// </summary>
    public static class DateNightNicetiesSoftCompat
    {
        private static bool resolved;
        private static Type? compType;
        private static PropertyInfo? sharedProp;

        public static bool InSharedRoom(Building_Bed bed)
        {
            Resolve();
            if (compType == null || sharedProp == null || bed?.Map == null)
            {
                return false;
            }
            Room? room = bed.GetRoom();
            if (room == null || room.PsychologicallyOutdoors)
            {
                return false;
            }
            foreach (Building_Bed other in room.ContainedBeds)
            {
                if (IsMarked(other))
                {
                    return true;
                }
            }
            return false;
        }

        private static bool IsMarked(Building_Bed bed)
        {
            List<ThingComp> comps = bed.AllComps;
            for (int i = 0; i < comps.Count; i++)
            {
                if (comps[i].GetType() == compType)
                {
                    try
                    {
                        return sharedProp!.GetValue(comps[i], null) is bool shared && shared;
                    }
                    catch
                    {
                        return false;
                    }
                }
            }
            return false;
        }

        private static void Resolve()
        {
            if (resolved)
            {
                return;
            }
            resolved = true;
            compType = AccessTools.TypeByName("Niceties.CompSharedRoom");
            sharedProp = compType == null ? null : AccessTools.Property(compType, "Shared");
        }
    }
}
