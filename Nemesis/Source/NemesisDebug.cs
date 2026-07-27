using System.Collections.Generic;
using System.Text;
using LudeonTK;
using RimWorld;
using RimWorld.Planet;
using UnityEngine;
using Verse;

namespace Nemesis
{
    /// <summary>
    /// Dev-mode helpers (Development Mode on) to exercise hunts without waiting on pacing.
    /// </summary>
    [StaticConstructorOnStartup]
    public static class NemesisDebug
    {
        private const string Cat = "Nemesis";
        private const string ActionsCat = "Nemesis/Actions";

        static NemesisDebug() { }

        private static GameComponent_Nemesis Comp => GameComponent_Nemesis.Instance;

        private static Map PreferMap() =>
            SoftCompat.PreferHarassmentMap(Find.CurrentMap ?? Find.AnyPlayerHomeMap);

        private static bool RequireComp(out GameComponent_Nemesis comp)
        {
            comp = Comp;
            if (comp != null) return true;
            Messages.Message("[Nemesis] No game component (load a save).",
                MessageTypeDefOf.RejectInput, historical: false);
            return false;
        }

        private static bool RequireActiveHunt(out GameComponent_Nemesis comp, out NemesisData data)
        {
            data = null;
            if (!RequireComp(out comp)) return false;
            data = comp.Data;
            if (data != null && data.active) return true;
            Messages.Message("[Nemesis] No active hunt.",
                MessageTypeDefOf.RejectInput, historical: false);
            return false;
        }

        private static void ClearEngaged(GameComponent_Nemesis comp)
        {
            if (comp.IsEngaged)
                comp.EndHunt(NemesisEndReason.Cleared);
        }

        private static Faction PickHostileFaction()
        {
            List<Faction> hostiles = new List<Faction>();
            foreach (Faction f in Find.FactionManager.AllFactions)
            {
                if (f == null || f.IsPlayer || f.defeated || f.Hidden) continue;
                if (!f.HostileTo(Faction.OfPlayer)) continue;
                if (f.def.humanlikeFaction)
                    hostiles.Add(f);
            }
            return hostiles.Count > 0 ? hostiles.RandomElement() : null;
        }

        private static Pawn SelectedHumanlike()
        {
            if (Find.Selector?.SingleSelectedThing is Pawn p
                && p.RaceProps.Humanlike
                && !p.Dead)
                return p;
            return null;
        }

        [DebugAction(Cat, "Log hunt state", allowedGameStates = AllowedGameStates.PlayingOnMap)]
        private static void LogHuntState()
        {
            if (!RequireComp(out GameComponent_Nemesis comp)) return;
            NemesisData d = comp.Data;
            StringBuilder sb = new StringBuilder();
            sb.AppendLine("=== Nemesis ===");
            if (d == null || (!d.active && d.truceUntilTick <= 0))
            {
                sb.AppendLine("idle (no hunt / truce)");
                Log.Message(sb.ToString());
                Messages.Message("[Nemesis] Idle — see log.", MessageTypeDefOf.NeutralEvent, historical: false);
                return;
            }

            Pawn nemesis = comp.FindNemesisPawn();
            Pawn target = comp.FindTargetPawn();
            int now = Find.TickManager.TicksGame;

            sb.AppendLine($"active={d.active} truceUntil={d.truceUntilTick} rogue={d.rogue}");
            sb.AppendLine($"name={d.nemesisName} faction={d.factionName} trigger={d.trigger}");
            sb.AppendLine($"targetMode={d.targetMode} target={d.targetPawnName ?? "-"} (id {d.targetPawnId})");
            sb.AppendLine($"aggression={d.aggressionLevel:F2} effective={d.EffectiveAggression:F2}");
            sb.AppendLine($"escapes={d.escapeCount} harassment={d.harassmentCount} lastAction={d.lastActionKind}");
            sb.AppendLine($"nextAction in {FormatTicks(d.nextActionTick - now)} (tick {d.nextActionTick})");
            sb.AppendLine($"pendingFakeAmbush={d.pendingFakeAmbush} ambushTick={d.fakeAmbushTick}");
            sb.AppendLine($"nemesis pawn: {(nemesis == null ? "MISSING" : DescribePawn(nemesis))}");
            sb.AppendLine($"target pawn: {(target == null ? "-" : DescribePawn(target))}");
            Log.Message(sb.ToString());
            Messages.Message("[Nemesis] Hunt state dumped to log.", MessageTypeDefOf.NeutralEvent, historical: false);
        }

        private static string DescribePawn(Pawn p)
        {
            string where = p.Spawned ? $"{p.Map?.ToString() ?? "?"}@{p.Position}"
                : p.IsWorldPawn() ? "world"
                : p.IsPrisonerOfColony ? "prisoner"
                : "elsewhere";
            return $"{p.LabelShort} id={p.thingIDNumber} dead={p.Dead} downed={p.Downed} {where}";
        }

        private static string FormatTicks(int ticks)
        {
            if (ticks < 0) return "overdue";
            return $"{ticks}t ({ticks / 60000f:F2}d)";
        }

        [DebugAction(Cat, "Clear hunt", allowedGameStates = AllowedGameStates.PlayingOnMap)]
        private static void ClearHunt()
        {
            if (!RequireComp(out GameComponent_Nemesis comp)) return;
            if (!comp.IsEngaged)
            {
                Messages.Message("[Nemesis] Nothing to clear.", MessageTypeDefOf.NeutralEvent, historical: false);
                return;
            }
            ClearEngaged(comp);
            Messages.Message("[Nemesis] Hunt cleared.", MessageTypeDefOf.NeutralEvent, historical: false);
        }

        [DebugAction(Cat, "Start hunt vs colony (selected hostile or random)",
            allowedGameStates = AllowedGameStates.PlayingOnMap)]
        private static void StartHuntColony()
        {
            if (!RequireComp(out GameComponent_Nemesis comp)) return;
            ClearEngaged(comp);

            Pawn selected = SelectedHumanlike();
            if (selected != null && selected.Faction != null && !selected.Faction.IsPlayer
                && selected.Faction.HostileTo(Faction.OfPlayer))
            {
                comp.CreateNemesis(selected, NemesisTargetMode.Colony, NemesisTrigger.FactionRetaliation,
                    useAsNemesis: selected);
                Messages.Message($"[Nemesis] Hunt started — {selected.LabelShort} vs colony.",
                    MessageTypeDefOf.NeutralEvent, historical: false);
                return;
            }

            Faction faction = PickHostileFaction();
            if (faction == null)
            {
                Messages.Message("[Nemesis] No hostile humanlike faction.",
                    MessageTypeDefOf.RejectInput, historical: false);
                return;
            }

            PawnKindDef kind = faction.RandomPawnKind();
            if (kind == null || !kind.RaceProps.Humanlike)
                kind = PawnKindDefOf.SpaceRefugee;
            Pawn seed = PawnGenerator.GeneratePawn(new PawnGenerationRequest(
                kind, faction, PawnGenerationContext.NonPlayer,
                forceGenerateNewPawn: true, canGeneratePawnRelations: false, allowDead: false));
            // CreateNemesis with useAsNemesis keeps this pawn; without it would generate another.
            comp.CreateNemesis(seed, NemesisTargetMode.Colony, NemesisTrigger.FactionRetaliation,
                useAsNemesis: seed);
            Messages.Message($"[Nemesis] Hunt started — {seed.LabelShort} ({faction.Name}) vs colony.",
                MessageTypeDefOf.NeutralEvent, historical: false);
        }

        [DebugAction(Cat, "Start hunt targeting selected colonist",
            allowedGameStates = AllowedGameStates.PlayingOnMap,
            actionType = DebugActionType.ToolMapForPawns)]
        private static void StartHuntTargetPawn(Pawn colonist)
        {
            if (colonist == null || !colonist.RaceProps.Humanlike) return;
            if (colonist.Faction != Faction.OfPlayer && !colonist.IsColonist)
            {
                Messages.Message("[Nemesis] Select a colonist.",
                    MessageTypeDefOf.RejectInput, historical: false);
                return;
            }
            if (!RequireComp(out GameComponent_Nemesis comp)) return;
            ClearEngaged(comp);

            Faction faction = PickHostileFaction();
            if (faction == null)
            {
                Messages.Message("[Nemesis] No hostile humanlike faction.",
                    MessageTypeDefOf.RejectInput, historical: false);
                return;
            }

            PawnKindDef kind = faction.RandomPawnKind();
            if (kind == null || !kind.RaceProps.Humanlike)
                kind = PawnKindDefOf.SpaceRefugee;
            Pawn seed = PawnGenerator.GeneratePawn(new PawnGenerationRequest(
                kind, faction, PawnGenerationContext.NonPlayer,
                forceGenerateNewPawn: true, canGeneratePawnRelations: false, allowDead: false));

            comp.CreateNemesis(seed, NemesisTargetMode.Pawn, NemesisTrigger.Fixation,
                targetPawn: colonist, useAsNemesis: seed);
            Messages.Message($"[Nemesis] Hunt started — {seed.LabelShort} fixates on {colonist.LabelShort}.",
                MessageTypeDefOf.NeutralEvent, historical: false);
        }

        [DebugAction(Cat, "Make selected hostile the nemesis (colony target)",
            allowedGameStates = AllowedGameStates.PlayingOnMap,
            actionType = DebugActionType.ToolMapForPawns)]
        private static void MakeSelectedNemesis(Pawn pawn)
        {
            if (pawn == null || !pawn.RaceProps.Humanlike || pawn.Dead) return;
            if (pawn.Faction == null || pawn.Faction.IsPlayer || !pawn.Faction.HostileTo(Faction.OfPlayer))
            {
                Messages.Message("[Nemesis] Need a hostile humanlike.",
                    MessageTypeDefOf.RejectInput, historical: false);
                return;
            }
            if (!RequireComp(out GameComponent_Nemesis comp)) return;
            ClearEngaged(comp);
            comp.CreateNemesis(pawn, NemesisTargetMode.Colony, NemesisTrigger.FactionRetaliation,
                useAsNemesis: pawn);
            Messages.Message($"[Nemesis] {pawn.LabelShort} is now the nemesis.",
                MessageTypeDefOf.NeutralEvent, historical: false);
        }

        [DebugAction(Cat, "Fire next action now", allowedGameStates = AllowedGameStates.PlayingOnMap)]
        private static void FireNextNow()
        {
            if (!RequireActiveHunt(out GameComponent_Nemesis comp, out NemesisData data)) return;
            data.nextActionTick = Find.TickManager.TicksGame;
            Messages.Message("[Nemesis] Next action armed for this tick.",
                MessageTypeDefOf.NeutralEvent, historical: false);
        }

        [DebugAction(Cat, "Bump aggression +1", allowedGameStates = AllowedGameStates.PlayingOnMap)]
        private static void BumpAggression()
        {
            if (!RequireActiveHunt(out _, out NemesisData data)) return;
            data.aggressionLevel = Mathf.Min(data.aggressionLevel + 1f, 10f);
            Messages.Message($"[Nemesis] Aggression → {data.aggressionLevel:F1} (eff {data.EffectiveAggression:F1}).",
                MessageTypeDefOf.NeutralEvent, historical: false);
        }

        [DebugAction(Cat, "Max aggression", allowedGameStates = AllowedGameStates.PlayingOnMap)]
        private static void MaxAggression()
        {
            if (!RequireActiveHunt(out _, out NemesisData data)) return;
            data.aggressionLevel = 10f;
            Messages.Message($"[Nemesis] Aggression maxed (eff {data.EffectiveAggression:F1}).",
                MessageTypeDefOf.NeutralEvent, historical: false);
        }

        [DebugAction(Cat, "Force escape (spawned nemesis)", allowedGameStates = AllowedGameStates.PlayingOnMap)]
        private static void ForceEscape()
        {
            if (!RequireActiveHunt(out GameComponent_Nemesis comp, out _)) return;
            Pawn nemesis = comp.FindNemesisPawn();
            if (nemesis == null || !nemesis.Spawned || nemesis.Dead)
            {
                Messages.Message("[Nemesis] Need a spawned living nemesis on the map.",
                    MessageTypeDefOf.RejectInput, historical: false);
                return;
            }
            comp.HandleLethalDamage(nemesis);
            Messages.Message("[Nemesis] Escape / subdue path fired.",
                MessageTypeDefOf.NeutralEvent, historical: false);
        }

        [DebugAction(Cat, "Open resolution dialog (if prisoner)", allowedGameStates = AllowedGameStates.PlayingOnMap)]
        private static void OpenResolution()
        {
            if (!RequireComp(out GameComponent_Nemesis comp)) return;
            NemesisData data = comp.Data;
            Pawn nemesis = comp.FindNemesisPawn();
            if (data == null || nemesis == null || !nemesis.IsPrisonerOfColony)
            {
                Messages.Message("[Nemesis] Nemesis must be a colony prisoner.",
                    MessageTypeDefOf.RejectInput, historical: false);
                return;
            }
            data.active = false;
            Find.WindowStack.Add(new Dialog_NemesisResolution(data, nemesis));
        }

        [DebugAction(Cat, "Resolve pending fake ambush now", allowedGameStates = AllowedGameStates.PlayingOnMap)]
        private static void ResolveFakeAmbush()
        {
            if (!RequireActiveHunt(out _, out NemesisData data)) return;
            if (!data.pendingFakeAmbush)
            {
                Messages.Message("[Nemesis] No pending fake ambush.",
                    MessageTypeDefOf.RejectInput, historical: false);
                return;
            }
            Map map = PreferMap();
            if (map == null)
            {
                Messages.Message("[Nemesis] No map.", MessageTypeDefOf.RejectInput, historical: false);
                return;
            }
            NemesisActions.ResolvePendingFakeAmbush(data, map);
        }

        private static void FireAction(NemesisAction action)
        {
            if (!RequireActiveHunt(out _, out NemesisData data)) return;
            Map map = PreferMap();
            if (map == null)
            {
                Messages.Message("[Nemesis] No map.", MessageTypeDefOf.RejectInput, historical: false);
                return;
            }
            NemesisActions.Execute(action, data, map);
            data.nextActionTick = Find.TickManager.TicksGame + 60000;
            Messages.Message($"[Nemesis] Fired {action}.", MessageTypeDefOf.NeutralEvent, historical: false);
        }

        [DebugAction(ActionsCat, "Comms taunt", allowedGameStates = AllowedGameStates.PlayingOnMap)]
        private static void ActionTaunt() => FireAction(NemesisAction.CommsTaunt);

        [DebugAction(ActionsCat, "Direct raid", allowedGameStates = AllowedGameStates.PlayingOnMap)]
        private static void ActionRaid() => FireAction(NemesisAction.DirectRaid);

        [DebugAction(ActionsCat, "Nemesis assault", allowedGameStates = AllowedGameStates.PlayingOnMap)]
        private static void ActionAssault() => FireAction(NemesisAction.NemesisAssault);

        [DebugAction(ActionsCat, "Waste pack drop", allowedGameStates = AllowedGameStates.PlayingOnMap)]
        private static void ActionWaste() => FireAction(NemesisAction.WastePackDrop);

        [DebugAction(ActionsCat, "Fake signal ambush", allowedGameStates = AllowedGameStates.PlayingOnMap)]
        private static void ActionFake() => FireAction(NemesisAction.FakeSignalAmbush);

        [DebugAction(ActionsCat, "Caravan harass", allowedGameStates = AllowedGameStates.PlayingOnMap)]
        private static void ActionCaravan() => FireAction(NemesisAction.CaravanHarass);

        [DebugAction(ActionsCat, "Power sabotage", allowedGameStates = AllowedGameStates.PlayingOnMap)]
        private static void ActionSabotage() => FireAction(NemesisAction.PowerSabotage);

        [DebugAction(ActionsCat, "Food store raid", allowedGameStates = AllowedGameStates.PlayingOnMap)]
        private static void ActionFood() => FireAction(NemesisAction.FoodStoreRaid);

        [DebugAction(ActionsCat, "Anomaly bait", allowedGameStates = AllowedGameStates.PlayingOnMap)]
        private static void ActionAnomaly() => FireAction(NemesisAction.AnomalyBait);
    }
}
