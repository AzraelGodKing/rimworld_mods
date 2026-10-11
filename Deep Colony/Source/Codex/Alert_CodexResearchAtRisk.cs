using System.Collections.Generic;
using System.Text;
using RimWorld;
using Verse;

namespace DeepColony
{
    public class Alert_CodexResearchAtRisk : Alert
    {
        private readonly List<Pawn> atRisk = new List<Pawn>();

        public Alert_CodexResearchAtRisk()
        {
            defaultLabel = "DC_Alert_CodexAtRisk".Translate();
            defaultPriority = AlertPriority.Medium;
        }

        public override AlertReport GetReport()
        {
            CodexArchive.AtRiskResearchers(atRisk);
            return atRisk.Count == 0 ? AlertReport.Inactive : AlertReport.CulpritsAre(atRisk);
        }

        public override TaggedString GetExplanation()
        {
            ResearchProjectDef proj = CodexArchive.ProjectAtRisk();
            var sb = new StringBuilder();
            sb.AppendLine("DC_Alert_CodexAtRiskDesc".Translate(
                ((int)CodexArchive.LostProgress).Named("POINTS"),
                (proj?.LabelCap ?? (TaggedString)"—").Named("PROJECT")));
            sb.AppendLine();
            foreach (Pawn p in atRisk)
                sb.AppendLine("  " + p.LabelShort);
            return sb.ToString();
        }
    }
}
