using System.Collections.Generic;
using RimWorld;
using UnityEngine;
using Verse;

namespace Strata
{
    // The top half of a stairwell pair. Vanilla MapPortal handles pocket map
    // generation (via def.portal), the enter job, and the view-level gizmo.
    // The stairwell also exchanges temperature between the rooms at its top
    // and bottom: heat rises fast, cold seeps down slowly.
    public class Building_StairsDown : MapPortal
    {
        private const int ExchangeInterval = 250;

        // Fraction of the temperature difference moved per pulse. Warm air
        // below convects upward quickly; a warmer top layer stratifies and
        // only bleeds down slowly.
        private const float ConvectionRate = 0.12f;

        private const float StratifyRate = 0.02f;

        private const float MinDelta = 0.25f;

        private const float ShaftPowerCapWatts = 2000f;

        public override bool AutoDraftOnEnter => false;

        public override string EnterString => "Go downstairs";

        public override string EnteringString => "going downstairs";

        public bool Sealed => GetComp<CompStairwellControl>()?.Sealed ?? false;

        public override bool IsEnterable(out string reason)
        {
            if (Sealed)
            {
                reason = "The stairwell is sealed.";
                return false;
            }
            return base.IsEnterable(out reason);
        }

        public override void OnEntered(Pawn pawn)
        {
            base.OnEntered(pawn);
            StrataPortalUtility.TransferHaulDesignation(this, pawn);
        }

        public override AcceptanceReport DeconstructibleBy(Faction faction)
        {
            if (PocketMapExists && PocketMap.mapPawns.AnyPawnBlockingMapRemoval)
            {
                return "Someone is still on the level below.";
            }
            return base.DeconstructibleBy(faction);
        }

        public override void Destroy(DestroyMode mode = DestroyMode.Vanish)
        {
            Map level = PocketMapExists ? PocketMap : null;
            base.Destroy(mode);
            // Collapse an empty level with the stairs; a level with pawns on it
            // stays alive so they can still climb out via the stairwell below,
            // and a shared level stays alive while any other entrance links in.
            if (level != null && Find.Maps.Contains(level)
                && !level.mapPawns.AnyPawnBlockingMapRemoval
                && !AnyEntranceTo(level))
            {
                PocketMapUtility.DestroyPocketMap(level);
            }
        }

        private static bool AnyEntranceTo(Map level)
        {
            foreach (Map map in Find.Maps)
            {
                if (map == level)
                {
                    continue;
                }
                foreach (Thing thing in map.listerThings.ThingsInGroup(ThingRequestGroup.MapPortal))
                {
                    if (thing is MapPortal portal && !(portal is PocketMapExit)
                        && portal.Spawned && portal.PocketMap == level)
                    {
                        return true;
                    }
                }
            }
            return false;
        }

        // A second stairwell or elevator dug on this level joins the SAME level
        // below instead of opening a parallel pocket dimension: adopt a sibling
        // portal's map and carve our own landing into it, roughly under where
        // this portal stands.
        protected override Map GeneratePocketMapInt()
        {
            Map existing = ExistingLevelBelow();
            if (existing != null)
            {
                IntVec3 landing = FindLandingCell(existing);
                if (landing.IsValid)
                {
                    // currentlyGeneratingPortal == this here (set by
                    // MapPortal.GeneratePocketMap), so the landing wires
                    // itself to this entrance exactly like during map gen.
                    StrataPortalUtility.SpawnLanding(def.portal.exitDef, landing, existing);
                    Messages.Message("Broke through to the existing level below.", this, MessageTypeDefOf.PositiveEvent);
                    return existing;
                }
            }
            return base.GeneratePocketMapInt();
        }

        private Map ExistingLevelBelow()
        {
            foreach (Thing thing in Map.listerThings.ThingsInGroup(ThingRequestGroup.MapPortal))
            {
                if (thing != this && thing is Building_StairsDown other
                    && other.Spawned && other.PocketMapExists)
                {
                    return other.PocketMap;
                }
            }
            return null;
        }

        private IntVec3 FindLandingCell(Map level)
        {
            IntVec3 target = ProportionalCell(Position, Map, level);
            if (LandingSpotClear(target, level))
            {
                return target;
            }
            foreach (IntVec3 cell in GenRadial.RadialCellsAround(target, 25f, useCenter: false))
            {
                if (LandingSpotClear(cell, level))
                {
                    return cell;
                }
            }
            return IntVec3.Invalid;
        }

        // The landing goes roughly under the new portal: same relative position
        // on the (possibly smaller) level map.
        private static IntVec3 ProportionalCell(IntVec3 pos, Map from, Map to)
        {
            int x = Mathf.RoundToInt((float)pos.x / from.Size.x * to.Size.x);
            int z = Mathf.RoundToInt((float)pos.z / from.Size.z * to.Size.z);
            return new IntVec3(x, 0, z);
        }

        // Natural rock gets carved away, but never break through into another
        // portal or a player-built structure.
        private bool LandingSpotClear(IntVec3 cell, Map level)
        {
            if (!cell.InBounds(level) || cell.DistanceToEdge(level) < 8)
            {
                return false;
            }
            foreach (IntVec3 c in GenAdj.OccupiedRect(cell, Rot4.North, def.portal.exitDef.size))
            {
                if (!c.InBounds(level))
                {
                    return false;
                }
                List<Thing> things = c.GetThingList(level);
                for (int i = 0; i < things.Count; i++)
                {
                    Thing thing = things[i];
                    if (thing is MapPortal)
                    {
                        return false;
                    }
                    if (thing.def.IsEdifice() && thing.def.building?.isNaturalRock != true)
                    {
                        return false;
                    }
                }
            }
            return true;
        }

        protected override void Tick()
        {
            base.Tick();
            if (!Spawned)
            {
                return;
            }
            if (this.IsHashIntervalTick(ExchangeInterval))
            {
                ExchangeTemperature();
            }
            if (this.IsHashIntervalTick(60) && PocketMapExists && exit != null && exit.Spawned)
            {
                TieShaftPower();
            }
        }

        private void TieShaftPower()
        {
            CompPowerShaft top = GetComp<CompPowerShaft>();
            CompPowerShaft bottom = exit.GetComp<CompPowerShaft>();
            if (top != null && bottom != null)
            {
                top.DriveTie(bottom, ShaftPowerCapWatts);
            }
        }

        private void ExchangeTemperature()
        {
            if (!PocketMapExists || exit == null || !exit.Spawned || Sealed)
            {
                return;
            }
            Room top = Position.GetRoom(Map);
            Room bottom = exit.Position.GetRoom(exit.Map);
            if (top == null || bottom == null)
            {
                return;
            }
            bool topIsReservoir = top.UsesOutdoorTemperature;
            bool bottomIsReservoir = bottom.UsesOutdoorTemperature;
            if (topIsReservoir && bottomIsReservoir)
            {
                return;
            }
            float topTemp = topIsReservoir ? Map.mapTemperature.OutdoorTemp : top.Temperature;
            float bottomTemp = bottomIsReservoir ? exit.Map.mapTemperature.OutdoorTemp : bottom.Temperature;
            float delta = bottomTemp - topTemp;
            if (Mathf.Abs(delta) < MinDelta)
            {
                return;
            }
            // delta > 0: warmer below, heat convects up. delta < 0: warmer
            // above, cold air stays put and heat only creeps down.
            float flow = delta * (delta > 0f ? ConvectionRate : StratifyRate);
            if (topIsReservoir)
            {
                bottom.Temperature -= flow;
                return;
            }
            if (bottomIsReservoir)
            {
                top.Temperature += flow;
                return;
            }
            // Split by room size so the same energy moves both temperatures.
            float totalCells = top.CellCount + bottom.CellCount;
            top.Temperature += flow * (bottom.CellCount / totalCells);
            bottom.Temperature -= flow * (top.CellCount / totalCells);
        }

        public override string GetInspectString()
        {
            string text = base.GetInspectString();
            string state = "Level below: not yet opened";
            if (PocketMapExists)
            {
                state = "Level below: excavated";
                if (exit != null && exit.Spawned)
                {
                    Room bottom = exit.Position.GetRoom(exit.Map);
                    if (bottom != null)
                    {
                        float temp = bottom.UsesOutdoorTemperature
                            ? exit.Map.mapTemperature.OutdoorTemp
                            : bottom.Temperature;
                        state += " (" + temp.ToStringTemperature("F0") + " at the landing)";
                    }
                }
                state += "\n" + SmokeRiseInspectLine();
                state += "\n" + PowerShaftInspectLine();
            }
            return text.NullOrEmpty() ? state : text + "\n" + state;
        }

        private string SmokeRiseInspectLine()
        {
            if (Sealed)
            {
                return "Smoke shaft: sealed";
            }
            return "Smoke shaft: fumes rise to the level above";
        }

        private string PowerShaftInspectLine()
        {
            return "Power shaft: ties both levels' grids (wire each floor into the stairwell; keep batteries on each level)";
        }
    }
}
