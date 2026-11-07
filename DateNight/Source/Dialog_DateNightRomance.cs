#nullable enable
using System.Collections.Generic;
using RimWorld;
using UnityEngine;
using Verse;

namespace DateNight
{
    /// <summary>
    /// Romance info panel (AZR-524): the stored data that used to be reachable
    /// only through dev logging. Read-only, so nothing here needs syncing.
    /// </summary>
    public class Dialog_DateNightRomance : Window
    {
        private readonly Pawn pawn;
        private readonly List<string> lines = new List<string>();
        private string title = "";

        public Dialog_DateNightRomance(Pawn pawn)
        {
            this.pawn = pawn;
            doCloseX = true;
            doCloseButton = true;
            closeOnClickedOutside = true;
            absorbInputAroundWindow = false;
            forcePause = false;
        }

        public override Vector2 InitialSize => new Vector2(460f, 380f);

        public override void PreOpen()
        {
            base.PreOpen();
            Build();
        }

        public override void DoWindowContents(Rect inRect)
        {
            Text.Font = GameFont.Medium;
            Widgets.Label(new Rect(inRect.x, inRect.y, inRect.width, 34f), title);
            Text.Font = GameFont.Small;
            Listing_Standard listing = new Listing_Standard();
            listing.Begin(new Rect(inRect.x, inRect.y + 40f, inRect.width, inRect.height - 40f - CloseButSize.y));
            for (int i = 0; i < lines.Count; i++)
            {
                listing.Label(lines[i]);
            }
            listing.End();
        }

        private void Build()
        {
            lines.Clear();
            Pawn? partner = DateNightRomance.FindCommittedPartner(pawn);
            if (partner == null)
            {
                title = pawn.LabelShortCap;
                lines.Add("DateNight_Romance_NoPartner".Translate(pawn.LabelShort));
                return;
            }

            title = "DateNight_Romance_Title".Translate(pawn.LabelShort, partner.LabelShort);
            lines.Add("DateNight_Romance_Stage".Translate(DateNightRomance.StageLabel(pawn, partner)));

            DirectPawnRelation? rel = DateNightAnniversaries.GetLoveRelation(pawn, partner);
            if (rel != null)
            {
                int together = Find.TickManager.TicksGame - rel.startTicks;
                lines.Add("DateNight_Romance_Together".Translate(together.ToStringTicksToPeriod()));
                long startAbs = Find.TickManager.gameStartAbsTick + rel.startTicks;
                Vector2 longLat = LongLat(pawn);
                lines.Add("DateNight_Romance_Anniversary".Translate(
                    GenDate.DayOfQuadrum(startAbs, longLat.x) + 1,
                    GenDate.Quadrum(startAbs, longLat.x).Label()));
            }
            else
            {
                lines.Add("DateNight_Romance_Progress".Translate(
                    DateNightRomance.Progress(pawn, partner), DateNightRomance.DatesToAdvance));
            }

            CoupleHistory? history = DateNightHistory.Get(pawn, partner);
            if (history != null && history.lastDateTick >= 0)
            {
                string quality = history.lastQuality?.stages?.Count > 0
                    ? history.lastQuality.stages[0].label
                    : "-";
                lines.Add("DateNight_Romance_LastDate".Translate(
                    (Find.TickManager.TicksGame - history.lastDateTick).ToStringTicksToPeriod(),
                    quality, history.dates));
            }
            else
            {
                lines.Add("DateNight_Romance_NoLastDate".Translate());
            }

            FavoriteVenue? fav = DateNightVenues.Get(pawn, partner);
            string? venue = fav == null ? null : DateNightVenues.Describe(fav);
            lines.Add(venue == null
                ? "DateNight_Romance_NoVenue".Translate().ToString()
                : "DateNight_Romance_Venue".Translate(venue, fav!.score.ToString("0.0")).ToString());

            List<string> rapport = DateNightDoubleDates.DescribeRapport(pawn, partner);
            if (rapport.Count == 0)
            {
                lines.Add("DateNight_Romance_NoRapport".Translate());
            }
            else
            {
                lines.AddRange(rapport);
            }

            PlannedDate? plan = DateNightAskOut.PlanFor(pawn);
            if (plan != null)
            {
                lines.Add("DateNight_Romance_Planned".Translate(plan.hour));
            }
        }

        private static Vector2 LongLat(Pawn pawn)
        {
            var tile = pawn.Tile;
            return tile.Valid ? Find.WorldGrid.LongLatOf(tile) : Vector2.zero;
        }
    }
}
