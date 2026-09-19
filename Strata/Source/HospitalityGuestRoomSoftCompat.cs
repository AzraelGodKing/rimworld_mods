using System;
using System.Collections;
using System.Collections.Generic;
using System.Reflection;
using HarmonyLib;
using RimWorld;
using Verse;

namespace Strata
{
    /// <summary>
    /// Hospitality visit attraction counts guest beds on the surface map only.
    /// Soft-compat: when Hospitality is loaded, fold linked Strata floors into
    /// BedCheck / GetGuestBeds so upstairs/downstairs guest rooms attract visits.
    /// Guests still spawn on the surface; portal relay already walks them to beds.
    /// </summary>
    [StaticConstructorOnStartup]
    public static class HospitalityGuestRoomSoftCompat
    {
        private const string PackageId = "Orion.Hospitality";

        static HospitalityGuestRoomSoftCompat()
        {
            if (!ModsConfig.IsActive(PackageId))
            {
                return;
            }

            try
            {
                Harmony harmony = new Harmony("azraelgodking.strata.hospitality.guestrooms");
                int patched = 0;
                patched += TryPatch(harmony, "Hospitality.IncidentWorker_VisitorGroup", "BedCheck",
                    nameof(BedCheck_Postfix));
                patched += TryPatch(harmony, "Hospitality.Utilities.BedUtility", "GetGuestBeds",
                    nameof(GetGuestBeds_Postfix));
                if (patched == 0)
                {
                    Log.Warning("[Strata] Hospitality loaded but guest-room soft-compat found no hooks.");
                }
                else
                {
                    StrataLog.Verbose("[Strata] Hospitality guest-room soft-compat: patched "
                        + patched + " hook(s).");
                }
            }
            catch (Exception e)
            {
                Log.Warning("[Strata] Hospitality guest-room soft-compat failed: " + e.Message);
            }
        }

        private static int TryPatch(Harmony harmony, string typeName, string methodName, string postfix)
        {
            Type type = AccessTools.TypeByName(typeName);
            if (type == null)
            {
                return 0;
            }
            MethodInfo method = AccessTools.Method(type, methodName);
            if (method == null)
            {
                return 0;
            }
            MethodInfo postfixMethod = AccessTools.Method(typeof(HospitalityGuestRoomSoftCompat), postfix);
            harmony.Patch(method, postfix: new HarmonyMethod(postfixMethod));
            return 1;
        }

        // bool BedCheck(Map map)
        public static void BedCheck_Postfix(Map map, ref bool __result)
        {
            if (__result || map == null || !LevelGraph.AnyLinkFrom(map))
            {
                return;
            }

            List<LevelGraph.LevelLink> links = LevelGraph.ReachableLevels(map);
            for (int i = 0; i < links.Count; i++)
            {
                Map other = links[i].map;
                if (other == null || other == map)
                {
                    continue;
                }
                if (HasAnyGuestBed(other))
                {
                    __result = true;
                    return;
                }
            }
        }

        // IEnumerable GetGuestBeds(Map map, Area area) — concrete T is Hospitality.Building_GuestBed
        public static void GetGuestBeds_Postfix(Map map, Area area, ref object __result)
        {
            if (map == null || !LevelGraph.AnyLinkFrom(map))
            {
                return;
            }

            List<object> extras = null;
            List<LevelGraph.LevelLink> links = LevelGraph.ReachableLevels(map);
            MethodInfo isGuestBed = AccessTools.Method(
                AccessTools.TypeByName("Hospitality.Utilities.BedUtility"),
                "IsGuestBed",
                new[] { typeof(Building_Bed) });

            for (int i = 0; i < links.Count; i++)
            {
                Map other = links[i].map;
                if (other == null || other == map)
                {
                    continue;
                }

                foreach (Thing thing in other.listerThings.ThingsInGroup(ThingRequestGroup.Bed))
                {
                    if (thing is not Building_Bed bed)
                    {
                        continue;
                    }
                    bool guest = false;
                    if (isGuestBed != null)
                    {
                        try
                        {
                            guest = (bool)isGuestBed.Invoke(null, new object[] { bed });
                        }
                        catch
                        {
                            guest = bed.GetType().Name.IndexOf("Guest", StringComparison.Ordinal) >= 0;
                        }
                    }
                    else
                    {
                        guest = bed.GetType().Name.IndexOf("Guest", StringComparison.Ordinal) >= 0;
                    }
                    if (!guest)
                    {
                        continue;
                    }
                    extras ??= new List<object>();
                    extras.Add(bed);
                }
            }

            if (extras == null || extras.Count == 0)
            {
                return;
            }

            __result = Concat(__result as IEnumerable, extras);
        }

        private static bool HasAnyGuestBed(Map map)
        {
            MethodInfo isGuestBed = AccessTools.Method(
                AccessTools.TypeByName("Hospitality.Utilities.BedUtility"),
                "IsGuestBed",
                new[] { typeof(Building_Bed) });
            foreach (Thing thing in map.listerThings.ThingsInGroup(ThingRequestGroup.Bed))
            {
                if (thing is not Building_Bed bed)
                {
                    continue;
                }
                if (isGuestBed != null)
                {
                    try
                    {
                        if ((bool)isGuestBed.Invoke(null, new object[] { bed }))
                        {
                            return true;
                        }
                    }
                    catch
                    {
                        // fall through
                    }
                }
                if (bed.GetType().Name.IndexOf("Guest", StringComparison.Ordinal) >= 0)
                {
                    return true;
                }
            }
            return false;
        }

        private static IEnumerable Concat(IEnumerable first, List<object> extras)
        {
            if (first != null)
            {
                foreach (object o in first)
                {
                    yield return o;
                }
            }
            for (int i = 0; i < extras.Count; i++)
            {
                yield return extras[i];
            }
        }
    }
}
