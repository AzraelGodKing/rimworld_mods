using System.Collections.Generic;
using RimWorld;
using UnityEngine;
using Verse;

namespace ShiftChange
{
    public class MainTabWindow_ShiftChange : MainTabWindow
    {
        private Vector2 scroll;
        private Pawn selected;

        public override Vector2 RequestedTabSize => new Vector2(980f, 560f);

        public override void DoWindowContents(Rect inRect)
        {
            Text.Font = GameFont.Medium;
            Widgets.Label(new Rect(inRect.x, inRect.y, inRect.width, 32f),
                "ShiftChange_TabTitle".Translate());
            Text.Font = GameFont.Small;

            Widgets.Label(new Rect(inRect.x, inRect.y + 34f, inRect.width, 22f),
                "ShiftChange_TabIntro".Translate());

            GameComponent_ShiftChange comp = GameComponent_ShiftChange.Get;
            if (comp == null)
            {
                Widgets.Label(new Rect(inRect.x, inRect.y + 70f, inRect.width, 40f),
                    "ShiftChange_TabNoGame".Translate());
                return;
            }

            if (ShiftChangeMod.Settings != null && !ShiftChangeMod.Settings.enabled)
            {
                Widgets.Label(new Rect(inRect.x, inRect.y + 70f, inRect.width, 40f),
                    "ShiftChange_TabDisabled".Translate());
            }

            float y = inRect.y + 64f;
            float listW = inRect.width * 0.34f;
            Rect listOut = new Rect(inRect.x, y, listW, inRect.yMax - y);
            Rect detailOut = new Rect(inRect.x + listW + 12f, y, inRect.width - listW - 12f, inRect.yMax - y);

            List<Pawn> colonists = Colonists();
            float rowH = 28f;
            Rect listView = new Rect(0, 0, listOut.width - 16f, Mathf.Max(colonists.Count * rowH, listOut.height));
            Widgets.BeginScrollView(listOut, ref scroll, listView);
            float ry = 0f;
            for (int i = 0; i < colonists.Count; i++)
            {
                Pawn p = colonists[i];
                Rect row = new Rect(0, ry, listView.width, rowH - 2f);
                if (selected == p)
                {
                    Widgets.DrawHighlightSelected(row);
                }
                else if (Mouse.IsOver(row))
                {
                    Widgets.DrawHighlight(row);
                }

                ShiftChangeRule rule = comp.FindRule(p.thingIDNumber, ShiftChangeTriggerKind.Sleep);
                string mark = rule != null && rule.enabled ? "● " : "○ ";
                Widgets.Label(new Rect(row.x + 4f, row.y + 4f, row.width - 8f, row.height - 4f),
                    mark + p.LabelShortCap);

                if (Widgets.ButtonInvisible(row))
                {
                    selected = p;
                }

                ry += rowH;
            }

            Widgets.EndScrollView();

            Widgets.DrawMenuSection(detailOut);
            Rect inner = detailOut.ContractedBy(10f);
            if (selected == null || selected.Destroyed || !selected.Spawned)
            {
                selected = null;
                Widgets.Label(inner, "ShiftChange_TabSelectPawn".Translate());
                return;
            }

            DrawPawnDetail(inner, selected, comp);
        }

        private static void DrawPawnDetail(Rect inner, Pawn pawn, GameComponent_ShiftChange comp)
        {
            ShiftChangeRule rule = comp.GetOrCreateSleepRule(pawn);
            float y = inner.y;

            Text.Font = GameFont.Medium;
            Widgets.Label(new Rect(inner.x, y, inner.width, 28f), pawn.LabelCap);
            Text.Font = GameFont.Small;
            y += 32f;

            Widgets.CheckboxLabeled(
                new Rect(inner.x, y, inner.width, 24f),
                "ShiftChange_Rule_SleepEnabled".Translate(),
                ref rule.enabled);
            y += 30f;

            Widgets.CheckboxLabeled(
                new Rect(inner.x, y, inner.width, 24f),
                "ShiftChange_Rule_Replace".Translate(),
                ref rule.replaceMode);
            y += 30f;

            Widgets.Label(new Rect(inner.x, y, inner.width, 22f),
                "ShiftChange_Rule_Policy".Translate(rule.apparelPolicyName ?? "—"));
            y += 24f;

            if (Widgets.ButtonText(new Rect(inner.x, y, 220f, 28f),
                    "ShiftChange_Rule_PickPolicy".Translate()))
            {
                List<FloatMenuOption> opts = new List<FloatMenuOption>();
                opts.Add(new FloatMenuOption("ShiftChange_Rule_ClearPolicy".Translate(), () =>
                {
                    rule.apparelPolicyName = null;
                }));

                if (Current.Game?.outfitDatabase != null)
                {
                    List<ApparelPolicy> policies = Current.Game.outfitDatabase.AllOutfits;
                    for (int i = 0; i < policies.Count; i++)
                    {
                        ApparelPolicy pol = policies[i];
                        if (pol == null)
                        {
                            continue;
                        }

                        string label = pol.label;
                        opts.Add(new FloatMenuOption(label, () =>
                        {
                            rule.apparelPolicyName = label;
                        }));
                    }
                }

                Find.WindowStack.Add(new FloatMenu(opts));
            }

            y += 36f;

            Zone_Stockpile currentZone = FindZoneById(pawn.Map, rule.wardrobeZoneId);
            string zoneLabel = currentZone != null
                ? currentZone.label
                : "ShiftChange_Rule_ZoneAuto".Translate().ToString();
            Widgets.Label(new Rect(inner.x, y, inner.width, 22f),
                "ShiftChange_Rule_Zone".Translate(zoneLabel));
            y += 24f;

            if (Widgets.ButtonText(new Rect(inner.x, y, 220f, 28f),
                    "ShiftChange_Rule_PickZone".Translate()))
            {
                List<FloatMenuOption> opts = new List<FloatMenuOption>();
                opts.Add(new FloatMenuOption("ShiftChange_Rule_ZoneAuto".Translate(), () =>
                {
                    rule.wardrobeZoneId = -1;
                }));

                List<Zone> zones = pawn.Map?.zoneManager?.AllZones;
                if (zones != null)
                {
                    for (int i = 0; i < zones.Count; i++)
                    {
                        if (zones[i] is Zone_Stockpile stock)
                        {
                            Zone_Stockpile local = stock;
                            opts.Add(new FloatMenuOption(local.label, () =>
                            {
                                rule.wardrobeZoneId = local.ID;
                            }));
                        }
                    }
                }

                Find.WindowStack.Add(new FloatMenu(opts));
            }

            y += 40f;
            PawnShiftState state = comp.GetState(pawn.thingIDNumber);
            string status = state == null
                ? "ShiftChange_Status_Idle".Translate()
                : state.managed
                    ? "ShiftChange_Status_Managed".Translate()
                    : state.wantsRestore
                        ? "ShiftChange_Status_WantsRestore".Translate()
                        : "ShiftChange_Status_Idle".Translate();
            Widgets.Label(new Rect(inner.x, y, inner.width, 22f), status);
            y += 28f;

            if (Widgets.ButtonText(new Rect(inner.x, y, 180f, 28f),
                    "ShiftChange_ForceApply".Translate()))
            {
                if (rule.ResolvePolicy() == null)
                {
                    Messages.Message("ShiftChange_Msg_NeedPolicy".Translate(),
                        MessageTypeDefOf.RejectInput, historical: false);
                }
                else
                {
                    state = comp.GetOrCreateState(pawn.thingIDNumber);
                    state.snapshotApparelIds = ShiftChangeUtility.SnapshotWornApparelIds(pawn);
                    state.activeRuleId = rule.ruleId;
                    state.managed = true;
                    state.lastSwapTick = Find.TickManager.TicksGame;
                    ShiftChangeUtility.TryStartApplyJob(pawn, rule);
                }
            }

            if (Widgets.ButtonText(new Rect(inner.x + 190f, y, 180f, 28f),
                    "ShiftChange_ForceRestore".Translate()))
            {
                ShiftChangeUtility.TryStartRestoreJob(pawn, rule);
            }
        }

        private static Zone_Stockpile FindZoneById(Map map, int id)
        {
            if (map == null || id < 0)
            {
                return null;
            }

            List<Zone> zones = map.zoneManager?.AllZones;
            if (zones == null)
            {
                return null;
            }

            for (int i = 0; i < zones.Count; i++)
            {
                if (zones[i] is Zone_Stockpile stock && stock.ID == id)
                {
                    return stock;
                }
            }

            return null;
        }

        private static List<Pawn> Colonists()
        {
            List<Pawn> list = new List<Pawn>();
            List<Map> maps = Find.Maps;
            for (int m = 0; m < maps.Count; m++)
            {
                List<Pawn> pawns = maps[m]?.mapPawns?.FreeColonistsSpawned;
                if (pawns == null)
                {
                    continue;
                }

                for (int i = 0; i < pawns.Count; i++)
                {
                    if (pawns[i] != null && !list.Contains(pawns[i]))
                    {
                        list.Add(pawns[i]);
                    }
                }
            }

            list.SortBy(p => p.LabelShort);
            return list;
        }
    }
}
