using System.Collections.Generic;
using System.Text;
using RimWorld;
using Verse;

namespace DeepColony
{
    public class Alert_UnspentPerkPoints : Alert
    {
        private const int IdleTicks = 60000; // 1 day

        private readonly List<Pawn> idle = new List<Pawn>();

        public Alert_UnspentPerkPoints()
        {
            defaultLabel = "DC_Alert_UnspentPerks".Translate();
            defaultPriority = AlertPriority.Medium;
        }

        private void Rebuild()
        {
            idle.Clear();
            if (!DeepColonySettings.Get.enablePerks) return;
            if (Find.CurrentMap == null) return;

            int now = Find.TickManager.TicksGame;
            foreach (Map map in Find.Maps)
            {
                foreach (Pawn p in map.mapPawns.FreeColonistsSpawned)
                {
                    var comp = p.TryGetComp<Comp_DeepColony>();
                    if (comp == null || comp.availablePerkPoints <= 0) continue;
                    if (comp.unspentPerkPointsSinceTick < 0) continue;
                    if (now - comp.unspentPerkPointsSinceTick < IdleTicks) continue;
                    idle.Add(p);
                }
            }
        }

        public override AlertReport GetReport()
        {
            Rebuild();
            return idle.Count == 0 ? AlertReport.Inactive : AlertReport.CulpritsAre(idle);
        }

        public override TaggedString GetExplanation()
        {
            var sb = new StringBuilder();
            sb.AppendLine("DC_Alert_UnspentPerksDesc".Translate());
            sb.AppendLine();
            foreach (Pawn p in idle)
            {
                var comp = p.TryGetComp<Comp_DeepColony>();
                sb.AppendLine("  " + p.LabelShort + " — "
                    + "DC_PerkPoints".Translate(comp?.availablePerkPoints ?? 0));
            }
            return sb.ToString();
        }
    }
}
