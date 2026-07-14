using System.Collections.Generic;
using RimWorld;
using Verse;

namespace Strata
{
    public class MapComponent_DeepPockets : MapComponent
    {
        private List<DeepPocketRecord> pockets = new List<DeepPocketRecord>();

        public MapComponent_DeepPockets(Map map) : base(map)
        {
        }

        public IReadOnlyList<DeepPocketRecord> Pockets => pockets;

        public void Register(DeepPocketRecord record)
        {
            if (record != null)
            {
                pockets.Add(record);
            }
        }

        public DeepPocketRecord PocketAt(IntVec3 cell)
        {
            for (int i = 0; i < pockets.Count; i++)
            {
                DeepPocketRecord pocket = pockets[i];
                if (!pocket.breached && DeepPocketUtility.ContainsCell(pocket, cell))
                {
                    return pocket;
                }
            }
            return null;
        }

        public bool IsGasSourceCell(IntVec3 cell)
        {
            for (int i = 0; i < pockets.Count; i++)
            {
                DeepPocketRecord pocket = pockets[i];
                if (pocket.breached && pocket.kind == DeepPocketKind.NaturalGas && pocket.center == cell)
                {
                    return true;
                }
            }
            return false;
        }

        public override void ExposeData()
        {
            base.ExposeData();
            Scribe_Collections.Look(ref pockets, "pockets", LookMode.Deep);
            pockets ??= new List<DeepPocketRecord>();
        }
    }
}
