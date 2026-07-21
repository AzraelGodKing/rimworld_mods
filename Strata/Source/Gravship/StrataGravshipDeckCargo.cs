using System.Collections.Generic;
using RimWorld;
using RimWorld.Planet;
using Verse;

namespace Strata
{
    // G7 MVP: pack linked A+/B+ deck buildings/items into world cargo at takeoff
    // (engine-relative) and place them at the landed engine root — MultiFloors
    // SubGravship-lite. Terrain stays on the pocket; portals are never packed.
    public static class StrataGravshipDeckCargo
    {
        public class CargoThing : IExposable
        {
            public Thing thing;
            public IntVec3 local;
            public Rot4 rotation = Rot4.North;

            public void ExposeData()
            {
                Scribe_Deep.Look(ref thing, "thing");
                Scribe_Values.Look(ref local, "local", IntVec3.Invalid);
                Scribe_Values.Look(ref rotation, "rot", Rot4.North);
            }
        }

        public class DeckCargo : IExposable
        {
            public int pocketMapId = -1;
            public bool isTower;
            public List<CargoThing> things = new List<CargoThing>();

            public void ExposeData()
            {
                Scribe_Values.Look(ref pocketMapId, "pocketMapId", -1);
                Scribe_Values.Look(ref isTower, "isTower", false);
                Scribe_Collections.Look(ref things, "things", LookMode.Deep);
                things ??= new List<CargoThing>();
            }
        }

        private static readonly List<DeckCargo> pending = new List<DeckCargo>();
        private static bool placedThisLand;

        public static bool HasPending => pending.Count > 0;

        public static bool PlacedThisLand => placedThisLand;

        public static void ResetSession()
        {
            pending.Clear();
            placedThisLand = false;
        }

        public static List<DeckCargo> TakeForSave() => new List<DeckCargo>(pending);

        public static void RestoreFromSave(List<DeckCargo> saved)
        {
            pending.Clear();
            if (saved != null)
            {
                pending.AddRange(saved);
            }
        }

        public static void CaptureAll(Building_GravEngine engine, List<Map> levels)
        {
            CaptureAll(engine, levels, engine != null ? engine.Position : IntVec3.Invalid);
        }

        public static void CaptureAll(
            Building_GravEngine engine,
            List<Map> levels,
            IntVec3 origin)
        {
            pending.Clear();
            placedThisLand = false;
            if (engine == null || levels == null)
            {
                return;
            }
            if (!origin.IsValid)
            {
                origin = engine.Position;
            }
            for (int i = 0; i < levels.Count; i++)
            {
                Map pocket = levels[i];
                if (pocket == null || !StrataGravshipStackUtility.IsStrataLinkedLevel(pocket))
                {
                    continue;
                }
                DeckCargo cargo = CaptureOne(pocket, origin);
                if (cargo.things.Count > 0)
                {
                    pending.Add(cargo);
                }
            }
            if (pending.Count > 0)
            {
                Log.Message("[Strata] G7 deck cargo: packed " + pending.Count
                    + " linked level(s) for land place.");
            }
        }

        private static DeckCargo CaptureOne(Map pocket, IntVec3 origin)
        {
            var cargo = new DeckCargo
            {
                pocketMapId = pocket.uniqueID,
                isTower = StrataMapUtility.IsUpperLevel(pocket),
            };
            var seen = new HashSet<Thing>();
            var snapshot = new List<Thing>(pocket.listerThings.AllThings);
            for (int i = 0; i < snapshot.Count; i++)
            {
                Thing thing = snapshot[i];
                if (thing == null || thing.Destroyed || !thing.Spawned || thing is Pawn)
                {
                    continue;
                }
                if (thing is MapPortal || !seen.Add(thing))
                {
                    continue;
                }
                if (thing.def.defName == StrataGravshipSubstructureSync.SubstructureDefName)
                {
                    continue;
                }
                if (thing.def.category != ThingCategory.Building
                    && thing.def.category != ThingCategory.Item)
                {
                    continue;
                }
                if (thing.def.category == ThingCategory.Item && !thing.def.bringAlongOnGravship)
                {
                    continue;
                }
                // Keep landings / exits on the pocket so wiring stays intact.
                if (StrataGravshipUtility.IsGravshipLanding(thing) || thing is PocketMapExit)
                {
                    continue;
                }
                IntVec3 local = thing.Position - origin;
                Rot4 rot = thing.Rotation;
                thing.DeSpawn(DestroyMode.WillReplace);
                cargo.things.Add(new CargoThing
                {
                    thing = thing,
                    local = local,
                    rotation = rot,
                });
            }
            return cargo;
        }

        public static void PlaceAll(Gravship ship, Map host, Building_GravEngine engine)
        {
            placedThisLand = false;
            if (pending.Count == 0 || host == null || engine == null)
            {
                return;
            }
            IntVec3 root = engine.Position;
            int placedThings = 0;
            for (int i = 0; i < pending.Count; i++)
            {
                DeckCargo cargo = pending[i];
                Map pocket = StrataGravshipOrphanLevels.FindMapById(cargo.pocketMapId);
                if (pocket == null || !Find.Maps.Contains(pocket))
                {
                    continue;
                }
                for (int t = 0; t < cargo.things.Count; t++)
                {
                    CargoThing ct = cargo.things[t];
                    if (ct?.thing == null || ct.thing.Destroyed || ct.thing.Spawned)
                    {
                        continue;
                    }
                    IntVec3 dest = (root + ct.local).ClampInsideMap(pocket);
                    if (GenSpawn.Spawn(ct.thing, dest, pocket, ct.rotation, WipeMode.Vanish) != null)
                    {
                        placedThings++;
                    }
                }
            }
            pending.Clear();
            placedThisLand = placedThings > 0;
            if (placedThings > 0)
            {
                Log.Message("[Strata] G7 deck cargo: placed " + placedThings
                    + " thing(s) at landed engine root " + root + ".");
            }
        }
    }
}
