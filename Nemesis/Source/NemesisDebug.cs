using System.Collections.Generic;
using LudeonTK;
using RimWorld;
using Verse;

namespace Nemesis
{
    /// <summary>Dev-mode helpers for playtesting the hunt loop without waiting on pacing.</summary>
    public static class NemesisDebug
    {
        private const string Cat = "Nemesis";
        private const string ActionsCat = "Nemesis/Actions";

        private static GameComponent_Nemesis Comp => GameComponent_Nemesis.Instance;

        private static bool RequireHunt(out GameComponent_Nemesis comp)
        {
            comp = Comp;
            if (comp == null || !comp.IsEngaged)
            {
                Messages.Message("[Nemesis] No active hunt.", MessageTypeDefOf.RejectInput, historical: false);
                return false;
            }
            return true;
        }

        [DebugAction(Cat, "Force create hunt (fixation)", allowedGameStates = AllowedGameStates.PlayingOnMap)]
        private static void ForceCreateFixation()
        {
            Map map = Find.CurrentMap;
            Pawn colonist = map?.mapPawns?.FreeColonistsSpawned?.RandomElement();
            if (colonist == null)
            {
                Messages.Message("[Nemesis] Need a free colonist on the current map.",
                    MessageTypeDefOf.RejectInput, historical: false);
                return;
            }
            if (Comp != null && Comp.IsEngaged)
            {
                Messages.Message("[Nemesis] Hunt already active — end it first.",
                    MessageTypeDefOf.RejectInput, historical: false);
                return;
            }

            Faction hostile = null;
            List<Faction> factions = Find.FactionManager.AllFactionsListForReading;
            for (int i = 0; i < factions.Count; i++)
            {
                Faction f = factions[i];
                if (f != null && !f.IsPlayer && !f.defeated && f.HostileTo(Faction.OfPlayer)
                    && f.def.humanlikeFaction)
                {
                    hostile = f;
                    break;
                }
            }
            if (hostile == null)
            {
                Messages.Message("[Nemesis] No hostile humanlike faction.",
                    MessageTypeDefOf.RejectInput, historical: false);
                return;
            }

            PawnKindDef kind = hostile.RandomPawnKind();
            if (kind == null || !kind.RaceProps.Humanlike)
                kind = PawnKindDefOf.SpaceRefugee;
            Pawn source = PawnGenerator.GeneratePawn(kind, hostile);
            Comp.CreateNemesis(source, NemesisTargetMode.Pawn, NemesisTrigger.Fixation, colonist, source);
            Messages.Message($"[Nemesis] Hunt created vs {colonist.LabelShort}.",
                MessageTypeDefOf.TaskCompletion, historical: false);
        }

        [DebugAction(Cat, "Force end hunt", allowedGameStates = AllowedGameStates.PlayingOnMap)]
        private static void ForceEnd()
        {
            if (!RequireHunt(out GameComponent_Nemesis comp)) return;
            comp.EndHunt(NemesisEndReason.Cleared);
            Messages.Message("[Nemesis] Hunt cleared.", MessageTypeDefOf.TaskCompletion, historical: false);
        }

        [DebugAction(Cat, "Fire next action now", allowedGameStates = AllowedGameStates.PlayingOnMap)]
        private static void FireNext()
        {
            if (!RequireHunt(out GameComponent_Nemesis comp)) return;
            comp.Data.nextActionTick = Find.TickManager.TicksGame;
            Messages.Message("[Nemesis] Next action armed for this tick.",
                MessageTypeDefOf.TaskCompletion, historical: false);
        }

        [DebugAction(Cat, "Advance intel / place camp", allowedGameStates = AllowedGameStates.PlayingOnMap)]
        private static void AdvanceIntel()
        {
            if (!RequireHunt(out GameComponent_Nemesis comp)) return;
            if (NemesisCampUtility.TryAdvanceIntel(comp.Data))
                Messages.Message($"[Nemesis] Intel level {comp.Data.intelLevel}.",
                    MessageTypeDefOf.TaskCompletion, historical: false);
            else
                Messages.Message("[Nemesis] Intel already maxed or camp placed.",
                    MessageTypeDefOf.RejectInput, historical: false);
        }

        [DebugAction(Cat, "Apply gear tint", allowedGameStates = AllowedGameStates.PlayingOnMap)]
        private static void ApplyTint()
        {
            if (!RequireHunt(out GameComponent_Nemesis comp)) return;
            Pawn n = comp.FindNemesisPawn();
            if (n == null)
            {
                Messages.Message("[Nemesis] Nemesis pawn not found.",
                    MessageTypeDefOf.RejectInput, historical: false);
                return;
            }
            NemesisIdentity.Apply(n, comp.Data);
            Messages.Message("[Nemesis] Tint applied.", MessageTypeDefOf.TaskCompletion, historical: false);
        }

        [DebugAction(Cat, "Open comms reply dialog", allowedGameStates = AllowedGameStates.PlayingOnMap)]
        private static void OpenComms()
        {
            if (!RequireHunt(out GameComponent_Nemesis comp)) return;
            Find.WindowStack.Add(new Dialog_NemesisComms(comp.Data));
        }

        [DebugAction(Cat, "Bump aggression +1", allowedGameStates = AllowedGameStates.PlayingOnMap)]
        private static void BumpAgg()
        {
            if (!RequireHunt(out GameComponent_Nemesis comp)) return;
            comp.Data.aggressionLevel = UnityEngine.Mathf.Min(comp.Data.aggressionLevel + 1f, 10f);
            Messages.Message($"[Nemesis] Aggression {comp.Data.aggressionLevel:0.0} ({comp.Data.PhaseLabelKeyed()}).",
                MessageTypeDefOf.TaskCompletion, historical: false);
        }

        [DebugAction(ActionsCat, "Execute: CommsTaunt", allowedGameStates = AllowedGameStates.PlayingOnMap)]
        private static void ActTaunt() => ForceAction(NemesisAction.CommsTaunt);

        [DebugAction(ActionsCat, "Execute: DirectRaid", allowedGameStates = AllowedGameStates.PlayingOnMap)]
        private static void ActRaid() => ForceAction(NemesisAction.DirectRaid);

        [DebugAction(ActionsCat, "Execute: NemesisAssault", allowedGameStates = AllowedGameStates.PlayingOnMap)]
        private static void ActAssault() => ForceAction(NemesisAction.NemesisAssault);

        [DebugAction(ActionsCat, "Execute: CampIntel", allowedGameStates = AllowedGameStates.PlayingOnMap)]
        private static void ActIntel() => ForceAction(NemesisAction.CampIntel);

        [DebugAction(ActionsCat, "Execute: PowerSabotage", allowedGameStates = AllowedGameStates.PlayingOnMap)]
        private static void ActSabotage() => ForceAction(NemesisAction.PowerSabotage);

        [DebugAction(ActionsCat, "Execute: FoodStoreRaid", allowedGameStates = AllowedGameStates.PlayingOnMap)]
        private static void ActFood() => ForceAction(NemesisAction.FoodStoreRaid);

        [DebugAction(ActionsCat, "Execute: KidnapAttempt", allowedGameStates = AllowedGameStates.PlayingOnMap)]
        private static void ActKidnap() => ForceAction(NemesisAction.KidnapAttempt);

        [DebugAction(ActionsCat, "Execute: CaravanHarass", allowedGameStates = AllowedGameStates.PlayingOnMap)]
        private static void ActCaravan() => ForceAction(NemesisAction.CaravanHarass);

        [DebugAction(ActionsCat, "Execute finale assault", allowedGameStates = AllowedGameStates.PlayingOnMap)]
        private static void ActFinale()
        {
            if (!RequireHunt(out GameComponent_Nemesis comp)) return;
            Map map = SoftCompat.PreferHarassmentMap(Find.CurrentMap ?? Find.AnyPlayerHomeMap);
            if (map == null) return;
            NemesisActions.ExecuteFinaleAssault(comp.Data, map);
        }

        private static void ForceAction(NemesisAction action)
        {
            if (!RequireHunt(out GameComponent_Nemesis comp)) return;
            Map map = SoftCompat.PreferHarassmentMap(Find.CurrentMap ?? Find.AnyPlayerHomeMap);
            if (map == null)
            {
                Messages.Message("[Nemesis] No map.", MessageTypeDefOf.RejectInput, historical: false);
                return;
            }
            NemesisActions.Execute(action, comp.Data, map);
            Messages.Message($"[Nemesis] Executed {action}.", MessageTypeDefOf.TaskCompletion, historical: false);
        }
    }
}
