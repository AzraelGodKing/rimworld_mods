using System.Collections.Generic;
using RimWorld;
using Verse;

namespace Strata
{
    // Powered ore skip — links to a partner hoist on the level above or below
    // (like shaft conduit) and shuttles mined chunks between levels.
    public class Building_OreHoist : Building
    {
        private const int PartnerCheckInterval = 250;

        private const int ShaftSearchRadius = 5;

        private const float LandingSearchRadius = 4.5f;

        private Building_OreHoist partner;

        private Building_OreHoist parentAbove;

        private int partnerId = -1;

        private int parentAboveId = -1;

        public bool IsAutoSpawned => parentAbove != null || parentAboveId >= 0;

        public Building_OreHoist Partner => partner;

        public override void ExposeData()
        {
            base.ExposeData();
            Scribe_References.Look(ref partner, "strataHoistPartner");
            Scribe_References.Look(ref parentAbove, "strataHoistParentAbove");
            if (Scribe.mode == LoadSaveMode.Saving)
            {
                partnerId = partner?.thingIDNumber ?? -1;
                parentAboveId = parentAbove?.thingIDNumber ?? -1;
            }
            Scribe_Values.Look(ref partnerId, "strataHoistPartnerId", -1);
            Scribe_Values.Look(ref parentAboveId, "strataHoistParentAboveId", -1);
        }

        public override void SpawnSetup(Map map, bool respawningAfterLoad)
        {
            base.SpawnSetup(map, respawningAfterLoad);
            if (respawningAfterLoad)
            {
                ReconcilePartnerLinks();
            }
            else if (!IsAutoSpawned)
            {
                EnsurePartner();
            }
        }

        public override void Destroy(DestroyMode mode = DestroyMode.Vanish)
        {
            Building_OreHoist child = partner;
            partner = null;
            partnerId = -1;
            base.Destroy(mode);
            if (child != null && !child.Destroyed && child.parentAbove == this)
            {
                child.Destroy(DestroyMode.Vanish);
            }
        }

        public override AcceptanceReport DeconstructibleBy(Faction faction)
        {
            if (IsAutoSpawned && parentAbove is { Spawned: true })
            {
                return "Strata_OreHoistDriven".Translate();
            }
            return base.DeconstructibleBy(faction);
        }

        protected override void Tick()
        {
            base.Tick();
            if (!Spawned || IsAutoSpawned)
            {
                return;
            }
            if (this.IsHashIntervalTick(PartnerCheckInterval))
            {
                if (!PartnerValid())
                {
                    ReconcilePartnerLinks();
                }
                EnsurePartner();
            }
        }

        internal void ReconcilePartnerLinks()
        {
            if (!Spawned)
            {
                return;
            }
            if (parentAboveId >= 0 && (parentAbove == null || parentAbove.Destroyed))
            {
                parentAbove = FindHoistById(parentAboveId);
            }
            if (parentAbove != null && !parentAbove.Destroyed)
            {
                parentAbove.partner = this;
                parentAbove.partnerId = thingIDNumber;
                return;
            }
            if (partnerId >= 0 && (partner == null || partner.Destroyed))
            {
                partner = FindHoistById(partnerId);
            }
            if (partner == null || partner.Destroyed)
            {
                partner = FindLinkedPartner();
            }
            if (partner != null && !partner.Destroyed)
            {
                LinkPartners(this, partner);
            }
        }

        internal static void ReconcileAllAfterLoad()
        {
            List<Map> maps = Find.Maps;
            for (int m = 0; m < maps.Count; m++)
            {
                List<Thing> things = maps[m].listerThings.AllThings;
                for (int i = 0; i < things.Count; i++)
                {
                    if (things[i] is Building_OreHoist hoist && hoist.Spawned)
                    {
                        hoist.ReconcilePartnerLinks();
                    }
                }
            }
        }

        private static void LinkPartners(Building_OreHoist a, Building_OreHoist b)
        {
            Map upper = StrataDepth.Of(a.Map) <= StrataDepth.Of(b.Map) ? a.Map : b.Map;
            Building_OreHoist top = a.Map == upper ? a : b;
            Building_OreHoist bottom = top == a ? b : a;
            top.partner = bottom;
            top.partnerId = bottom.thingIDNumber;
            bottom.parentAbove = top;
            bottom.parentAboveId = top.thingIDNumber;
        }

        private bool PartnerValid()
        {
            return partner != null && !partner.Destroyed && partner.Spawned;
        }

        private void EnsurePartner()
        {
            if (IsAutoSpawned)
            {
                return;
            }
            MapPortal portal = NearestPortal();
            if (portal == null)
            {
                return;
            }
            Map other = LevelGraph.OtherMapSafe(portal);
            if (other == null)
            {
                return;
            }
            IntVec3 aligned = StrataMapUtility.ProportionalCell(Position, Map, other);
            if (PartnerValid() && partner.Map == other
                && aligned.InHorDistOf(partner.Position, LandingSearchRadius))
            {
                return;
            }
            if (PartnerValid() && partner.parentAbove == this)
            {
                partner.Destroy(DestroyMode.Vanish);
                partner = null;
                partnerId = -1;
            }
            partner = FindAdoptable(other, aligned) ?? SpawnPartner(other, aligned);
            if (partner != null)
            {
                LinkPartners(this, partner);
            }
        }

        private MapPortal NearestPortal()
        {
            MapPortal best = null;
            float bestDist = ShaftSearchRadius * ShaftSearchRadius + 0.1f;
            foreach (Thing thing in Map.listerThings.ThingsInGroup(ThingRequestGroup.MapPortal))
            {
                if (thing is not MapPortal portal || LevelGraph.OtherMapSafe(portal) == null)
                {
                    continue;
                }
                float d = Position.DistanceToSquared(portal.Position);
                if (d < bestDist)
                {
                    bestDist = d;
                    best = portal;
                }
            }
            return best;
        }

        private Building_OreHoist FindAdoptable(Map other, IntVec3 aligned)
        {
            Building_OreHoist best = null;
            float bestDist = LandingSearchRadius * LandingSearchRadius + 0.1f;
            foreach (Building building in other.listerBuildings.AllBuildingsColonistOfDef(def))
            {
                if (building is not Building_OreHoist hoist
                    || (hoist.parentAbove != null && hoist.parentAbove != this))
                {
                    continue;
                }
                float d = aligned.DistanceToSquared(building.Position);
                if (d < bestDist)
                {
                    bestDist = d;
                    best = hoist;
                }
            }
            return best;
        }

        private Building_OreHoist SpawnPartner(Map other, IntVec3 aligned)
        {
            IntVec3 cell = FindPartnerCell(other, aligned);
            if (!cell.IsValid)
            {
                return null;
            }
            cell.GetFirstMineable(other)?.Destroy(DestroyMode.Vanish);
            var child = (Building_OreHoist)GenSpawn.Spawn(ThingMaker.MakeThing(def), cell, other);
            child.SetFaction(Faction.OfPlayer);
            Messages.Message("Strata_OreHoistExtended".Translate(), child, MessageTypeDefOf.PositiveEvent);
            return child;
        }

        private static IntVec3 FindPartnerCell(Map map, IntVec3 aligned)
        {
            if (TryCell(map, aligned, carveRock: true, out IntVec3 exact))
            {
                return exact;
            }
            foreach (IntVec3 cell in GenRadial.RadialCellsAround(aligned, LandingSearchRadius, useCenter: false))
            {
                if (TryCell(map, cell, carveRock: false, out IntVec3 open))
                {
                    return open;
                }
            }
            return IntVec3.Invalid;
        }

        private static bool TryCell(Map map, IntVec3 cell, bool carveRock, out IntVec3 result)
        {
            result = IntVec3.Invalid;
            if (!cell.InBounds(map) || cell.DistanceToEdge(map) < 2)
            {
                return false;
            }
            Building edifice = cell.GetEdifice(map);
            if (edifice is Building_OreHoist { IsAutoSpawned: true })
            {
                return false;
            }
            if (edifice == null && cell.Standable(map))
            {
                result = cell;
                return true;
            }
            if (carveRock && edifice?.def.building?.isNaturalRock == true)
            {
                edifice.Destroy(DestroyMode.Vanish);
                if (cell.Standable(map))
                {
                    result = cell;
                    return true;
                }
            }
            return false;
        }

        private Building_OreHoist FindLinkedPartner()
        {
            foreach (Map map in Find.Maps)
            {
                foreach (Building building in map.listerBuildings.AllBuildingsColonistOfDef(def))
                {
                    if (building is Building_OreHoist hoist
                        && (hoist.parentAboveId == thingIDNumber || hoist.parentAbove == this))
                    {
                        return hoist;
                    }
                }
            }
            return null;
        }

        private static Building_OreHoist FindHoistById(int thingId)
        {
            if (thingId < 0)
            {
                return null;
            }
            List<Map> maps = Find.Maps;
            for (int m = 0; m < maps.Count; m++)
            {
                List<Thing> things = maps[m].listerThings.AllThings;
                for (int i = 0; i < things.Count; i++)
                {
                    if (things[i].thingIDNumber == thingId && things[i] is Building_OreHoist hoist)
                    {
                        return hoist;
                    }
                }
            }
            return null;
        }

        public override string GetInspectString()
        {
            string text = base.GetInspectString();
            string state = IsAutoSpawned
                ? "Skip: driven by the hoist on the level above"
                : PartnerValid()
                    ? "Skip: linked to partner hoist on another level"
                    : NearestPortal() == null
                        ? "Skip: build within " + ShaftSearchRadius + " tiles of a stairwell or elevator"
                        : "Skip: waiting for a partner on the linked level";
            return text.NullOrEmpty() ? state : text + "\n" + state;
        }
    }
}
