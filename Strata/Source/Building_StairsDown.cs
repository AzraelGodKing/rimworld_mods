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

        // How far a landing may sit from the spot directly below the shaft
        // before we treat it as misaligned (matches shaft conduit tolerance).
        private const float LandingAlignRadius = 4.5f;

        public override void SpawnSetup(Map map, bool respawningAfterLoad)
        {
            base.SpawnSetup(map, respawningAfterLoad);
            if (respawningAfterLoad && PocketMapExists)
            {
                TryRealignLandingIfNeeded();
            }
        }

        internal void TryOpenLevelAfterBuilt()
        {
            if (!Spawned || PocketMapExists)
            {
                return;
            }
            OpenLevelBelow();
            if (def.defName == "Strata_DigDownShaft")
            {
                LinkNearbyLanding();
            }
        }

        private void LinkNearbyLanding()
        {
            Map map = Map;
            if (map == null)
            {
                return;
            }
            const float radiusSq = 8f * 8f;
            Building_StairsUp best = null;
            float bestDist = float.MaxValue;
            foreach (Thing thing in map.listerThings.ThingsInGroup(ThingRequestGroup.MapPortal))
            {
                if (thing is not Building_StairsUp landing || !landing.Spawned)
                {
                    continue;
                }
                float dist = landing.Position.DistanceToSquared(Position);
                if (dist <= radiusSq && dist < bestDist)
                {
                    best = landing;
                    bestDist = dist;
                }
            }
            if (best == null)
            {
                return;
            }
            best.SetDownEntrance(this);
            StairwellPowerUtility.MaintainVerticalTie(best);
        }

        public override bool AutoDraftOnEnter => false;

        public override string EnterString => "Go downstairs";

        public override string EnteringString => "going downstairs";

        public bool Sealed => GetComp<CompStairwellControl>()?.Sealed ?? false;

        // Ancient colony stairwells open B1 without digging-down research.
        protected virtual bool BypassFirstLevelResearch => false;

        public override bool IsEnterable(out string reason)
        {
            if (Sealed)
            {
                reason = "The stairwell is sealed.";
                return false;
            }
            if (!PocketMapExists && !BypassFirstLevelResearch && !CanOpenPortalLevel(out reason))
            {
                return false;
            }
            return base.IsEnterable(out reason);
        }

        // Dig-down stairwells gate on excavation research; tower stairwells override.
        protected virtual bool CanOpenPortalLevel(out string reason)
        {
            return LevelExcavationUtility.CanOpenNewLevelBelow(Map, out reason, this);
        }

        public override void OnEntered(Pawn pawn)
        {
            base.OnEntered(pawn);
            StrataPortalUtility.TransferHaulDesignation(this, pawn);
            MapComponent_RaidPursuit.NotifyPortalArrival(pawn, pawn.MapHeld);
        }

        public override AcceptanceReport DeconstructibleBy(Faction faction)
        {
            if (PocketMapExists && PocketMap.mapPawns.AnyPawnBlockingMapRemoval)
            {
                return OccupiedOtherLevelMessage();
            }
            return base.DeconstructibleBy(faction);
        }

        protected virtual string OccupiedOtherLevelMessage()
        {
            return "Someone is still on the level below.";
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
        //
        // A NEW level matches the footprint of the map it sits under (instead
        // of the def's fixed pocketMapSize), so landings, shaft conduits, and
        // the level hotkeys stack exactly 1:1 beneath the level above. On big
        // maps each level costs what another map of that size costs; levels
        // opened before this change keep their old size and the proportional
        // alignment still handles them.
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
            if (!BypassFirstLevelResearch && !LevelExcavationUtility.CanOpenNewLevelBelow(Map, out string reason))
            {
                Messages.Message(reason, this, MessageTypeDefOf.RejectInput, historical: false);
                return null;
            }
            return PocketMapUtility.GeneratePocketMap(
                new IntVec3(Map.Size.x, 1, Map.Size.z),
                def.portal.pocketMapGenerator, null, Map);
        }

        private Map ExistingLevelBelow()
        {
            foreach (Thing thing in Map.listerThings.ThingsInGroup(ThingRequestGroup.MapPortal))
            {
                // Tower / gravship shafts keep their own stacks — never join those.
                if (thing == this || thing is Building_StairsBuildUp
                    || thing is IStrataGravshipPortal
                    || thing is not Building_StairsDown other
                    || !other.Spawned || !other.PocketMapExists)
                {
                    continue;
                }
                Map pocket = other.PocketMap;
                if (pocket != null && !StrataMapUtility.IsUpperLevel(pocket))
                {
                    return pocket;
                }
            }
            return null;
        }

        internal IntVec3 FindLandingCell(Map level)
        {
            IntVec3 target = StrataMapUtility.VerticalAlign(Position, Map, level);
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

        // One-time fix for saves where the landing spawned at map center instead
        // of beneath this shaft. Keeps a good landing; never re-runs once aligned.
        internal void TryRealignLandingIfNeeded()
        {
            if (exit == null || !exit.Spawned || !PocketMapExists)
            {
                return;
            }
            Map level = PocketMap;
            float alignRadiusSq = LandingAlignRadius * LandingAlignRadius + 0.1f;
            IntVec3 aligned = StrataMapUtility.ProportionalCell(Position, Map, level);
            if (aligned.DistanceToSquared(exit.Position) <= alignRadiusSq)
            {
                return;
            }
            IntVec3 target = FindLandingCell(level);
            if (!target.IsValid || target == exit.Position)
            {
                return;
            }
            ThingDef exitDef = def.portal?.exitDef;
            if (exitDef == null)
            {
                return;
            }
            exit.DeSpawn();
            PocketMapUtility.currentlyGeneratingPortal = this;
            try
            {
                StrataPortalUtility.SpawnLanding(exitDef, target, level);
            }
            finally
            {
                PocketMapUtility.currentlyGeneratingPortal = null;
            }
            Messages.Message("Stairwell landing repositioned beneath the shaft above.", this, MessageTypeDefOf.NeutralEvent);
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
            string state = LevelInspectState();
            return text.NullOrEmpty() ? state : text + "\n" + state;
        }

        protected virtual string LevelInspectState()
        {
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
            else if (StrataMapUtility.IsUnderground(Map))
            {
                state += "\nSelect Dig down to designate a dig shaft; colonists must finish carving it before the level below opens.";
            }
            return state;
        }

        // Force pocket-map generation (used by the underground Dig down gizmo).
        // Virtual so tower stairwells can open an upper level with their own gates.
        public virtual void OpenLevelBelow()
        {
            if (PocketMapExists)
            {
                return;
            }
            if (!BypassFirstLevelResearch && !LevelExcavationUtility.CanOpenNewLevelBelow(Map, out string reason))
            {
                if (!reason.NullOrEmpty())
                {
                    Messages.Message(reason, this, MessageTypeDefOf.RejectInput, historical: false);
                }
                return;
            }
            _ = PocketMap;
        }

        public override IEnumerable<Gizmo> GetGizmos()
        {
            foreach (Gizmo gizmo in base.GetGizmos())
            {
                yield return gizmo;
            }
            foreach (Gizmo gizmo in ExtraGizmos())
            {
                yield return gizmo;
            }
        }

        // Dig-down shaft gizmo for unfinished underground portals; tower stairs omit this.
        protected virtual IEnumerable<Gizmo> ExtraGizmos()
        {
            if (!StrataMapUtility.IsUnderground(Map) || PocketMapExists)
            {
                yield break;
            }
            StairwellDigUtility.CanDigDownFromEntrance(this, out string reason);
            yield return new Command_Action
            {
                defaultLabel = "Dig down",
                defaultDesc = "Break through to the level below once this stairwell is fully built, or use it if construction finished before research was available.",
                icon = ContentFinder<Texture2D>.Get("UI/Designators/Mine", reportFailure: false),
                action = () =>
                {
                    if (StairwellDigUtility.TryDigDownFromEntrance(this, out string message))
                    {
                        return;
                    }
                    if (!message.NullOrEmpty())
                    {
                        Messages.Message(message, this, MessageTypeDefOf.RejectInput, historical: false);
                    }
                },
                Disabled = !reason.NullOrEmpty(),
                disabledReason = reason,
            };
        }

        private string SmokeRiseInspectLine()
        {
            if (Sealed)
            {
                return "Smoke shaft: sealed";
            }
            return "Smoke shaft: fumes rise to the level above";
        }

        protected string PowerShaftInspectLine()
        {
            return "Power shaft: ties both levels' grids (wire each floor into the stairwell; keep batteries on each level)";
        }
    }
}
