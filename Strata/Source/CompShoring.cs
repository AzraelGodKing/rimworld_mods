using System.Collections.Generic;
using HarmonyLib;
using RimWorld;
using Verse;

namespace Strata
{
    public class CompProperties_Shoring : CompProperties
    {
        // Cave-in protection radius — twice vanilla column roof support (6.9 → 13.8).
        public float protectionRadius = 13.8f;

        public CompProperties_Shoring()
        {
            compClass = typeof(CompShoring);
        }
    }

    // Reinforcement pillar — nearby excavations resist roof collapse.
    public class CompShoring : ThingComp
    {
        public CompProperties_Shoring Props => (CompProperties_Shoring)props;

        public override void PostSpawnSetup(bool respawningAfterLoad)
        {
            base.PostSpawnSetup(respawningAfterLoad);
            parent.Map.GetComponent<ShoringMapComponent>()?.Register(this);
        }

        public override void PostDeSpawn(Map map, DestroyMode mode = DestroyMode.Vanish)
        {
            map.GetComponent<ShoringMapComponent>()?.Unregister(this);
            base.PostDeSpawn(map, mode);
        }

        public override string CompInspectStringExtra()
        {
            return "Strata_ShoringInspect".Translate(Props.protectionRadius.ToString("0.#"));
        }
    }

    public class ShoringMapComponent : MapComponent
    {
        private readonly System.Collections.Generic.HashSet<CompShoring> pillars =
            new System.Collections.Generic.HashSet<CompShoring>();

        public ShoringMapComponent(Map map) : base(map)
        {
        }

        public void Register(CompShoring pillar) => pillars.Add(pillar);

        public void Unregister(CompShoring pillar) => pillars.Remove(pillar);

        public bool CellIsProtected(IntVec3 cell)
        {
            foreach (CompShoring pillar in pillars)
            {
                if (!pillar.parent.Spawned)
                {
                    continue;
                }
                float r = pillar.Props.protectionRadius;
                if (cell.InHorDistOf(pillar.parent.Position, r))
                {
                    return true;
                }
            }
            return false;
        }

        public int ActivePillarCount
        {
            get
            {
                int n = 0;
                foreach (CompShoring pillar in pillars)
                {
                    if (pillar.parent.Spawned)
                    {
                        n++;
                    }
                }
                return n;
            }
        }

        // Twice vanilla RoofCollapseUtility.RoofMaxSupportDistance for shoring pillars.
        public const float RoofSupportRadius = 13.8f;

        public static bool WithinRangeOfShoringRoofHolder(IntVec3 c, Map map, bool assumeNonNoRoofCellsAreRoofed)
        {
            ThingDef shoring = StrataThingDefOf.Strata_ShoringPillar;
            if (map == null || shoring == null)
            {
                return false;
            }
            bool connected = false;
            map.floodFiller.FloodFill(
                c,
                x => (x.Roofed(map) || x == c
                    || (assumeNonNoRoofCellsAreRoofed && !map.areaManager.NoRoof[x]))
                    && x.InHorDistOf(c, RoofSupportRadius),
                x =>
                {
                    for (int i = 0; i < 5; i++)
                    {
                        IntVec3 adj = x + GenAdj.CardinalDirectionsAndInside[i];
                        if (!adj.InBounds(map) || !adj.InHorDistOf(c, RoofSupportRadius))
                        {
                            continue;
                        }
                        Building edifice = adj.GetEdifice(map);
                        if (edifice != null && edifice.def == shoring)
                        {
                            connected = true;
                            return true;
                        }
                    }
                    return false;
                });
            return connected;
        }

        public static bool ConnectedToShoringRoofHolder(IntVec3 c, Map map, bool assumeRoofAtRoot)
        {
            ThingDef shoring = StrataThingDefOf.Strata_ShoringPillar;
            if (map == null || shoring == null)
            {
                return false;
            }
            bool connected = false;
            map.floodFiller.FloodFill(
                c,
                x => (x.Roofed(map) || (x == c && assumeRoofAtRoot)) && !connected,
                x =>
                {
                    for (int i = 0; i < 5; i++)
                    {
                        IntVec3 adj = x + GenAdj.CardinalDirectionsAndInside[i];
                        if (!adj.InBounds(map))
                        {
                            continue;
                        }
                        Building edifice = adj.GetEdifice(map);
                        if (edifice != null && edifice.def == shoring)
                        {
                            connected = true;
                            break;
                        }
                    }
                });
            return connected;
        }
    }

    [HarmonyPatch(typeof(RoofCollapseUtility), nameof(RoofCollapseUtility.WithinRangeOfRoofHolder))]
    public static class Patch_ShoringWithinRangeOfRoofHolder
    {
        public static void Postfix(IntVec3 c, Map map, bool assumeNonNoRoofCellsAreRoofed, ref bool __result)
        {
            if (!__result)
            {
                __result = ShoringMapComponent.WithinRangeOfShoringRoofHolder(c, map, assumeNonNoRoofCellsAreRoofed);
            }
        }
    }

    [HarmonyPatch(typeof(RoofCollapseUtility), nameof(RoofCollapseUtility.ConnectedToRoofHolder))]
    public static class Patch_ShoringConnectedToRoofHolder
    {
        public static void Postfix(IntVec3 c, Map map, bool assumeRoofAtRoot, ref bool __result)
        {
            if (!__result)
            {
                __result = ShoringMapComponent.ConnectedToShoringRoofHolder(c, map, assumeRoofAtRoot);
            }
        }
    }
}
