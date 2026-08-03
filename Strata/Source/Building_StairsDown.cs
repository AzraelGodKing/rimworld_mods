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
                if (thing is not Building_StairsUp landing || !landing.Spawned
                    || !StrataGravshipUtility.SameShaftFamily(this, landing))
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
            // Rock fill continues after PocketMapExists — keep Enter blocked.
            if (StrataPocketMapOpen.IsGenerating(this))
            {
                reason = "Strata_OpeningLevel".Translate();
                return false;
            }
            if (!PocketMapExists)
            {
                if (!BypassFirstLevelResearch && !CanOpenPortalLevel(out reason))
                {
                    return false;
                }
                if (StrataPocketMapOpen.HasFailed(this))
                {
                    reason = "Strata_OpenLevelFailed".Translate();
                    return false;
                }
                // First descent (ancient / excavated) generates a full-size rock
                // map — do it as a LongEvent so large mod lists don't hard-freeze.
                StrataPocketMapOpen.TryBeginOpen(this);
                reason = "Strata_OpeningLevel".Translate();
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
            StrataPortalUtility.NotifyHaulArrival(pawn);
            DraftedPortalPathing.NotifyPortalArrival(pawn);
            PortalRelayChain.NotifyPortalArrival(pawn);
            MapComponent_RaidPursuit.NotifyPortalArrival(pawn, pawn.MapHeld);
        }

        public override AcceptanceReport DeconstructibleBy(Faction faction)
        {
            // Broader than vanilla AnyPawnBlockingMapRemoval: downed colonists,
            // mechs, prisoners, and player animals must also block tear-down.
            if (PocketMapExists && StrataPortalUtility.LinkedLevelHasColonyPresence(PocketMap))
            {
                return OccupiedOtherLevelMessage();
            }
            // Dig shafts / some elevators set building.deconstructible=false so
            // base always rejects. Empty or unlinked shafts must still come down.
            if (def?.building != null && !def.building.IsDeconstructible)
            {
                if (!PocketMapExists
                    || PocketMap == null
                    || PocketMap.Disposed
                    || !Find.Maps.Contains(PocketMap)
                    || !ColonyBedUtility.MapsLinked(Map, PocketMap))
                {
                    return true;
                }
                if (!StrataPortalUtility.LinkedLevelHasColonyPresence(PocketMap))
                {
                    return true;
                }
            }
            return base.DeconstructibleBy(faction);
        }

        protected virtual string OccupiedOtherLevelMessage()
        {
            return "Strata_SomeoneBelow".Translate();
        }

        public override void Destroy(DestroyMode mode = DestroyMode.Vanish)
        {
            Map level = PocketMapExists ? PocketMap : null;
            // destroyable=false: need the non-destroyable allowance or player
            // deconstruct / intentional ForceDestroy never removes us.
            // PortalMove lifts DeSpawn immunity so the shaft actually leaves the grid.
            bool openedMove = false;
            if (!StrataPortalUtility.PortalMoveInProgress)
            {
                StrataPortalUtility.BeginPortalMove();
                openedMove = true;
            }
            bool prev = Thing.allowDestroyNonDestroyable;
            Thing.allowDestroyNonDestroyable = true;
            try
            {
                base.Destroy(mode);
            }
            finally
            {
                Thing.allowDestroyNonDestroyable = prev;
                if (openedMove)
                {
                    StrataPortalUtility.EndPortalMove();
                }
            }
            // Collapse an empty level with the surface/host shaft only. A level
            // with colony presence stays alive so they can climb out; a shared
            // level stays while any other entrance still links in.
            if (level != null && Find.Maps.Contains(level)
                && !StrataPortalUtility.LinkedLevelHasColonyPresence(level)
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
                    Messages.Message("Strata_StairsConnectedBelow".Translate(), this, MessageTypeDefOf.PositiveEvent);
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
                // Defense in depth: never join a gravship underdeck even if the
                // owning shaft wasn't tagged as IStrataGravshipPortal.
                if (pocket != null && !StrataMapUtility.IsUpperLevel(pocket)
                    && !StrataGravshipUtility.IsGravshipLinkedLevel(pocket))
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
            // Gravship shafts are strictly 1:1 with their pocket — proportional
            // realign on a different-size host map would drag the landing off the
            // ship footprint. Snap to the exact shaft cell instead.
            if (this is IStrataGravshipPortal)
            {
                StrataGravshipPortalTravel.SnapLandingUnderShaft(this);
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
            PocketMapUtility.currentlyGeneratingPortal = this;
            try
            {
                exit.DeSpawn();
                StrataPortalUtility.SpawnLanding(exitDef, target, level);
            }
            finally
            {
                PocketMapUtility.currentlyGeneratingPortal = null;
            }
            Messages.Message("Strata_LandingRepositioned".Translate(), this, MessageTypeDefOf.NeutralEvent);
        }

        // Natural rock gets carved away, but never break through into another
        // portal or a player-built structure.
        private bool LandingSpotClear(IntVec3 cell, Map level)
        {
            if (!cell.InBounds(level) || cell.DistanceToEdge(level) < 8)
            {
                return false;
            }
            foreach (IntVec3 c in GenAdj.OccupiedRect(cell, Rotation, def.portal.exitDef.size))
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
            // Drive from the host side so vacant underdecks (MapPreTick throttled)
            // still get a live shaft tie every tick.
            if (exit is Building_StairsUp landing && landing.Spawned)
            {
                StairwellPowerUtility.MaintainVerticalTie(landing);
            }
            if (this.IsHashIntervalTick(ExchangeInterval))
            {
                ExchangeTemperature();
                ExchangeGravshipAtmosphere();
            }
        }

        /// <summary>Open gravship shafts act as an O₂ umbilical (VGE-style life support bridge).</summary>
        private void ExchangeGravshipAtmosphere()
        {
            if (!PocketMapExists || exit == null || !exit.Spawned || Sealed)
            {
                return;
            }
            if (this is not IStrataGravshipPortal)
            {
                return;
            }
            StrataGravshipLifeSupport.ExchangeAtmosphereAcrossShaft(Map, Position, exit.Map, exit.Position);
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
            string state = "Strata_StairwellNotOpen".Translate();
            if (PocketMapExists)
            {
                state = "Strata_StairwellExcavated".Translate();
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
                state += "\n" + "Strata_StairwellDigHint".Translate();
            }
            return state;
        }

        // Force pocket-map generation (used by the underground Dig down gizmo).
        // Virtual so tower stairwells can open an upper level with their own gates.
        public virtual void OpenLevelBelow()
        {
            if (PocketMapExists || StrataPocketMapOpen.IsGenerating(this))
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
            StrataPocketMapOpen.ClearFailed(this);
            StrataPocketMapOpen.TryBeginOpen(this);
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
                defaultLabel = "Strata_DigDownLabel".Translate(),
                defaultDesc = "Strata_DigDownFromEntranceDesc".Translate(),
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
                return "Strata_SmokeShaftSealed".Translate();
            }
            return "Strata_SmokeShaftOpen".Translate();
        }

        protected string PowerShaftInspectLine()
        {
            return "Strata_PowerShaft".Translate();
        }
    }
}
