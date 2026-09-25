using System;
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
                Hediff convalHediff = conval != null ? pawn.health.hediffSet.GetFirstHediffOfDef(conval) : null;
                Room room = pawn.GetRoom();
                bool infirmary = BodyRooms.IsInfirmary(room);
                if (convalHediff != null && infirmary)
                {
                    convalHediff.Severity = Math.Max(0.05f, convalHediff.Severity - 0.04f);
                }
                if (convalHediff != null && pawn.Drafted && !infirmary)
                {
                    convalHediff.Severity = Math.Min(1f, convalHediff.Severity + 0.08f);
                    if (Rand.Chance(0.12f))
                    {
                        Messages.Message("DC_Body_Relapse".Translate(pawn.LabelShortCap), pawn, MessageTypeDefOf.NegativeEvent);
                    }
                }
                if (infection != null && conval != null && convalHediff == null && Rand.Chance(0.08f))
                {
                    pawn.health.AddHediff(conval);
                }
                if (infection == null)
                {
                    continue;
                }
                if (room == null || room.PsychologicallyOutdoors)
                {
                    continue;
                }
                float spread = infirmary ? 0.012f : 0.04f;
                for (int j = 0; j < pawns.Count; j++)
                {
                    Pawn other = pawns[j];
                    if (other == pawn || other.GetRoom() != room)
                    {
                        continue;
                    }
                    if (Rand.Chance(spread) && other.health.hediffSet.GetFirstHediffOfDef(infection.def) == null)
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
