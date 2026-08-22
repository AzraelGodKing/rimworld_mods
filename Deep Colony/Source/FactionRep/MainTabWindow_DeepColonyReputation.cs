using System.Collections.Generic;
using System.Linq;
using RimWorld;
using UnityEngine;
using Verse;

namespace DeepColony
{
    public class MainTabWindow_DeepColonyReputation : MainTabWindow
    {
        private Vector2 factionScroll;
        private Vector2 ledgerScroll;
        private Faction selected;
        private int repFilter; // 0 all, 1 ally, 2 hostile

        public override Vector2 RequestedTabSize => new Vector2(900f, 560f);

        public override void DoWindowContents(Rect inRect)
        {
            Text.Font = GameFont.Medium;
            Widgets.Label(new Rect(inRect.x, inRect.y, inRect.width, 32f),
                "DC_RepTitle".Translate());
            Text.Font = GameFont.Small;

            if (!DeepColonySettings.Get.enableFactionRep)
            {
                Widgets.Label(new Rect(inRect.x, inRect.y + 40f, inRect.width, 40f),
                    "DC_RepDisabled".Translate());
                return;
            }

            float y = inRect.y + 36f;
            Widgets.Label(new Rect(inRect.x, y, inRect.width, 22f),
                DeepColonySettings.Get.enableAttitudeConsequences
                    ? "DC_RepConsequencesOn".Translate()
                    : "DC_RepConsequencesOff".Translate());
            y += 24f;

            Rect filterRect = new Rect(inRect.x, y, 180f, 26f);
            if (Widgets.ButtonText(filterRect, RepFilterLabel()))
            {
                var opts = new List<FloatMenuOption>
                {
                    new FloatMenuOption("DC_RepFilter_All".Translate(), () => repFilter = 0),
                    new FloatMenuOption("DC_RepFilter_Ally".Translate(), () => repFilter = 1),
                    new FloatMenuOption("DC_RepFilter_Hostile".Translate(), () => repFilter = 2)
                };
                Find.WindowStack.Add(new FloatMenu(opts));
            }
            y += 30f;

            float listW = inRect.width * 0.38f;
            Rect listOut = new Rect(inRect.x, y, listW, inRect.yMax - y);
            Rect detailOut = new Rect(inRect.x + listW + 12f, y, inRect.width - listW - 12f, inRect.yMax - y);

            var factions = Find.FactionManager.AllFactionsListForReading
                .Where(f => !f.IsPlayer && !f.defeated && !f.Hidden)
                .Where(MatchesRepFilter)
                .OrderByDescending(f => f.GoodwillWith(Faction.OfPlayer))
                .ToList();

            // Left: faction names (+ goodwill) only — details live on the right.
            float rowH = 28f;
            Rect listView = new Rect(0, 0, listOut.width - 16f, Mathf.Max(factions.Count * rowH, listOut.height));
            Widgets.BeginScrollView(listOut, ref factionScroll, listView);
            float ry = 0f;
            foreach (Faction f in factions)
            {
                Rect row = new Rect(0, ry, listView.width, rowH - 2f);
                if (selected == f)
                    Widgets.DrawHighlightSelected(row);
                else if (Mouse.IsOver(row))
                    Widgets.DrawHighlight(row);

                int gw = f.GoodwillWith(Faction.OfPlayer);
                Widgets.Label(new Rect(row.x + 4f, row.y + 4f, row.width - 8f, row.height - 4f),
                    $"{f.Name}  {gw:+0;-0;0}");

                if (Widgets.ButtonInvisible(row))
                    selected = f;
                ry += rowH;
            }
            Widgets.EndScrollView();

            // Right: reputation details only after a faction is clicked.
            Widgets.DrawMenuSection(detailOut);
            Rect inner = detailOut.ContractedBy(8f);
            if (selected == null || selected.defeated)
            {
                selected = null;
                Widgets.Label(inner, "DC_RepSelectFaction".Translate());
                return;
            }

            float dy = inner.y;
            Text.Font = GameFont.Medium;
            Widgets.Label(new Rect(inner.x, dy, inner.width, 28f), "DC_RepDetailHeader".Translate());
            Text.Font = GameFont.Small;
            dy += 30f;

            Widgets.Label(new Rect(inner.x, dy, inner.width, 22f), selected.Name);
            dy += 24f;
            Widgets.Label(new Rect(inner.x, dy, inner.width, 22f),
                "DC_RepGoodwill".Translate(selected.GoodwillWith(Faction.OfPlayer)));
            dy += 22f;

            float pending = GameComp_DeepColony.Instance?.GetPendingDrift(selected) ?? 0f;
            Widgets.Label(new Rect(inner.x, dy, inner.width, 22f),
                "DC_RepPending".Translate(pending.ToString("+0.00;-0.00")));
            dy += 22f;

            Widgets.Label(new Rect(inner.x, dy, inner.width, 22f),
                "DC_RepAttitude".Translate(
                    FactionAttitudeUtility.AttitudeLabel(FactionAttitudeUtility.GetAttitude(selected))));
            dy += 22f;

            string epithet = FactionEpithetUtility.TryGetEpithet(selected);
            Widgets.Label(new Rect(inner.x, dy, inner.width, 22f),
                epithet.NullOrEmpty()
                    ? "DC_RepNoEpithet".Translate()
                    : "DC_RepEpithet".Translate(epithet));
            dy += 22f;

            Pawn selectedEnvoy = FactionEnvoyUtility.FindEnvoy(selected);
            Widgets.Label(new Rect(inner.x, dy, inner.width, 22f),
                selectedEnvoy != null
                    ? "DC_RepEnvoy".Translate(selectedEnvoy.LabelShort)
                    : "DC_RepNoEnvoy".Translate());
            dy += 24f;

            float btnW = 140f;
            Rect assignRect = new Rect(inner.x, dy, btnW, 28f);
            if (Widgets.ButtonText(assignRect, "DC_AssignEnvoy".Translate()))
            {
                List<FloatMenuOption> opts = new List<FloatMenuOption>();
                foreach (Pawn p in FactionEnvoyUtility.EnvoyCandidates())
                {
                    Pawn local = p;
                    string label = local.LabelShortCap;
                    if (SoftCompat.HasAnyRoyalTitle(local))
                        label += " " + "DC_EnvoyTitled".Translate();
                    opts.Add(new FloatMenuOption(label,
                        () => FactionEnvoyUtility.SetEnvoy(local, selected)));
                }
                if (opts.Count == 0)
                {
                    opts.Add(new FloatMenuOption("DC_NoEnvoyCandidates".Translate(), null) { Disabled = true });
                }
                Find.WindowStack.Add(new FloatMenu(opts));
            }
            if (selectedEnvoy != null)
            {
                Rect clearRect = new Rect(inner.x + btnW + 8f, dy, btnW, 28f);
                if (Widgets.ButtonText(clearRect, "DC_ClearEnvoyButton".Translate()))
                    FactionEnvoyUtility.ClearEnvoy(selectedEnvoy);
            }
            dy += 36f;

            if (DeepColonySettings.Get.enableApologyTribute)
            {
                Rect tributeRect = new Rect(inner.x, dy, btnW + 40f, 28f);
                if (Widgets.ButtonText(tributeRect, "DC_SendTribute".Translate()))
                    TributeUtility.TrySendTribute(selected);
                dy += 36f;
            }

            Widgets.Label(new Rect(inner.x, dy, inner.width, 22f), "DC_RepLedgerHeader".Translate());
            dy += 24f;

            var entries = GameComp_DeepColony.Instance?.GetLedger(selected)?.ToList()
                ?? new List<FactionRepLedgerEntry>();
            Rect ledgerOut = new Rect(inner.x, dy, inner.width, inner.yMax - dy);
            float entryH = 22f;
            Rect ledgerView = new Rect(0, 0, ledgerOut.width - 16f,
                Mathf.Max(entries.Count * entryH + 4f, ledgerOut.height));
            Widgets.BeginScrollView(ledgerOut, ref ledgerScroll, ledgerView);
            float ey = 0f;
            if (entries.Count == 0)
            {
                Widgets.Label(new Rect(4f, ey, ledgerView.width - 8f, entryH),
                    "DC_RepLedgerEmpty".Translate());
            }
            else
            {
                foreach (FactionRepLedgerEntry e in entries)
                {
                    float daysAgo = (Find.TickManager.TicksGame - e.ticksGame) / 60000f;
                    string line = "DC_RepLedgerLine".Translate(
                        e.ReasonLabel(),
                        e.amount.ToString("+0.00;-0.00"),
                        e.count,
                        daysAgo.ToString("F1"));
                    Widgets.Label(new Rect(4f, ey, ledgerView.width - 8f, entryH), line);
                    ey += entryH;
                }
            }
            Widgets.EndScrollView();
        }

        private string RepFilterLabel()
        {
            string key = repFilter == 1 ? "DC_RepFilter_Ally"
                : repFilter == 2 ? "DC_RepFilter_Hostile"
                : "DC_RepFilter_All";
            return key.Translate();
        }

        private bool MatchesRepFilter(Faction f)
        {
            if (repFilter == 1)
                return f.RelationKindWith(Faction.OfPlayer) == FactionRelationKind.Ally
                    || f.GoodwillWith(Faction.OfPlayer) >= 0;
            if (repFilter == 2)
                return f.RelationKindWith(Faction.OfPlayer) == FactionRelationKind.Hostile
                    || f.GoodwillWith(Faction.OfPlayer) < 0;
            return true;
        }
    }
}
