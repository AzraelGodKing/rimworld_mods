using RimWorld;
using Verse;

namespace Strata
{
    public class CompProperties_Shoring : CompProperties
    {
        // Cells within this radius are protected from cave-in collapse.
        public float protectionRadius = 4.5f;

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
            return "Reinforces rock ceiling within " + Props.protectionRadius.ToString("0.#") + " tiles.";
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
    }
}
