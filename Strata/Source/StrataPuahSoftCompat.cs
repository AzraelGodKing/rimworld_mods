using System;
using System.Collections.Generic;
using System.Reflection;
using RimWorld;
using Verse;

namespace Strata
{
    /// <summary>
    /// Soft-compat for Pick Up And Haul and Hauler's Dream. Strata cross-level
    /// haul is single-carry + portal; these mods pack inventory. On dest
    /// arrival, place inventory haulables into storage when possible.
    /// </summary>
    public static class StrataPuahSoftCompat
    {
        private static readonly string[] PackageIds =
        {
            "Mehni.PickUpAndHaul",
            "Uuugggg.PickUpAndHaul",
            "giwaffed.HaulersDream",
        };

        private static bool? active;

        // Both mods tag what they pick up for hauling in a comp of this name
        // with a public GetHashSet(). Read it by reflection: no hard dependency.
        private const string HauledCompName = "CompHauledToInventory";
        private static readonly Dictionary<Type, MethodInfo> hashSetGetters = new Dictionary<Type, MethodInfo>();

        public static bool Active
        {
            get
            {
                if (active == null)
                {
                    active = false;
                    for (int i = 0; i < PackageIds.Length; i++)
                    {
                        if (ModLister.GetActiveModWithIdentifier(PackageIds[i], ignorePostfix: true) != null)
                        {
                            active = true;
                            break;
                        }
                    }
                }
                return active.Value;
            }
        }

        public static void ResetCaches()
        {
            active = null;
            hashSetGetters.Clear();
        }

        // Things the haul mod picked up for delivery. Medicine, drugs and
        // packed food the pawn carries for itself are not in it.
        private static HashSet<Thing> HauledThings(Pawn pawn)
        {
            List<ThingComp> comps = pawn.AllComps;
            if (comps == null)
            {
                return null;
            }

            for (int i = 0; i < comps.Count; i++)
            {
                ThingComp comp = comps[i];
                Type type = comp?.GetType();
                if (type == null || type.Name != HauledCompName)
                {
                    continue;
                }

                if (!hashSetGetters.TryGetValue(type, out MethodInfo getter))
                {
                    getter = type.GetMethod("GetHashSet", BindingFlags.Public | BindingFlags.Instance, null, Type.EmptyTypes, null);
                    if (getter != null && !typeof(HashSet<Thing>).IsAssignableFrom(getter.ReturnType))
                    {
                        getter = null;
                    }
                    hashSetGetters[type] = getter;
                }

                if (getter != null && getter.Invoke(comp, null) is HashSet<Thing> set)
                {
                    return set;
                }
            }

            return null;
        }

        public static void TryDeliverInventory(Pawn pawn)
        {
            if (!Active || pawn?.inventory == null || pawn.Map == null || pawn.Dead || pawn.Downed)
            {
                return;
            }

            ThingOwner<Thing> inv = pawn.inventory.innerContainer;
            if (inv == null || inv.Count == 0)
            {
                return;
            }

            HashSet<Thing> hauled = HauledThings(pawn);
            if (hauled == null || hauled.Count == 0)
            {
                return;
            }

            List<Thing> items = new List<Thing>(inv.Count);
            for (int i = 0; i < inv.Count; i++)
            {
                if (hauled.Contains(inv[i]))
                {
                    items.Add(inv[i]);
                }
            }

            for (int i = 0; i < items.Count; i++)
            {
                Thing thing = items[i];
                if (thing == null || thing.Destroyed)
                {
                    continue;
                }
                if (!thing.def.EverHaulable && !thing.def.alwaysHaulable)
                {
                    continue;
                }

                if (!inv.Contains(thing))
                {
                    continue;
                }

                // Prefer building storage (ASF) when available.
                if (StoreUtility.TryFindBestBetterStorageFor(
                        thing,
                        pawn,
                        pawn.Map,
                        StoragePriority.Unstored,
                        pawn.Faction,
                        out IntVec3 cell,
                        out IHaulDestination dest,
                        needAccurateResult: false))
                {
                    if (dest is Building building && building.Spawned)
                    {
                        // Drop onto the building cell; local haulers / ASF finish.
                        if (inv.TryDrop(thing, building.Position, pawn.Map, ThingPlaceMode.Near, out Thing _, null))
                        {
                            continue;
                        }
                    }
                    else if (cell.IsValid
                        && inv.TryDrop(thing, cell, pawn.Map, ThingPlaceMode.Direct, out Thing _, null))
                    {
                        continue;
                    }
                }

                inv.TryDrop(thing, pawn.Position, pawn.Map, ThingPlaceMode.Near, out _, null);
            }
        }
    }
}
