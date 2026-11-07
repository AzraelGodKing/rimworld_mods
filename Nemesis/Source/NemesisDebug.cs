#nullable enable
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

        private static GameComponent_Nemesis? Comp => GameComponent_Nemesis.Instance;

        private static Map? PreferMap() =>
            SoftCompat.PreferHarassmentMap(Find.CurrentMap ?? Find.AnyPlayerHomeMap);

        private static bool RequireComp(out GameComponent_Nemesis comp)
        {
            GameComponent_Nemesis? found = Comp;
            comp = found!;
            if (found != null) return true;
            Messages.Message("[Nemesis] No game component (load a save).",
                MessageTypeDefOf.RejectInput, historical: false);
            return false;
        }

        /// <summary>The hunt picked in the dossier, else the first engaged one.</summary>
        private static NemesisData? DebugHunt(GameComponent_Nemesis comp)
        {
            NemesisData? picked = comp.HuntById(MainTabWindow_NemesisDossier.SelectedHuntId);
            if (picked != null && picked.Engaged) return picked;
            IReadOnlyList<NemesisData> hunts = comp.Hunts;
            for (int i = 0; i < hunts.Count; i++)
            {
                if (hunts[i].Engaged) return hunts[i];
            }
            return null;
        }

        private static bool RequireActiveHunt(out GameComponent_Nemesis comp, out NemesisData data)
        {
            data = null!;
            if (!RequireComp(out comp)) return false;
            NemesisData? hunt = DebugHunt(comp);
            data = hunt!;
            if (hunt != null && hunt.active) return true;
            Messages.Message("[Nemesis] No active hunt.",
                MessageTypeDefOf.RejectInput, historical: false);
            return false;
        }

        private static void Started(NemesisData? h, string what)
        {
            if (h != null)
                Messages.Message("[Nemesis] Hunt started — " + what + ".", MessageTypeDefOf.NeutralEvent, historical: false);
            else
                Messages.Message("[Nemesis] No hunt started: that faction already has one, or the pawn is claimed.",
                    MessageTypeDefOf.RejectInput, historical: false);
        }

        /// <summary>A hostile humanlike faction with no hunt yet.</summary>
        private static Faction? PickHostileFaction(GameComponent_Nemesis comp)
        {
            List<Faction> hostiles = new List<Faction>();
            foreach (Faction f in Find.FactionManager.AllFactions)
            {
                if (f == null || f.IsPlayer || f.defeated || f.Hidden) continue;
                if (!f.HostileTo(Faction.OfPlayer)) continue;
                if (HasHunt(comp, f)) continue;
                if (f.def.humanlikeFaction)
                    hostiles.Add(f);
            }
            return hostiles.Count > 0 ? hostiles.RandomElement() : null;
        }

        private static bool HasHunt(GameComponent_Nemesis comp, Faction f)
        {
            IReadOnlyList<NemesisData> hunts = comp.Hunts;
            for (int i = 0; i < hunts.Count; i++)
            {
                if (hunts[i].Engaged && hunts[i].faction == f) return true;
            }
            return false;
        }

        private static Pawn? SelectedHumanlike()
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
            StringBuilder sb = new StringBuilder();
            sb.AppendLine("=== Nemesis ===");
            IReadOnlyList<NemesisData> hunts = comp.Hunts;
            if (hunts.Count == 0)
            {
                sb.AppendLine("idle (no hunt / truce)");
                Log.Message(sb.ToString());
                Messages.Message("[Nemesis] Idle — see log.", MessageTypeDefOf.NeutralEvent, historical: false);
                return;
            }
            int now = Find.TickManager.TicksGame;
            sb.AppendLine($"hunts={hunts.Count} lastThreat={FormatTicks(now - comp.LastThreatTick)} ago");
            for (int i = 0; i < hunts.Count; i++)
                AppendHunt(sb, comp, hunts[i], now);
            Log.Message(sb.ToString());
            Messages.Message("[Nemesis] Hunt state dumped to log.", MessageTypeDefOf.NeutralEvent, historical: false);
        }

        private static void AppendHunt(StringBuilder sb, GameComponent_Nemesis comp, NemesisData d, int now)
        {
            Pawn? nemesis = comp.FindNemesisPawnFor(d);
            Pawn? target = comp.FindTargetPawn(d);
            sb.AppendLine($"--- hunt #{d.huntId}");

            sb.AppendLine($"active={d.active} truceUntil={d.truceUntilTick} rogue={d.rogue}");
            sb.AppendLine($"name={d.nemesisName} faction={d.factionName} trigger={d.trigger}");
            sb.AppendLine($"targetMode={d.targetMode} target={d.targetPawnName ?? "-"} (id {d.targetPawnId})");
            sb.AppendLine($"aggression={d.aggressionLevel:F2} effective={d.EffectiveAggression:F2}");
            sb.AppendLine($"escapes={d.escapeCount} harassment={d.harassmentCount} lastAction={d.lastActionKind}");
            sb.AppendLine($"focus={d.combatFocus} progression={d.progressionLevel} applied={d.appliedProgressionLevel} mount={d.mountKindDefName ?? "-"}");
            sb.AppendLine($"nextAction in {FormatTicks(d.nextActionTick - now)} (tick {d.nextActionTick})");
            sb.AppendLine($"pendingFakeAmbush={d.pendingFakeAmbush} ambushTick={d.fakeAmbushTick}");
            sb.AppendLine($"nemesis pawn: {(nemesis == null ? "MISSING" : DescribePawn(nemesis))}");
            sb.AppendLine($"target pawn: {(target == null ? "-" : DescribePawn(target))}");
        }

        [DebugAction(Cat, "Force captain level-up", allowedGameStates = AllowedGameStates.PlayingOnMap)]
        private static void ForceLevelUp()
        {
            if (!RequireActiveHunt(out GameComponent_Nemesis comp, out NemesisData data)) return;
            Pawn? nemesis = comp.FindNemesisPawnFor(data);
            if (nemesis == null)
            {
                Messages.Message("[Nemesis] Nemesis pawn missing.", MessageTypeDefOf.RejectInput, historical: false);
                return;
            }
            NemesisProgression.LevelUpOnEscape(data, nemesis);
            Messages.Message(
                $"[Nemesis] Progression → {data.progressionLevel} ({data.combatFocus}).",
                MessageTypeDefOf.NeutralEvent, historical: false);
        }

        [DebugAction(Cat, "Cycle combat focus + reapply", allowedGameStates = AllowedGameStates.PlayingOnMap)]
        private static void CycleFocus()
        {
            if (!RequireActiveHunt(out GameComponent_Nemesis comp, out NemesisData data)) return;
            Pawn? nemesis = comp.FindNemesisPawnFor(data);
            if (nemesis == null)
            {
                Messages.Message("[Nemesis] Nemesis pawn missing.", MessageTypeDefOf.RejectInput, historical: false);
                return;
            }
            NemesisCombatFocus[] foci = (NemesisCombatFocus[])System.Enum.GetValues(typeof(NemesisCombatFocus));
            int next = (System.Array.IndexOf(foci, data.combatFocus) + 1) % foci.Length;
            if (foci[next] == NemesisCombatFocus.Mechanitor && !ModsConfig.BiotechActive)
                next = (next + 1) % foci.Length;
            data.combatFocus = foci[next];
            data.appliedProgressionLevel = -1;
            NemesisProgression.Apply(nemesis, data);
            Messages.Message($"[Nemesis] Focus → {data.combatFocus}.", MessageTypeDefOf.NeutralEvent, historical: false);
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

        [DebugAction(Cat, "Clear all hunts", allowedGameStates = AllowedGameStates.PlayingOnMap)]
        private static void ClearHunt()
        {
            if (!RequireComp(out GameComponent_Nemesis comp)) return;
            if (!comp.IsEngaged)
            {
                Messages.Message("[Nemesis] Nothing to clear.", MessageTypeDefOf.NeutralEvent, historical: false);
                return;
            }
            comp.EndAllHunts(NemesisEndReason.Cleared);
            Messages.Message("[Nemesis] Hunts cleared.", MessageTypeDefOf.NeutralEvent, historical: false);
        }

        [DebugAction(Cat, "Start hunt vs colony (selected hostile or random)",
            allowedGameStates = AllowedGameStates.PlayingOnMap)]
        private static void StartHuntColony()
        {
            if (!RequireComp(out GameComponent_Nemesis comp)) return;

            Pawn? selected = SelectedHumanlike();
            if (selected != null && selected.Faction != null && !selected.Faction.IsPlayer
                && selected.Faction.HostileTo(Faction.OfPlayer))
            {
                Started(comp.CreateNemesis(selected, NemesisTargetMode.Colony, NemesisTrigger.FactionRetaliation,
                    useAsNemesis: selected, ignoreLimits: true), selected.LabelShort + " vs colony");
                return;
            }

            Faction? faction = PickHostileFaction(comp);
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
            Started(comp.CreateNemesis(seed, NemesisTargetMode.Colony, NemesisTrigger.FactionRetaliation,
                useAsNemesis: seed, ignoreLimits: true), $"{seed.LabelShort} ({faction.Name}) vs colony");
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

            Faction? faction = PickHostileFaction(comp);
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

            Started(comp.CreateNemesis(seed, NemesisTargetMode.Pawn, NemesisTrigger.Fixation,
                targetPawn: colonist, useAsNemesis: seed, ignoreLimits: true),
                $"{seed.LabelShort} fixates on {colonist.LabelShort}");
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
            Started(comp.CreateNemesis(pawn, NemesisTargetMode.Colony, NemesisTrigger.FactionRetaliation,
                useAsNemesis: pawn, ignoreLimits: true), pawn.LabelShort + " is now a nemesis");
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
            if (!RequireActiveHunt(out GameComponent_Nemesis comp, out NemesisData hunt)) return;
            Pawn? nemesis = comp.FindNemesisPawnFor(hunt);
            if (nemesis == null || !nemesis.Spawned || nemesis.Dead)
            {
                Messages.Message("[Nemesis] Need a spawned living nemesis on the map.",
                    MessageTypeDefOf.RejectInput, historical: false);
                return;
            }
            comp.HandleLethalDamage(hunt, nemesis);
            Messages.Message("[Nemesis] Escape / subdue path fired.",
                MessageTypeDefOf.NeutralEvent, historical: false);
        }

        [DebugAction(Cat, "Open resolution dialog (if prisoner)", allowedGameStates = AllowedGameStates.PlayingOnMap)]
        private static void OpenResolution()
        {
            if (!RequireComp(out GameComponent_Nemesis comp)) return;
            NemesisData? data = DebugHunt(comp);
            Pawn? nemesis = comp.FindNemesisPawnFor(data);
            if (data == null || nemesis == null || !nemesis.IsPrisonerOfColony)
            {
                Messages.Message("[Nemesis] Nemesis must be a colony prisoner.",
                    MessageTypeDefOf.RejectInput, historical: false);
                return;
            }
            data.active = false;
            data.pendingResolution = true;
            comp.TryOpenResolutionDialog(data, nemesis);
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
            Map? map = PreferMap();
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
            Map? map = PreferMap();
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

        [DebugAction(ActionsCat, "Do nothing", allowedGameStates = AllowedGameStates.PlayingOnMap)]
        private static void ActionIdle() => FireAction(NemesisAction.DoNothing);

        [DebugAction(ActionsCat, "Direct raid", allowedGameStates = AllowedGameStates.PlayingOnMap)]
        private static void ActionRaid() => FireAction(NemesisAction.DirectRaid);

        [DebugAction(ActionsCat, "Vengeance army raid (post-escape)", allowedGameStates = AllowedGameStates.PlayingOnMap)]
        private static void ActionVengeanceArmyRaid()
        {
            if (!RequireActiveHunt(out GameComponent_Nemesis comp, out NemesisData data)) return;
            data.escapeCount = Mathf.Max(data.escapeCount, 1);
            Pawn? nemesis = comp.FindNemesisPawnFor(data);
            if (nemesis != null && nemesis.Spawned && !nemesis.Dead)
                NemesisPawnUtil.ParkAsWorldNemesis(nemesis);
            FireAction(NemesisAction.DirectRaid);
        }

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

        [DebugAction(ActionsCat, "Kidnap attempt (pawn hunts)", allowedGameStates = AllowedGameStates.PlayingOnMap)]
        private static void ActionKidnap() => FireAction(NemesisAction.KidnapAttempt);
    }
}
