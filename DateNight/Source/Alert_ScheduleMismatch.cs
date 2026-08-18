using System.Collections.Generic;
using System.Text;
using RimWorld;
using Verse;

namespace DateNight
{
    public class Alert_ScheduleMismatch : Alert
    {
        private const int ScanInterval = 250;

        private readonly List<Pawn> culprits = new List<Pawn>();
        private readonly List<MismatchRow> rows = new List<MismatchRow>();
        private AlertReport cachedReport = AlertReport.Inactive;
        private int lastScanTick = -9999;

        public Alert_ScheduleMismatch()
        {
            defaultLabel = "DateNight_Alert_Mismatch".Translate();
            defaultPriority = AlertPriority.Medium;
        }

        public override string GetLabel()
        {
            if (rows.Count <= 1)
            {
                return defaultLabel;
            }
            return defaultLabel + " (" + rows.Count + ")";
        }

        public override AlertReport GetReport()
        {
            int tick = Find.TickManager.TicksGame;
            if (tick - lastScanTick < ScanInterval)
            {
                return cachedReport;
            }
            lastScanTick = tick;
            Rebuild();
            cachedReport = culprits.Count == 0
                ? AlertReport.Inactive
                : AlertReport.CulpritsAre(culprits);
            return cachedReport;
        }

        public override TaggedString GetExplanation()
        {
            StringBuilder sb = new StringBuilder();
            sb.AppendLine("DateNight_Alert_MismatchDesc".Translate());
            sb.AppendLine();
            for (int i = 0; i < rows.Count; i++)
            {
                MismatchRow row = rows[i];
                if (!row.line.NullOrEmpty())
                {
                    sb.AppendLine("  " + row.line);
                }
            }
            return sb.ToString();
        }

        private void Rebuild()
        {
            culprits.Clear();
            rows.Clear();

            TimeAssignmentDef lovin = DateNightDefOf.DateNight_Lovin;
            TimeAssignmentDef date = DateNightDefOf.DateNight_Date;
            if (lovin == null && date == null)
            {
                return;
            }

            List<Pawn> colonists = PawnsFinder.AllMapsCaravansAndTravellingTransporters_Alive_FreeColonists;
            if (colonists == null)
            {
                return;
            }

            HashSet<long> seen = new HashSet<long>();
            for (int i = 0; i < colonists.Count; i++)
            {
                Pawn pawn = colonists[i];
                if (!IsEligible(pawn))
                {
                    continue;
                }

                Pawn partner = LovePartnerRelationUtility.ExistingMostLikedLovePartner(pawn, allowDead: false);
                if (!IsEligible(partner) || partner.thingIDNumber == pawn.thingIDNumber)
                {
                    continue;
                }
                if (!colonists.Contains(partner))
                {
                    continue;
                }

                int x = pawn.thingIDNumber;
                int y = partner.thingIDNumber;
                if (x > y)
                {
                    int tmp = x;
                    x = y;
                    y = tmp;
                }
                long key = ((long)x << 32) | (uint)y;
                if (!seen.Add(key))
                {
                    continue;
                }

                AppendSlot(pawn, partner, lovin, "DateNight_Alert_SlotLovin".Translate());
                AppendSlot(pawn, partner, date, "DateNight_Alert_SlotDate".Translate());
            }
        }

        private void AppendSlot(Pawn a, Pawn b, TimeAssignmentDef def, string slotLabel)
        {
            if (def == null || slotLabel.NullOrEmpty())
            {
                return;
            }

            bool aAny = DateNightUtility.HasAnyHour(a, def);
            bool bAny = DateNightUtility.HasAnyHour(b, def);
            if (!aAny && !bAny)
            {
                return;
            }
            if (DateNightUtility.SameHours(a, b, def))
            {
                return;
            }

            string line;
            if (aAny && !bAny)
            {
                line = "DateNight_Alert_MismatchNone".Translate(a.LabelShort, slotLabel, b.LabelShort);
            }
            else if (bAny && !aAny)
            {
                line = "DateNight_Alert_MismatchNone".Translate(b.LabelShort, slotLabel, a.LabelShort);
            }
            else
            {
                line = "DateNight_Alert_MismatchHours".Translate(a.LabelShort, b.LabelShort, slotLabel);
            }

            rows.Add(new MismatchRow { line = line });
            AddCulprit(a);
            AddCulprit(b);
        }

        private void AddCulprit(Pawn pawn)
        {
            if (pawn != null && !culprits.Contains(pawn))
            {
                culprits.Add(pawn);
            }
        }

        private static bool IsEligible(Pawn pawn)
        {
            if (pawn == null || pawn.Dead || pawn.timetable == null)
            {
                return false;
            }
            if (!pawn.RaceProps.Humanlike)
            {
                return false;
            }
            if (pawn.ageTracker == null || !pawn.ageTracker.Adult || !pawn.DevelopmentalStage.Adult())
            {
                return false;
            }
            return true;
        }

        private struct MismatchRow
        {
            public string line;
        }
    }
}
