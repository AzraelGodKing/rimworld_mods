using System.Collections.Generic;
using RimWorld;
using RimWorld.Planet;
using UnityEngine;
using Verse;

namespace Strata
{
    // The bottom landing of a stairwell pair, spawned by map generation.
    // Hardened against a destroyed entrance so pawns can never be trapped:
    // if the stairs above are gone, climbing up still works and drops the
    // pawn at the old stairhead.
    public class Building_StairsUp : PocketMapExit
    {
        private IntVec3 lastKnownEntranceCell = IntVec3.Invalid;

        // Dig-down extension on this level (no power comp — this landing is the shaft).
        private Building_StairsDown downEntrance;

        public Building_StairsDown DownEntrance => downEntrance;

        internal void SetDownEntrance(Building_StairsDown entrance)
        {
            downEntrance = entrance;
        }

        private Map SourceMap => (Map?.Parent as PocketMapParent)?.sourceMap;

        private bool EntranceValid => entrance != null && !entrance.Destroyed && entrance.Spawned;

        public override string EnterString => "Go upstairs";

        public override string EnteringString => "going upstairs";

        public override void SpawnSetup(Map map, bool respawningAfterLoad)
        {
            base.SpawnSetup(map, respawningAfterLoad);
            if (Faction != Faction.OfPlayer)
            {
                SetFaction(Faction.OfPlayer);
            }
            if (respawningAfterLoad)
            {
                StairwellDigUtility.ResolveDownEntrance(this);
            }
        }

        public override void ExposeData()
        {
            base.ExposeData();
            if (Scribe.mode == LoadSaveMode.Saving)
            {
                CacheEntranceCell();
            }
            Scribe_Values.Look(ref lastKnownEntranceCell, "strataLastKnownEntranceCell", IntVec3.Invalid);
            Scribe_References.Look(ref downEntrance, "strataDownEntrance");
        }

        private void CacheEntranceCell()
        {
            if (EntranceValid)
            {
                lastKnownEntranceCell = entrance.Position;
            }
        }

        public override Map GetOtherMap()
        {
            if (EntranceValid)
            {
                CacheEntranceCell();
                return entrance.Map;
            }
            return SourceMap;
        }

        public override IntVec3 GetDestinationLocation()
        {
            if (EntranceValid)
            {
                CacheEntranceCell();
                return base.GetDestinationLocation();
            }
            Map above = SourceMap;
            if (above == null)
            {
                return IntVec3.Invalid;
            }
            IntVec3 near = lastKnownEntranceCell.IsValid ? lastKnownEntranceCell : above.Center;
            if (CellFinder.TryFindRandomCellNear(near, above, 5, c => c.Standable(above) && !c.Fogged(above), out IntVec3 cell))
            {
                return cell;
            }
            return CellFinder.RandomCell(above);
        }

        public override bool IsEnterable(out string reason)
        {
            if ((entrance as Building_StairsDown)?.Sealed == true)
            {
                reason = "The stairwell is sealed.";
                return false;
            }
            if (EntranceValid)
            {
                return base.IsEnterable(out reason);
            }
            if (SourceMap == null)
            {
                reason = "The way up has collapsed.";
                return false;
            }
            reason = null;
            return true;
        }

        public override AcceptanceReport DeconstructibleBy(Faction faction)
        {
            return "Strata_OnlyWayUp".Translate();
        }

        public override void OnEntered(Pawn pawn)
        {
            base.OnEntered(pawn);
            StrataPortalUtility.TransferHaulDesignation(this, pawn);
            StrataPortalUtility.TryDeliverConstructionCargo(pawn);
            DraftedPortalPathing.NotifyPortalArrival(pawn);
            MapComponent_RaidPursuit.NotifyPortalArrival(pawn, pawn.MapHeld);
        }

        protected override void Tick()
        {
            base.Tick();
            if ((Find.TickManager.TicksGame + Position.GetHashCode()) % 60 == 0)
            {
                StairwellPowerUtility.MaintainVerticalTie(this);
            }
        }

        public override IEnumerable<Gizmo> GetGizmos()
        {
            foreach (Gizmo gizmo in base.GetGizmos())
            {
                yield return gizmo;
            }
            if (!StrataMapUtility.IsUnderground(Map) || StairwellDigUtility.LandingHasDownwardShaft(this))
            {
                yield break;
            }
            StairwellDigUtility.CanDigDownFromLanding(this, out string reason);
            yield return new Command_Action
            {
                defaultLabel = "Strata_DigDownLabel".Translate(),
                defaultDesc = "Strata_DigDownFromLandingDesc".Translate(),
                icon = ContentFinder<Texture2D>.Get("UI/Designators/Mine", reportFailure: false),
                action = () =>
                {
                    if (StairwellDigUtility.TryDigDownFromLanding(this, out string message))
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

        public override string GetInspectString()
        {
            string text = base.GetInspectString();
            if (!StrataMapUtility.IsUnderground(Map) || StairwellDigUtility.LandingHasDownwardShaft(this))
            {
                return text;
            }
            string hint = "Dig down to designate a dig shaft beside this landing; colonists must finish carving it before the level below opens.";
            return text.NullOrEmpty() ? hint : text + "\n" + hint;
        }
    }
}
