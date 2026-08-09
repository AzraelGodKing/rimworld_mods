using System.Collections.Generic;
using RimWorld;
using Verse;
using Verse.AI;

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

            List<Thing> items = new List<Thing>(inv.Count);
            for (int i = 0; i < inv.Count; i++)
            {
                items.Add(inv[i]);
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
