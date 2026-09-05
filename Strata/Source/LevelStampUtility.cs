using System.Collections.Generic;
using RimWorld;
using UnityEngine;
using Verse;

namespace Strata
{
    // Copy a finished floor's walls, doors, floors, and stockpiles onto another
    // linked level as blueprints (AZR-61).
    public static class LevelStampUtility
    {
        private static readonly HashSet<string> SkipDefNames = new HashSet<string>
        {
            "Strata_StairsDown", "Strata_StairsUp",
            "Strata_ElevatorDown", "Strata_ElevatorUp",
            "Strata_ElevatorBuildUp", "Strata_ElevatorBuildUpLanding",
            "Strata_StairsBuildUp", "Strata_BuildUpLanding",
            "Strata_DigDownShaft",
            "Strata_AncientColonyStairsDown",
            "Strata_GravshipStairsDown", "Strata_GravshipStairsUp",
            "Strata_GravshipStairsBuildUp", "Strata_GravshipBuildUpLanding",
            "Strata_GravshipElevatorDown", "Strata_GravshipElevatorUp",
            "Strata_GravshipElevatorBuildUp", "Strata_GravshipElevatorBuildUpLanding",
        };

        public static bool CanStamp(Map source, Map dest)
        {
            return source != null
                && dest != null
                && source != dest
                && ColonyBedUtility.MapsLinked(source, dest);
        }

        public static int Stamp(Map source, Map dest, bool includeStockpiles, Rot4 rotation)
        {
            if (!CanStamp(source, dest))
            {
                return 0;
            }

            Sketch sketch = new Sketch();
            CellRect bounds = CellRect.WholeMap(source);
            IntVec3 origin = bounds.CenterCell;

            foreach (IntVec3 cell in bounds)
            {
                if (!cell.InBounds(source))
                {
                    continue;
                }

                TerrainDef terrain = source.terrainGrid.TerrainAt(cell);
                if (IsStampableFloor(terrain))
                {
                    IntVec3 local = Rotate(cell - origin, rotation);
                    sketch.AddTerrain(terrain, local);
                }

                Building edifice = cell.GetEdifice(source);
                if (edifice == null || !IsStampableBuilding(edifice))
                {
                    continue;
                }

                IntVec3 localThing = Rotate(cell - origin, rotation);
                Rot4 rot = rotation == Rot4.North
                    ? edifice.Rotation
                    : new Rot4((edifice.Rotation.AsInt + rotation.AsInt) % 4);
                sketch.AddThing(edifice.def, localThing, rot, edifice.Stuff);
            }

            IntVec3 destOrigin = CellRect.WholeMap(dest).CenterCell;
            sketch.Spawn(
                dest,
                destOrigin,
                Faction.OfPlayer,
                Sketch.SpawnPosType.Unchanged,
                Sketch.SpawnMode.Blueprint);

            int zones = 0;
            if (includeStockpiles)
            {
                zones = StampStockpiles(source, dest, origin, destOrigin, rotation);
            }

            return sketch.Things.Count + sketch.Terrain.Count + zones;
        }

        private static int StampStockpiles(
            Map source,
            Map dest,
            IntVec3 sourceOrigin,
            IntVec3 destOrigin,
            Rot4 rotation)
        {
            int n = 0;
            List<Zone> zones = source.zoneManager.AllZones;
            for (int i = 0; i < zones.Count; i++)
            {
                if (zones[i] is not Zone_Stockpile src)
                {
                    continue;
                }

                var destCells = new List<IntVec3>();
                foreach (IntVec3 cell in src.Cells)
                {
                    IntVec3 destCell = destOrigin + Rotate(cell - sourceOrigin, rotation);
                    if (destCell.InBounds(dest) && dest.zoneManager.ZoneAt(destCell) == null)
                    {
                        destCells.Add(destCell);
                    }
                }

                if (destCells.Count == 0)
                {
                    continue;
                }

                var copy = new Zone_Stockpile(StorageSettingsPreset.DefaultStockpile, dest.zoneManager);
                copy.settings.CopyFrom(src.settings);
                dest.zoneManager.RegisterZone(copy);
                for (int c = 0; c < destCells.Count; c++)
                {
                    copy.AddCell(destCells[c]);
                }
                n++;
            }
            return n;
        }

        private static bool IsStampableFloor(TerrainDef terrain)
        {
            if (terrain == null || terrain.IsWater)
            {
                return false;
            }
            return terrain.IsFloor || terrain.natural == false;
        }

        private static bool IsStampableBuilding(Building building)
        {
            if (building?.def?.building == null)
            {
                return false;
            }
            if (building.def.building.isNaturalRock || !building.def.BuildableByPlayer)
            {
                return false;
            }
            if (SkipDefNames.Contains(building.def.defName))
            {
                return false;
            }
            return building.Faction == null || building.Faction == Faction.OfPlayer;
        }

        private static IntVec3 Rotate(IntVec3 delta, Rot4 rotation)
        {
            if (rotation == Rot4.North)
            {
                return delta;
            }
            if (rotation == Rot4.East)
            {
                return new IntVec3(delta.z, 0, -delta.x);
            }
            if (rotation == Rot4.South)
            {
                return new IntVec3(-delta.x, 0, -delta.z);
            }
            return new IntVec3(-delta.z, 0, delta.x);
        }
    }

    public class Dialog_LevelStamp : Window
    {
        private readonly Map source;
        private Map dest;
        private bool includeStockpiles = true;
        private Rot4 rotation = Rot4.North;
        private readonly List<Map> candidates = new List<Map>();

        public override Vector2 InitialSize => new Vector2(420f, 320f);

        public Dialog_LevelStamp(Map source)
        {
            this.source = source;
            forcePause = true;
            absorbInputAroundWindow = true;
            closeOnClickedOutside = true;
            doCloseX = true;

            foreach (LevelGraph.LevelLink link in LevelGraph.ReachableLevels(source))
            {
                if (link.map != null && link.map != source)
                {
                    candidates.Add(link.map);
                }
            }
            if (candidates.Count > 0)
            {
                dest = candidates[0];
            }
        }

        public override void DoWindowContents(Rect inRect)
        {
            Listing_Standard listing = new Listing_Standard();
            listing.Begin(inRect);
            listing.Label("Strata_StampTitle".Translate());
            listing.GapLine();
            listing.Label("Strata_StampSource".Translate(Label(source)));

            if (candidates.Count == 0)
            {
                listing.Label("Strata_StampNoDest".Translate());
                listing.End();
                return;
            }

            if (listing.ButtonText(Label(dest)))
            {
                var opts = new List<FloatMenuOption>();
                for (int i = 0; i < candidates.Count; i++)
                {
                    Map map = candidates[i];
                    opts.Add(new FloatMenuOption(Label(map), () => dest = map));
                }
                Find.WindowStack.Add(new FloatMenu(opts));
            }

            listing.CheckboxLabeled("Strata_StampStockpiles".Translate(), ref includeStockpiles);
            if (listing.ButtonText("Strata_StampRotate".Translate(rotation.ToStringHuman())))
            {
                rotation = new Rot4((rotation.AsInt + 1) % 4);
            }

            listing.Gap();
            if (listing.ButtonText("Strata_StampConfirm".Translate()))
            {
                int n = LevelStampUtility.Stamp(source, dest, includeStockpiles, rotation);
                Messages.Message(
                    "Strata_StampDone".Translate(n),
                    MessageTypeDefOf.TaskCompletion,
                    historical: false);
                Close();
            }
            listing.End();
        }

        private static string Label(Map map)
        {
            string custom = StrataLevelLabels.Get?.GetLabel(map);
            if (!custom.NullOrEmpty())
            {
                return custom;
            }
            return map?.Parent?.LabelCap ?? map?.ToString() ?? "?";
        }
    }
}
