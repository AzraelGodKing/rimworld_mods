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
            // stays alive so they can still climb out via the stairwell below.
            if (level != null && Find.Maps.Contains(level) && !level.mapPawns.AnyPawnBlockingMapRemoval)
            {
                PocketMapUtility.DestroyPocketMap(level);
            }
        }

        protected override void Tick()
        {
            base.Tick();
            if (this.IsHashIntervalTick(ExchangeInterval) && Spawned)
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
            }
            return text.NullOrEmpty() ? state : text + "\n" + state;
        }
    }
}
