using System.Collections.Generic;
using RimWorld;
using UnityEngine;
using Verse;

namespace ShiftChange
{
    public class MainTabWindow_ShiftChange : MainTabWindow
    {
        private Vector2 listScroll;
        private Vector2 detailScroll;
        private Pawn selected;

        public override Vector2 RequestedTabSize => new Vector2(1040f, 640f);

        public override void DoWindowContents(Rect inRect)
        {
            Text.Font = GameFont.Medium;
            Widgets.Label(new Rect(inRect.x, inRect.y, inRect.width, 32f),
                "ShiftChange_TabTitle".Translate());
            Text.Font = GameFont.Small;

            Widgets.Label(new Rect(inRect.x, inRect.y + 34f, inRect.width, 36f),
                "ShiftChange_TabIntro".Translate());

            GameComponent_ShiftChange comp = GameComponent_ShiftChange.Get;
            if (comp == null)
            {
                Widgets.Label(new Rect(inRect.x, inRect.y + 80f, inRect.width, 40f),
                    "ShiftChange_TabNoGame".Translate());
                return;
            }

            if (ShiftChangeMod.Settings != null && !ShiftChangeMod.Settings.enabled)
            {
                Widgets.Label(new Rect(inRect.x, inRect.y + 72f, inRect.width, 22f),
                    "ShiftChange_TabDisabled".Translate());
            }

            float y = inRect.y + 78f;
            float listW = inRect.width * 0.30f;
            Rect listOut = new Rect(inRect.x, y, listW, inRect.yMax - y);
            Rect detailOut = new Rect(inRect.x + listW + 12f, y, inRect.width - listW - 12f, inRect.yMax - y);

            List<Pawn> colonists = Colonists();
            float rowH = 28f;
            Rect listView = new Rect(0, 0, listOut.width - 16f, Mathf.Max(colonists.Count * rowH, listOut.height));
            Widgets.BeginScrollView(listOut, ref listScroll, listView);
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

                int enabledCount = 0;
                List<ShiftChangeRule> pawnRules = comp.RulesForPawn(p.thingIDNumber);
                for (int r = 0; r < pawnRules.Count; r++)
                {
                    if (pawnRules[r].enabled)
                    {
                        enabledCount++;
                    }
                }

                string mark = enabledCount > 0 ? $"●{enabledCount} " : "○ ";
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

        private void DrawPawnDetail(Rect inner, Pawn pawn, GameComponent_ShiftChange comp)
        {
            float viewH = 920f;
            Rect view = new Rect(0, 0, inner.width - 16f, viewH);
            Widgets.BeginScrollView(inner, ref detailScroll, view);

            float y = 0f;
            float width = view.width;

            Text.Font = GameFont.Medium;
            Widgets.Label(new Rect(0, y, width, 28f), pawn.LabelCap);
            Text.Font = GameFont.Small;
            y += 32f;

            PawnShiftState state = comp.GetState(pawn.thingIDNumber);
            string status = StatusLabel(state, comp);
            Widgets.Label(new Rect(0, y, width, 22f), status);
            y += 28f;

            y = DrawRuleBlock(0, y, width, pawn, comp.GetOrCreateSleepRule(pawn),
                "ShiftChange_Rule_SleepTitle".Translate());

            for (int i = 0; i < ShiftChangeUtility.DefaultWorkTypeDefNames.Length; i++)
            {
                string defName = ShiftChangeUtility.DefaultWorkTypeDefNames[i];
                WorkTypeDef wt = DefDatabase<WorkTypeDef>.GetNamedSilentFail(defName);
                string title = wt != null
                    ? "ShiftChange_Rule_WorkTitle".Translate(wt.labelShort)
                    : "ShiftChange_Rule_WorkTitle".Translate(defName);
                ShiftChangeRule workRule = comp.GetOrCreateWorkRule(pawn, defName);
                y = DrawRuleBlock(0, y, width, pawn, workRule, title);
            }

            y = DrawRuleBlock(0, y, width, pawn, comp.GetOrCreateRitualRule(pawn),
                "ShiftChange_Rule_RitualTitle".Translate());

            y += 8f;
            if (Widgets.ButtonText(new Rect(0, y, 180f, 28f), "ShiftChange_ForceApply".Translate()))
            {
                ShiftChangeRule active = state != null
                    ? comp.FindRuleById(state.activeRuleId)
                    : null;
                active ??= comp.FindDesiredRule(pawn, ShiftChangeMod.Settings, comp.GetOrCreateState(pawn.thingIDNumber))
                    ?? comp.GetOrCreateSleepRule(pawn);
                ForceApply(pawn, comp, active);
            }

            if (Widgets.ButtonText(new Rect(190f, y, 180f, 28f), "ShiftChange_ForceRestore".Translate()))
            {
                ShiftChangeRule rule = state != null
                    ? comp.FindRuleById(state.activeRuleId)
                    : null;
                rule ??= comp.FindAnyRuleForPawn(pawn.thingIDNumber);
                ShiftChangeUtility.TryStartRestoreJob(pawn, rule);
            }

            Widgets.EndScrollView();
        }

        private static float DrawRuleBlock(float x, float y, float width, Pawn pawn, ShiftChangeRule rule, string title)
        {
            Widgets.DrawMenuSection(new Rect(x, y, width, 168f));
            Rect block = new Rect(x + 8f, y + 6f, width - 16f, 156f);
            float by = block.y;

            Text.Font = GameFont.Small;
            Widgets.Label(new Rect(block.x, by, block.width, 22f), title);
            by += 24f;

            Widgets.CheckboxLabeled(
                new Rect(block.x, by, block.width, 24f),
                "ShiftChange_Rule_Enabled".Translate(),
                ref rule.enabled);
            by += 26f;

            Widgets.CheckboxLabeled(
                new Rect(block.x, by, block.width, 24f),
                "ShiftChange_Rule_Replace".Translate(),
                ref rule.replaceMode);
            by += 26f;

            string policyName = string.IsNullOrEmpty(rule.apparelPolicyName)
                ? "—"
                : rule.apparelPolicyName;
            Widgets.Label(new Rect(block.x, by, block.width - 230f, 22f),
                "ShiftChange_Rule_Policy".Translate(policyName));
            if (Widgets.ButtonText(new Rect(block.xMax - 220f, by - 2f, 220f, 26f),
                    "ShiftChange_Rule_PickPolicy".Translate()))
            {
                OpenPolicyMenu(rule);
            }

            by += 28f;

            Zone_Stockpile currentZone = FindZoneById(pawn.Map, rule.wardrobeZoneId);
            string zoneLabel = currentZone != null
                ? currentZone.label
                : "ShiftChange_Rule_ZoneAuto".Translate().ToString();
            Widgets.Label(new Rect(block.x, by, block.width - 230f, 22f),
                "ShiftChange_Rule_Zone".Translate(zoneLabel));
            if (Widgets.ButtonText(new Rect(block.xMax - 220f, by - 2f, 220f, 26f),
                    "ShiftChange_Rule_PickZone".Translate()))
            {
                OpenZoneMenu(pawn, rule);
            }

            return y + 176f;
        }

        private static void OpenPolicyMenu(ShiftChangeRule rule)
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

        private static void OpenZoneMenu(Pawn pawn, ShiftChangeRule rule)
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

        private static void ForceApply(Pawn pawn, GameComponent_ShiftChange comp, ShiftChangeRule rule)
        {
            if (rule == null || rule.ResolvePolicy() == null)
            {
                Messages.Message("ShiftChange_Msg_NeedPolicy".Translate(),
                    MessageTypeDefOf.RejectInput, historical: false);
                return;
            }

            PawnShiftState state = comp.GetOrCreateState(pawn.thingIDNumber);
            if (!state.managed || state.snapshotApparelIds == null || state.snapshotApparelIds.Count == 0)
            {
                state.snapshotApparelIds = ShiftChangeUtility.SnapshotWornApparelIds(pawn);
            }

            state.activeRuleId = rule.ruleId;
            state.managed = true;
            state.lastSwapTick = Find.TickManager.TicksGame;
            ShiftChangeUtility.TryStartApplyJob(pawn, rule);
        }

        private static string StatusLabel(PawnShiftState state, GameComponent_ShiftChange comp)
        {
            if (state == null)
            {
                return "ShiftChange_Status_Idle".Translate();
            }

            if (state.managed)
            {
                ShiftChangeRule rule = comp.FindRuleById(state.activeRuleId);
                string name = rule?.LabelShort() ?? "?";
                return "ShiftChange_Status_ManagedNamed".Translate(name);
            }

            if (state.wantsRestore)
            {
                return "ShiftChange_Status_WantsRestore".Translate();
            }

            return "ShiftChange_Status_Idle".Translate();
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
