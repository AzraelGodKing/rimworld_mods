using System.Collections.Generic;
using System.Linq;
using RimWorld;
using UnityEngine;
using Verse;

namespace Wardrobe
{
    public class MainTabWindow_Wardrobe : MainTabWindow
    {
        private Vector2 scroll;
        private Pawn selected;

        public override Vector2 RequestedTabSize => new Vector2(920f, 580f);

        public override void DoWindowContents(Rect inRect)
        {
            Text.Font = GameFont.Medium;
            Widgets.Label(new Rect(inRect.x, inRect.y, inRect.width, 30f), "Wardrobe_TabTitle".Translate());
            Text.Font = GameFont.Small;
            Widgets.Label(new Rect(inRect.x, inRect.y + 28f, inRect.width, 22f), "Wardrobe_TabIntro".Translate());

            float y = inRect.y + 54f;
            float listW = 220f;
            Rect listOut = new Rect(inRect.x, y, listW, inRect.yMax - y);
            Rect detail = new Rect(inRect.x + listW + 12f, y, inRect.width - listW - 12f, inRect.yMax - y);

            List<Pawn> pawns = Find.CurrentMap?.mapPawns?.FreeColonistsSpawned?
                .OrderBy(p => p.LabelShort).ToList() ?? new List<Pawn>();

            float rowH = 28f;
            Rect view = new Rect(0, 0, listOut.width - 16f, Mathf.Max(pawns.Count * rowH, listOut.height));
            Widgets.BeginScrollView(listOut, ref scroll, view);
            float ry = 0f;
            for (int i = 0; i < pawns.Count; i++)
            {
                Pawn p = pawns[i];
                Rect row = new Rect(0, ry, view.width, rowH - 2f);
                if (selected == p)
                {
                    Widgets.DrawHighlightSelected(row);
                }
                else if (Mouse.IsOver(row))
                {
                    Widgets.DrawHighlight(row);
                }

                Widgets.Label(row.ContractedBy(4f), p.LabelShortCap);
                if (Widgets.ButtonInvisible(row))
                {
                    selected = p;
                }

                ry += rowH;
            }

            Widgets.EndScrollView();

            Widgets.DrawMenuSection(detail);
            Rect inner = detail.ContractedBy(10f);
            if (selected == null || selected.Dead || !selected.Spawned)
            {
                selected = null;
                Widgets.Label(inner, "Wardrobe_SelectColonist".Translate());
                return;
            }

            DrawPawnRules(inner, selected);
        }

        private static void DrawPawnRules(Rect rect, Pawn pawn)
        {
            GameComponent_Wardrobe comp = WardrobeUtility.Comp;
            if (comp == null)
            {
                Widgets.Label(rect, "Wardrobe_NoGame".Translate());
                return;
            }

            WardrobePawnState state = comp.GetState(pawn, create: true);
            Listing_Standard listing = new Listing_Standard();
            listing.Begin(rect);

            listing.Label("Wardrobe_Colonist".Translate(pawn.LabelShortCap));
            if (state.IsManaged)
            {
                listing.Label("Wardrobe_ActiveMode".Translate(state.activeTrigger.ToString()));
            }

            listing.GapLine();
            listing.Label("Wardrobe_Stockpile".Translate());
            List<Zone_Stockpile> stocks = WardrobeUtility.AllStockpiles(pawn.Map);
            string stockLabel = stocks.FirstOrDefault(s => s.ID == state.stockpileId)?.label
                ?? "Wardrobe_StockpileNone".Translate();
            if (listing.ButtonText(stockLabel))
            {
                List<FloatMenuOption> opts = new List<FloatMenuOption>
                {
                    new FloatMenuOption("Wardrobe_StockpileNone".Translate(), () => state.stockpileId = -1)
                };
                foreach (Zone_Stockpile s in stocks)
                {
                    Zone_Stockpile local = s;
                    opts.Add(new FloatMenuOption(local.label, () => state.stockpileId = local.ID));
                }

                Find.WindowStack.Add(new FloatMenu(opts));
            }

            listing.GapLine();
            DrawTriggerRow(listing, state, WardrobeTrigger.Sleep, "Wardrobe_TriggerSleep".Translate());
            DrawTriggerRow(listing, state, WardrobeTrigger.Cook, "Wardrobe_TriggerCook".Translate());
            DrawTriggerRow(listing, state, WardrobeTrigger.Doctor, "Wardrobe_TriggerDoctor".Translate());
            DrawTriggerRow(listing, state, WardrobeTrigger.Animals, "Wardrobe_TriggerAnimals".Translate());

            listing.Gap(12f);
            if (listing.ButtonText("Wardrobe_ClearRules".Translate(), null, 0.35f))
            {
                state.sleepEnabled = state.cookEnabled = state.doctorEnabled = state.animalsEnabled = false;
                state.activeTrigger = WardrobeTrigger.None;
                state.snapshotThingIds.Clear();
                state.snapshotDefNames.Clear();
            }

            listing.End();
        }

        private static void DrawTriggerRow(
            Listing_Standard listing, WardrobePawnState state, WardrobeTrigger trigger, string label)
        {
            bool enabled = state.EnabledFor(trigger);
            listing.CheckboxLabeled(label, ref enabled);
            SetEnabled(state, trigger, enabled);
            if (!enabled)
            {
                return;
            }

            int policyId = state.PolicyIdFor(trigger);
            ApparelPolicy policy = WardrobeUtility.FindPolicy(policyId);
            string name = policy != null ? policy.label : "Wardrobe_PolicyNone".Translate();
            if (listing.ButtonText(("Wardrobe_Policy".Translate() + ": " + name), null, 0.7f))
            {
                List<FloatMenuOption> opts = new List<FloatMenuOption>
                {
                    new FloatMenuOption("Wardrobe_PolicyNone".Translate(), () => SetPolicy(state, trigger, -1))
                };
                List<ApparelPolicy> all = Current.Game?.outfitDatabase?.AllOutfits;
                if (all != null)
                {
                    foreach (ApparelPolicy p in all)
                    {
                        ApparelPolicy local = p;
                        opts.Add(new FloatMenuOption(local.label, () => SetPolicy(state, trigger, local.id)));
                    }
                }

                Find.WindowStack.Add(new FloatMenu(opts));
            }
        }

        private static void SetEnabled(WardrobePawnState state, WardrobeTrigger trigger, bool enabled)
        {
            switch (trigger)
            {
                case WardrobeTrigger.Sleep: state.sleepEnabled = enabled; break;
                case WardrobeTrigger.Cook: state.cookEnabled = enabled; break;
                case WardrobeTrigger.Doctor: state.doctorEnabled = enabled; break;
                case WardrobeTrigger.Animals: state.animalsEnabled = enabled; break;
            }
        }

        private static void SetPolicy(WardrobePawnState state, WardrobeTrigger trigger, int policyId)
        {
            switch (trigger)
            {
                case WardrobeTrigger.Sleep: state.sleepPolicyId = policyId; break;
                case WardrobeTrigger.Cook: state.cookPolicyId = policyId; break;
                case WardrobeTrigger.Doctor: state.doctorPolicyId = policyId; break;
                case WardrobeTrigger.Animals: state.animalsPolicyId = policyId; break;
            }
        }
    }
}
