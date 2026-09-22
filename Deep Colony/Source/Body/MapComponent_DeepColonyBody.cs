using System.Collections.Generic;
using RimWorld;
using Verse;

namespace DeepColony
{
    public class MapComponent_DeepColonyBody : MapComponent
    {
        public MapComponent_DeepColonyBody(Map map) : base(map)
        {
        }

        public override void MapComponentTick()
        {
            if (!DeepColonySettings.Get.enableBody)
            {
                return;
            }
            if (!map.IsHashIntervalTick(2500))
            {
                return;
            }

            HediffDef conval = DefDatabase<HediffDef>.GetNamedSilentFail("DC_Hediff_Convalescence");
            List<Pawn> pawns = map.mapPawns.FreeColonistsSpawned;
            for (int i = 0; i < pawns.Count; i++)
            {
                Pawn pawn = pawns[i];
                if (pawn?.health?.hediffSet == null)
                {
                    continue;
                }
                Hediff infection = pawn.health.hediffSet.hediffs.Find(h => h.def.makesAlert && h.def.isBad && h.def.lethalSeverity > 0f && h.Severity > 0.35f);
                if (infection != null && conval != null && pawn.health.hediffSet.GetFirstHediffOfDef(conval) == null && Rand.Chance(0.08f))
                {
                    pawn.health.AddHediff(conval);
                }
                if (infection == null)
                {
                    continue;
                }
                Room room = pawn.GetRoom();
                if (room == null || room.PsychologicallyOutdoors)
                {
                    continue;
                }
                for (int j = 0; j < pawns.Count; j++)
                {
                    Pawn other = pawns[j];
                    if (other == pawn || other.GetRoom() != room)
                    {
                        continue;
                    }
                    if (Rand.Chance(0.04f) && other.health.hediffSet.GetFirstHediffOfDef(infection.def) == null)
                    {
                        Hediff copy = HediffMaker.MakeHediff(infection.def, other);
                        copy.Severity = 0.12f;
                        other.health.AddHediff(copy);
                    }
                }
            }
        }
    }
}
