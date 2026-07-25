using System.Collections.Generic;
using System.Linq;
using RimWorld;
using Verse;

namespace Strata
{
    // Surface (or A-n) tower stairwell: a MapPortal that opens an empty outdoor
    // upper level above this map. Joins a sibling tower's existing A-level when
    // one is already open — never the underground pocket under StairsDown.
    public class Building_StairsBuildUp : Building_StairsDown
    {
        public override string EnterString => "Go upstairs";

        public override string EnteringString => "going upstairs";

        protected override bool CanOpenPortalLevel(out string reason)
        {
            return LevelBuildUpUtility.CanOpenNewLevelAbove(Map, out reason, this);
        }

        protected override string OccupiedOtherLevelMessage()
        {
            return "Strata_SomeoneAbove".Translate();
        }

        public override void OpenLevelBelow()
        {
            if (PocketMapExists || StrataPocketMapOpen.IsGenerating(this))
            {
                return;
            }
            if (!LevelBuildUpUtility.CanOpenNewLevelAbove(Map, out string reason, this))
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

        protected override Map GeneratePocketMapInt()
        {
            Map existing = ExistingUpperLevel();
            if (existing != null)
            {
                IntVec3 landing = FindLandingCell(existing);
                if (landing.IsValid)
                {
                    // Shaft plaza is always buildable even if the floor below is unroofed.
                    UpperDeckUtility.EnsurePlaza(existing, landing);
                    StrataPortalUtility.SpawnLanding(def.portal.exitDef, landing, existing);
                    Messages.Message("Strata_StairsConnectedAbove".Translate(), this, MessageTypeDefOf.PositiveEvent);
                    return existing;
                }
            }
            if (!LevelBuildUpUtility.CanOpenNewLevelAbove(Map, out string reason, this))
            {
                Messages.Message(reason, this, MessageTypeDefOf.RejectInput, historical: false);
                return null;
            }
            return PocketMapUtility.GeneratePocketMap(
                new IntVec3(Map.Size.x, 1, Map.Size.z),
                def.portal.pocketMapGenerator, null, Map);
        }

        private Map ExistingUpperLevel()
        {
            foreach (Thing thing in Map.listerThings.ThingsInGroup(ThingRequestGroup.MapPortal))
            {
                if (thing != this && thing is Building_StairsBuildUp other
                    && other is not IStrataGravshipPortal
                    && other.Spawned && other.PocketMapExists)
                {
                    return other.PocketMap;
                }
            }
            return null;
        }

        protected override string LevelInspectState()
        {
            string state = "Level above: not yet opened";
            if (PocketMapExists)
            {
                state = "Level above: roof deck";
                if (exit != null && exit.Spawned)
                {
                    Room above = exit.Position.GetRoom(exit.Map);
                    if (above != null)
                    {
                        float temp = above.UsesOutdoorTemperature
                            ? exit.Map.mapTemperature.OutdoorTemp
                            : above.Temperature;
                        state += " (" + temp.ToStringTemperature("F0") + " at the landing)";
                    }
                }
                state += "\nBuildable only where this floor is roofed (plus the shaft plaza)";
                state += "\nSmoke shaft: fumes rise into the floor above";
                state += "\n" + PowerShaftInspectLine();
            }
            return state;
        }

        protected override IEnumerable<Gizmo> ExtraGizmos()
        {
            return Enumerable.Empty<Gizmo>();
        }
    }
}
