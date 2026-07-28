using System.Collections.Generic;
using RimWorld;
using Verse;
using Verse.AI;

namespace Strata
{
    /// <summary>
    /// Soft-compat for Pick Up And Haul (Mehni). Strata cross-level haul is
    /// single-carry + portal; PUAH packs inventory. On dest arrival, place
    /// inventory haulables into storage when possible so stacks are not stranded
    /// at the stairwell.
    /// </summary>
    public static class StrataPuahSoftCompat
    {
        private static bool? active;

        public static bool Active
        {
            get
            {
                if (active == null)
                {
                    active = ModLister.GetActiveModWithIdentifier("Mehni.PickUpAndHaul", ignorePostfix: true) != null
                        || ModLister.GetActiveModWithIdentifier("Uuugggg.PickUpAndHaul", ignorePostfix: true) != null;
                }
                return active.Value;
            }
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

            // Snapshot — TryDrop mutates the owner.
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

                if (!StoreUtility.TryFindBestBetterStoreCellFor(
                        thing,
                        pawn,
                        pawn.Map,
                        StoragePriority.Unstored,
                        pawn.Faction,
                        out IntVec3 cell))
                {
                    continue;
                }

                if (!inv.Contains(thing))
                {
                    continue;
                }

                if (!inv.TryDrop(thing, cell, pawn.Map, ThingPlaceMode.Direct, out Thing _, null))
                {
                    // Fall back: drop near the pawn so local haulers can finish.
                    inv.TryDrop(thing, pawn.Position, pawn.Map, ThingPlaceMode.Near, out _, null);
                }
            }
        }
    }
}
