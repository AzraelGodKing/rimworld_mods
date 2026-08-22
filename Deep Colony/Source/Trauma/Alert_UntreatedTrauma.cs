using System.Collections.Generic;
using System.Text;
using RimWorld;
using Verse;

namespace DeepColony
{
    public class Alert_UntreatedTrauma : Alert
    {
        private const int IdleTicks = 60000; // 1 day

        private readonly List<Pawn> idle = new List<Pawn>();

        public Alert_UntreatedTrauma()
        {
            defaultLabel = "DC_Alert_UntreatedTrauma".Translate();
            defaultPriority = AlertPriority.Medium;
        }

        private void Rebuild()
        {
            idle.Clear();
            if (!DeepColonySettings.Get.enableTrauma) return;
            if (Find.CurrentMap == null) return;

            int now = Find.TickManager.TicksGame;
            foreach (Map map in Find.Maps)
            {
                foreach (Pawn p in map.mapPawns.FreeColonistsSpawned)
                {
                    if (!TraumaUtility.HasAnyTrauma(p)) continue;
                    var comp = p.TryGetComp<Comp_DeepColony>();
                    if (comp == null) continue;
                    if (comp.untreatedTraumaSinceTick < 0) continue;
                    if (now - comp.untreatedTraumaSinceTick < IdleTicks) continue;
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
            sb.AppendLine("DC_Alert_UntreatedTraumaDesc".Translate());
            sb.AppendLine();
            foreach (Pawn p in idle)
                sb.AppendLine("  " + p.LabelShort);
            return sb.ToString();
        }
    }
}
