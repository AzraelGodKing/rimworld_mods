using System.Collections.Generic;
using RimWorld;
using RimWorld.Planet;
using UnityEngine;
using Verse;

namespace Nemesis
{
    /// <summary>
    /// Hunt runtime. Core loop and persistence from Dredd; extended with end conditions,
    /// deferred fake ambushes, target tracking, and mod-local tick throttling.
    /// </summary>
    public class GameComponent_Nemesis : GameComponent
    {
        public static GameComponent_Nemesis Instance => Current.Game?.GetComponent<GameComponent_Nemesis>();

        private NemesisData _data;
        private List<NemesisTrophyEntry> _trophies = new List<NemesisTrophyEntry>();

        public NemesisData Data => _data;
        public List<NemesisTrophyEntry> Trophies => _trophies;

        public bool IsEngaged => _data != null && (_data.active || _data.truceUntilTick > 0);

        private static int MaxEscapes => NemesisMod.Settings?.maxEscapes ?? 4;

        public GameComponent_Nemesis(Game game)
        {
        }

        public override void FinalizeInit()
        {
            base.FinalizeInit();
            SoftCompat.ResetCaches();
            NemesisRegistry.Clear();
        }

        public override void GameComponentTick()
        {
            if (_data == null) return;

            int tick = Find.TickManager.TicksGame;

            if (!_data.active)
            {
                if (_data.graveVisitPending && tick >= _data.graveVisitTick)
                    FireGraveVisit();

                // Grave-visit leave reuses sniper despawn while hunt is already inactive.
                if (_data.sniperActive && tick % 60 == 0)
                    TickSniperTerror(tick);

                if (_data.truceUntilTick > 0)
                {
                    if (_data.nextTruceEventTick > 0 && tick >= _data.nextTruceEventTick)
                        TickTruceEvent();
                    if (tick >= _data.truceUntilTick)
                        ResumeFromTruce();
                }
                return;
            }

            // Daily aggression climb — exact day boundary. Faster when the colony ignores the nemesis.
            if (tick % 60000 == 0)
            {
                float rate = NemesisMod.Settings?.escalationRatePerDay ?? 0.06f;
                int ignoreThresh = NemesisMod.Settings?.ignoredActionsThreshold ?? 3;
                if (_data.ignoredActionsCount >= ignoreThresh)
                    rate *= 1.5f;
                _data.aggressionLevel = Mathf.Min(_data.aggressionLevel + rate, 10f);
            }

            // Pending fake-signal ambush resolves on its own timer.
            if (_data.pendingFakeAmbush && tick >= _data.fakeAmbushTick)
            {
                Map ambushMap = SoftCompat.PreferHarassmentMap(Find.AnyPlayerHomeMap);
                if (ambushMap != null)
                    NemesisActions.ResolvePendingFakeAmbush(_data, ambushMap);
                else
                {
                    _data.pendingFakeAmbush = false;
                    _data.fakeAmbushTick = -1;
                }
            }

            // Sniper terror: limited shots / approach / timeout → despawn.
            if (_data.sniperActive && tick % 60 == 0)
                TickSniperTerror(tick);

            // Food tampering delayed reveal.
            if (_data.taintedFoodThingId > 0 && !_data.taintedRevealed
                && _data.taintedRevealTick > 0 && tick >= _data.taintedRevealTick)
            {
                _data.taintedRevealed = true;
                Find.LetterStack.ReceiveLetter(
                    "Nemesis_Letter_FoodTamperTitle".Translate(_data.nemesisName),
                    "Nemesis_Letter_FoodTamperBody".Translate(_data.nemesisName, NemesisTaunts.TargetPhrase(_data)),
                    LetterDefOf.ThreatSmall);
            }

            // Deliberate silence — mid-window ominous letter.
            if (_data.silenceUntilTick > 0 && !_data.silenceLetterSent
                && _data.silenceLetterTick > 0 && tick >= _data.silenceLetterTick)
            {
                _data.silenceLetterSent = true;
                Find.LetterStack.ReceiveLetter(
                    "Nemesis_Letter_SilenceTitle".Translate(_data.nemesisName),
                    "Nemesis_Letter_SilenceBody".Translate(
                        _data.nemesisName, NemesisTaunts.TargetPhrase(_data), _data.PhaseLabelKeyed()),
                    LetterDefOf.ThreatSmall);
            }
            if (_data.silenceUntilTick > 0 && tick >= _data.silenceUntilTick)
                _data.silenceUntilTick = -1;

            // Stagger health / end-condition work. Faster on the viewed map.
            Map home = SoftCompat.PreferHarassmentMap(Find.AnyPlayerHomeMap);
            int healthInterval = NemesisRegistry.MapIsViewed(home) ? 120 : 300;
            if (tick % healthInterval == 0)
                CheckNemesisHealth();

            if (tick % 500 == 0 || NemesisRegistry.ResolutionDirty)
            {
                NemesisRegistry.ResolutionDirty = false;
                CheckEndConditions();
                CheckResolution();
            }

            if (tick % 2500 == 0 && !_data.rogue)
                CheckRogue();

            if (_data.silenceUntilTick > 0 && tick < _data.silenceUntilTick)
            {
                // Quiet window — no harassment actions.
            }
            else if (tick >= _data.nextActionTick)
                FireNextAction();
        }

        public void CreateNemesis(Pawn sourcePawn, NemesisTargetMode mode, NemesisTrigger trigger,
            Pawn targetPawn = null, Pawn useAsNemesis = null)
        {
            if (IsEngaged) return;

            Pawn nemesis;
            Faction faction;

            if (useAsNemesis != null && !useAsNemesis.Dead && !useAsNemesis.Destroyed
                && useAsNemesis.RaceProps.Humanlike
                && useAsNemesis.Faction != null && !useAsNemesis.Faction.IsPlayer)
            {
                nemesis = useAsNemesis;
                faction = nemesis.Faction;
                if (nemesis.Spawned)
                    nemesis.DeSpawn(DestroyMode.WillReplace);
                if (!nemesis.IsWorldPawn())
                    Find.WorldPawns.PassToWorld(nemesis, PawnDiscardDecideMode.KeepForever);
            }
            else
            {
                faction = sourcePawn?.Faction;
                if (faction == null || faction.IsPlayer) return;

                PawnKindDef kind = faction.RandomPawnKind();
                if (kind == null || !kind.RaceProps.Humanlike)
                    kind = PawnKindDefOf.SpaceRefugee;

                PawnGenerationRequest request = new PawnGenerationRequest(
                    kind, faction, PawnGenerationContext.NonPlayer,
                    forceGenerateNewPawn: true, canGeneratePawnRelations: false, allowDead: false);

                nemesis = PawnGenerator.GeneratePawn(request);
                Find.WorldPawns.PassToWorld(nemesis, PawnDiscardDecideMode.KeepForever);
            }

            NemesisRegistry.CachedNemesis = nemesis;
            NemesisRegistry.CachedNemesisId = nemesis.thingIDNumber;
            NemesisRegistry.CachedTarget = targetPawn;
            NemesisRegistry.CachedTargetId = targetPawn?.thingIDNumber ?? -1;

            _data = new NemesisData
            {
                active = true,
                nemesisPawnId = nemesis.thingIDNumber,
                nemesisName = nemesis.Name?.ToStringShort ?? nemesis.LabelShort,
                factionName = faction.Name,
                faction = faction,
                targetMode = mode,
                trigger = trigger,
                targetPawnId = targetPawn?.thingIDNumber ?? -1,
                targetPawnName = targetPawn?.LabelShort,
                aggressionLevel = 1f,
                nextActionTick = Find.TickManager.TicksGame + 120000,
                huntStartTick = Find.TickManager.TicksGame,
                ignoredActionsCount = 0,
                engagedSinceLastAction = false,
            };

            // Hunted mood is ThoughtWorker-driven for the fixation target; spike/relief are memories.
            SendIntroLetter(targetPawn);
        }

        private void ResumeFromTruce()
        {
            _data.active = true;
            _data.truceUntilTick = -1;
            _data.nextTruceEventTick = -1;
            _data.nextActionTick = Find.TickManager.TicksGame + 120000;

            Find.LetterStack.ReceiveLetter(
                "Nemesis_Letter_TruceBrokenTitle".Translate(_data.nemesisName),
                "Nemesis_Letter_TruceBrokenBody".Translate(_data.nemesisName),
                LetterDefOf.ThreatBig);
        }

        private void TickTruceEvent()
        {
            // Stagger ~every 2 days with a chance of gift or (state for) raid warning.
            _data.nextTruceEventTick = Find.TickManager.TicksGame + 120000;
            if (!Rand.Chance(0.45f)) return;

            Map map = SoftCompat.PreferHarassmentMap(Find.AnyPlayerHomeMap);
            if (map == null) return;

            if (Rand.Bool)
                DropTruceGift(map);
            else
            {
                // Warning letter is flavor; actual raid prefix checks truce state separately.
                Find.LetterStack.ReceiveLetter(
                    "Nemesis_Letter_TruceWarnTitle".Translate(_data.nemesisName),
                    "Nemesis_Letter_TruceWarnBody".Translate(_data.nemesisName),
                    LetterDefOf.NeutralEvent);
            }
        }

        private void DropTruceGift(Map map)
        {
            ThingDef[] weird =
            {
                ThingDefOf.Beer,
                ThingDefOf.Chocolate,
                DefDatabase<ThingDef>.GetNamedSilentFail("Apparel_Parka"),
                DefDatabase<ThingDef>.GetNamedSilentFail("SculptureSmall"),
                DefDatabase<ThingDef>.GetNamedSilentFail("ComponentIndustrial"),
            };
            ThingDef pick = null;
            for (int i = 0; i < 8; i++)
            {
                ThingDef d = weird[Rand.Range(0, weird.Length)];
                if (d != null) { pick = d; break; }
            }
            if (pick == null) pick = ThingDefOf.Silver;

            Thing gift = ThingMaker.MakeThing(pick, pick.MadeFromStuff ? GenStuff.DefaultStuffFor(pick) : null);
            gift.stackCount = pick.stackLimit > 1 ? Rand.RangeInclusive(1, Mathf.Min(5, pick.stackLimit)) : 1;
            IntVec3 spot = DropCellFinder.TradeDropSpot(map);
            if (!spot.IsValid) spot = DropCellFinder.RandomDropSpot(map);
            DropPodUtility.DropThingsNear(spot, map, new List<Thing> { gift }, 110, canInstaDropDuringInit: false, leaveSlag: false);

            Find.LetterStack.ReceiveLetter(
                "Nemesis_Letter_TruceGiftTitle".Translate(_data.nemesisName),
                "Nemesis_Letter_TruceGiftBody".Translate(_data.nemesisName, gift.LabelNoCount),
                LetterDefOf.PositiveEvent,
                new GlobalTargetInfo(spot, map));
        }

        private void FireGraveVisit()
        {
            _data.graveVisitPending = false;
            _data.graveVisitDone = true;

            Map map = null;
            IntVec3 gravePos = IntVec3.Invalid;
            Thing grave = FindTargetGrave(out map);
            if (grave != null)
                gravePos = grave.Position;

            Pawn nemesis = FindNemesisPawn();
            if (map == null || nemesis == null || nemesis.Dead)
            {
                FinalizeTargetDiedClose();
                return;
            }

            if (nemesis.Spawned)
                nemesis.DeSpawn(DestroyMode.WillReplace);
            if (nemesis.IsWorldPawn())
                Find.WorldPawns.RemovePawn(nemesis);

            IntVec3 spawn = gravePos.IsValid
                ? CellFinder.RandomClosewalkCellNear(gravePos, map, 6)
                : CellFinder.RandomEdgeCell(map);
            GenSpawn.Spawn(nemesis, spawn, map);
            NemesisRegistry.CachedNemesis = nemesis;
            NemesisRegistry.CachedNemesisId = nemesis.thingIDNumber;

            Find.LetterStack.ReceiveLetter(
                "Nemesis_Letter_GraveVisitTitle".Translate(_data.nemesisName),
                "Nemesis_Letter_GraveVisitBody".Translate(
                    _data.nemesisName, _data.targetPawnName ?? "Nemesis_Phrase_Someone".Translate()),
                LetterDefOf.NegativeEvent,
                nemesis);

            // Brief presence then leave (SniperTerror despawn plumbing).
            _data.sniperActive = true;
            _data.sniperUntilTick = Find.TickManager.TicksGame + 900;
            _data.sniperShotsLeft = 0;
            // After sniper end, FinalizeTargetDiedClose is called from EndSniperTerror hook.
            _data.graveVisitPending = false;
            // Mark so EndSniperTerror knows to close the hunt.
            _data.lastActionKind = -2; // sentinel: grave visit leave
        }

        private Thing FindTargetGrave(out Map map)
        {
            map = null;
            string targetName = _data.targetPawnName;
            List<Map> maps = Find.Maps;
            for (int m = 0; m < maps.Count; m++)
            {
                Map candidate = maps[m];
                if (candidate?.IsPlayerHome != true) continue;
                List<Thing> graves = candidate.listerThings.ThingsOfDef(ThingDefOf.Grave);
                if (graves == null) continue;
                for (int i = 0; i < graves.Count; i++)
                {
                    Thing g = graves[i];
                    Building_Grave bg = g as Building_Grave;
                    if (bg?.Corpse?.InnerPawn == null) continue;
                    Pawn inner = bg.Corpse.InnerPawn;
                    if (inner.thingIDNumber == _data.targetPawnId
                        || (!string.IsNullOrEmpty(targetName) && inner.LabelShort == targetName))
                    {
                        map = candidate;
                        return g;
                    }
                }
            }
            // Any player grave as fallback atmosphere.
            for (int m = 0; m < maps.Count; m++)
            {
                Map candidate = maps[m];
                if (candidate?.IsPlayerHome != true) continue;
                List<Thing> graves = candidate.listerThings.ThingsOfDef(ThingDefOf.Grave);
                if (graves != null && graves.Count > 0)
                {
                    map = candidate;
                    return graves[0];
                }
            }
            map = SoftCompat.PreferHarassmentMap(Find.AnyPlayerHomeMap);
            return null;
        }

        private void FinalizeTargetDiedClose()
        {
            ReleaseNemesisWorldPawn();
            NemesisRegistry.Clear();
        }

        public void NotifySniperDespawnedAfterGraveVisit()
        {
            FinalizeTargetDiedClose();
        }

        public bool IsNemesisPawn(Pawn pawn) =>
            _data != null && _data.active && pawn != null && pawn.thingIDNumber == _data.nemesisPawnId;

        public bool IsTargetPawn(Pawn pawn) =>
            _data != null && _data.active
            && _data.targetMode == NemesisTargetMode.Pawn
            && pawn != null
            && pawn.thingIDNumber == _data.targetPawnId;

        public void HandleLethalDamage(Pawn nemesis)
        {
            if (_data.escapeCount >= MaxEscapes)
                SubdueNemesis(nemesis, fromLethalDamage: true);
            else
                FireEscape(nemesis);
        }

        private void FireEscape(Pawn nemesis)
        {
            Map map = nemesis.Map;
            IntVec3 pos = nemesis.Position;

            if (nemesis.Spawned)
                nemesis.DeSpawn(DestroyMode.WillReplace);

            if (!nemesis.IsWorldPawn())
                Find.WorldPawns.PassToWorld(nemesis, PawnDiscardDecideMode.KeepForever);

            NemesisRegistry.CachedNemesis = nemesis;
            NemesisRegistry.CachedNemesisId = nemesis.thingIDNumber;

            _data.escapeCount++;
            _data.aggressionLevel = Mathf.Min(_data.aggressionLevel + 0.5f, 10f);
            _data.lastEscapeTick = Find.TickManager.TicksGame;
            _data.nextActionTick = Find.TickManager.TicksGame + 120000;

            GlobalTargetInfo lookTarget = map != null ? new GlobalTargetInfo(pos, map) : GlobalTargetInfo.Invalid;

            Find.LetterStack.ReceiveLetter(
                "Nemesis_Letter_EscapeTitle".Translate(_data.nemesisName),
                NemesisTaunts.EscapeLetterBody(_data),
                LetterDefOf.NeutralEvent,
                lookTarget);

            // Staged finale when escape cap is hit — challenge instead of quiet cornering wait.
            if (_data.escapeCount >= MaxEscapes && !_data.finaleOffered)
            {
                _data.finaleOffered = true;
                Find.WindowStack.Add(new Dialog_NemesisFinale(_data));
            }
        }

        private void SubdueNemesis(Pawn nemesis, bool fromLethalDamage)
        {
            if (nemesis == null || nemesis.Dead) return;

            // Avoid LINQ on hot path — walk hediffs once.
            List<Hediff> hediffs = nemesis.health.hediffSet.hediffs;
            for (int i = hediffs.Count - 1; i >= 0; i--)
            {
                Hediff h = hediffs[i];
                if (h is Hediff_Injury injury && injury.Bleeding)
                    injury.Heal(injury.Severity + 1f);
            }

            HediffDef bloodLossDef = DefDatabase<HediffDef>.GetNamedSilentFail("BloodLoss");
            if (bloodLossDef != null)
            {
                Hediff bloodLoss = nemesis.health.hediffSet.GetFirstHediffOfDef(bloodLossDef);
                if (bloodLoss != null) nemesis.health.RemoveHediff(bloodLoss);
            }

            if (fromLethalDamage)
            {
                for (int i = hediffs.Count - 1; i >= 0; i--)
                {
                    if (hediffs[i] is Hediff_MissingPart missing)
                        nemesis.health.RemoveHediff(missing);
                }
            }

            if (!nemesis.Downed)
            {
                HediffDef anesthetic = DefDatabase<HediffDef>.GetNamedSilentFail("Anesthetic");
                if (anesthetic != null) nemesis.health.AddHediff(anesthetic);
            }

            if (!_data.corneredAnnounced)
            {
                _data.corneredAnnounced = true;
                Find.LetterStack.ReceiveLetter(
                    "Nemesis_Letter_CorneredTitle".Translate(_data.nemesisName),
                    "Nemesis_Letter_CorneredBody".Translate(_data.nemesisName),
                    LetterDefOf.PositiveEvent,
                    nemesis);
            }
        }

        private void CheckNemesisHealth()
        {
            Pawn nemesis = FindNemesisPawn();
            if (nemesis == null || !nemesis.Spawned || nemesis.Dead || nemesis.IsPrisonerOfColony) return;

            float hp = nemesis.health.summaryHealth.SummaryHealthPercent;

            if (_data.escapeCount >= MaxEscapes)
            {
                if (hp < 0.45f && !nemesis.Downed)
                    SubdueNemesis(nemesis, fromLethalDamage: false);
            }
            else if (hp < 0.3f)
            {
                // Flee-when-losing for on-map assaults.
                FireEscape(nemesis);
            }
        }

        private void CheckEndConditions()
        {
            if (_data == null || !_data.active) return;

            if (_data.targetMode == NemesisTargetMode.Pawn && _data.targetPawnId > 0)
            {
                Pawn target = FindTargetPawn();
                if (target == null || target.Dead)
                {
                    EndHunt(NemesisEndReason.TargetDied, look: null);
                    return;
                }

                // Handed over: no longer a colonist / player faction pawn.
                if (target.Faction != Faction.OfPlayer && !target.IsColonist && !target.IsPrisonerOfColony)
                {
                    EndHunt(NemesisEndReason.TargetHandedOver, target);
                    return;
                }
            }

            Pawn nemesis = FindNemesisPawn();
            if (nemesis != null && nemesis.Dead && _data.active)
                EndHunt(NemesisEndReason.Killed, nemesis);
        }

        private void CheckResolution()
        {
            Pawn nemesis = FindNemesisPawn();
            if (nemesis == null || !nemesis.IsPrisonerOfColony) return;

            _data.active = false;
            RecordTrophy(NemesisEndReason.Captured);
            NemesisMood.NotifyHuntEnded(_data, NemesisEndReason.Captured);
            Find.WindowStack.Add(new Dialog_NemesisResolution(_data, nemesis));
        }

        public void EndHunt(NemesisEndReason reason, Thing look = null)
        {
            if (_data == null) return;

            string name = _data.nemesisName ?? "Nemesis_Phrase_Someone".Translate();
            RecordTrophy(reason);
            NemesisMood.NotifyHuntEnded(_data, reason);
            _data.active = false;
            _data.pendingFakeAmbush = false;
            _data.truceUntilTick = -1;
            _data.finaleDuelActive = false;
            _data.silenceUntilTick = -1;

            switch (reason)
            {
                case NemesisEndReason.Killed:
                    _data.sniperActive = false;
                    Find.LetterStack.ReceiveLetter(
                        "Nemesis_Letter_EndedKilledTitle".Translate(name),
                        "Nemesis_Letter_EndedKilledBody".Translate(name),
                        LetterDefOf.PositiveEvent,
                        look);
                    break;
                case NemesisEndReason.TargetDied:
                    Find.LetterStack.ReceiveLetter(
                        "Nemesis_Letter_EndedTargetDiedTitle".Translate(name),
                        "Nemesis_Letter_EndedTargetDiedBody".Translate(name, _data.targetPawnName ?? "Nemesis_Phrase_Someone".Translate()),
                        LetterDefOf.NegativeEvent);
                    // Grief visit near the grave, then close — or close immediately if nothing to show.
                    if (!_data.graveVisitDone && FindTargetGrave(out _) != null)
                    {
                        _data.graveVisitPending = true;
                        _data.graveVisitTick = Find.TickManager.TicksGame + Rand.RangeInclusive(8000, 20000);
                        // Keep world pawn pinned until visit; do not Clear registry yet.
                        return;
                    }
                    _data.sniperActive = false;
                    ReleaseNemesisWorldPawn();
                    break;
                case NemesisEndReason.TargetHandedOver:
                    _data.sniperActive = false;
                    Find.LetterStack.ReceiveLetter(
                        "Nemesis_Letter_EndedHandedTitle".Translate(name),
                        "Nemesis_Letter_EndedHandedBody".Translate(name, _data.targetPawnName ?? "Nemesis_Phrase_Someone".Translate()),
                        LetterDefOf.NeutralEvent,
                        look);
                    ReleaseNemesisWorldPawn();
                    break;
                case NemesisEndReason.Cleared:
                    _data.sniperActive = false;
                    ReleaseNemesisWorldPawn();
                    break;
            }

            NemesisRegistry.Clear();
        }

        public void RecordTrophy(NemesisEndReason reason)
        {
            if (_data == null) return;
            if (_trophies == null) _trophies = new List<NemesisTrophyEntry>();
            _trophies.Add(new NemesisTrophyEntry
            {
                nemesisName = _data.nemesisName,
                factionName = _data.factionName,
                trigger = _data.trigger,
                endReason = reason,
                startTick = _data.huntStartTick > 0 ? _data.huntStartTick : Find.TickManager.TicksGame,
                endTick = Find.TickManager.TicksGame,
            });
            while (_trophies.Count > 20)
                _trophies.RemoveAt(0);
        }

        private void ReleaseNemesisWorldPawn()
        {
            Pawn nemesis = FindNemesisPawn();
            if (nemesis == null || nemesis.Dead) return;
            if (nemesis.IsPrisonerOfColony) return;
            if (nemesis.Spawned)
                nemesis.DeSpawn(DestroyMode.WillReplace);
            if (nemesis.IsWorldPawn())
            {
                // Allow world pawn GC later — drop KeepForever pin by discarding decide.
                // WorldPawns doesn't expose unpin easily; leave them but clear our cache.
            }
        }

        private void CheckRogue()
        {
            if (_data.faction != null && !_data.faction.HostileTo(Faction.OfPlayer))
                GoRogue();
        }

        private void GoRogue()
        {
            Faction old = _data.faction;
            _data.rogue = true;

            Faction rogueFaction = null;
            foreach (Faction f in Find.FactionManager.AllFactions)
            {
                if (f == null || f.IsPlayer || f.defeated || f.Hidden || f == old) continue;
                if (!f.HostileTo(Faction.OfPlayer)) continue;
                rogueFaction = f;
                break;
            }

            Pawn nemesis = FindNemesisPawn();
            if (rogueFaction != null)
            {
                nemesis?.SetFaction(rogueFaction);
                _data.faction = rogueFaction;
                _data.factionName = rogueFaction.Name;
            }

            Find.LetterStack.ReceiveLetter(
                "Nemesis_Letter_RogueTitle".Translate(_data.nemesisName),
                rogueFaction != null
                    ? "Nemesis_Letter_RogueBodyFaction".Translate(_data.nemesisName, old?.Name ?? "Nemesis_Phrase_TheirFaction".Translate(), rogueFaction.Name)
                    : "Nemesis_Letter_RogueBodyAlone".Translate(_data.nemesisName, old?.Name ?? "Nemesis_Phrase_TheirFaction".Translate()),
                LetterDefOf.ThreatBig);
        }

        private void FireNextAction()
        {
            Map map = SoftCompat.PreferHarassmentMap(Find.AnyPlayerHomeMap);
            if (map == null)
            {
                _data.nextActionTick = Find.TickManager.TicksGame + 60000;
                return;
            }

            Pawn nemesisPawn = FindNemesisPawn();
            if (nemesisPawn != null && nemesisPawn.IsPrisonerOfColony)
            {
                _data.nextActionTick = Find.TickManager.TicksGame + 1000;
                NemesisRegistry.ResolutionDirty = true;
                return;
            }

            // Mod-local: defer piling onto an already-hot raid map.
            if (NemesisRegistry.ShouldDeferActions(map))
            {
                _data.nextActionTick = Find.TickManager.TicksGame + 7500;
                return;
            }

            // Don't fire while nemesis is personally on-map fighting / fleeing.
            if (nemesisPawn != null && nemesisPawn.Spawned && !nemesisPawn.Downed)
            {
                _data.nextActionTick = Find.TickManager.TicksGame + 5000;
                return;
            }

            NemesisAction action = PickAction();
            NemesisActions.Execute(action, _data, map);
            _data.nextActionTick = Find.TickManager.TicksGame + ActionInterval();
        }

        private void TickSniperTerror(int tick)
        {
            Pawn nemesis = FindNemesisPawn();
            if (nemesis == null || !nemesis.Spawned || nemesis.Dead || nemesis.IsPrisonerOfColony)
            {
                _data.sniperActive = false;
                return;
            }

            bool approached = false;
            List<Pawn> colonists = nemesis.Map?.mapPawns?.FreeColonistsSpawned;
            if (colonists != null)
            {
                for (int i = 0; i < colonists.Count; i++)
                {
                    Pawn c = colonists[i];
                    if (c != null && c.Position.DistanceTo(nemesis.Position) <= 20f)
                    {
                        approached = true;
                        break;
                    }
                }
            }

            if (approached || tick >= _data.sniperUntilTick || _data.sniperShotsLeft <= 0)
            {
                NemesisActions.EndSniperTerror(_data, nemesis, approached);
                return;
            }

            // Occasional non-lethal dread shot toward the fixation target / colony center.
            if (tick % 400 == 0 && _data.sniperShotsLeft > 0)
            {
                IntVec3 aim = nemesis.Map.Center;
                Pawn target = FindTargetPawn();
                if (target != null && target.Spawned && target.Map == nemesis.Map)
                    aim = target.Position;
                else if (aim.DistanceTo(nemesis.Position) < 8f)
                    aim = CellFinder.RandomClosewalkCellNear(nemesis.Position, nemesis.Map, 20);

                Verb verb = nemesis.CurrentEffectiveVerb;
                if (verb != null && verb.verbProps != null && verb.verbProps.range >= 12f)
                {
                    // Missed shot near the aim cell — dread, not a kill.
                    IntVec3 splash = aim + GenRadial.RadialPattern[Rand.Range(1, 8)];
                    if (splash.InBounds(nemesis.Map))
                    {
                        FleckMaker.ThrowDustPuffThick(splash.ToVector3Shifted(), nemesis.Map, 1.2f, Color.white);
                    }
                }
                _data.sniperShotsLeft--;
            }
        }

        private NemesisAction PickAction()
        {
            NemesisHuntPhase phase = _data.Phase;
            NemesisSettings s = NemesisMod.Settings;
            float taunt = s?.actionWeightTaunt ?? 0.35f;
            float raid = s?.actionWeightRaid ?? 0.15f;
            // Obsessed+ : assault / kidnap (was aggression >= 3).
            float assault = phase >= NemesisHuntPhase.Obsessed ? (s?.actionWeightAssault ?? 0.15f) : 0f;
            float waste = ModsConfig.BiotechActive ? (s?.actionWeightWaste ?? 0.08f) : 0f;
            float fake = s?.actionWeightFakeSignal ?? 0.10f;
            float caravan = s?.actionWeightCaravan ?? 0.07f;
            // Testing+ : sabotage (was aggression >= 2).
            float sabotage = phase >= NemesisHuntPhase.Testing ? (s?.actionWeightSabotage ?? 0.05f) : 0f;
            float food = s?.actionWeightFood ?? 0.05f;
            // Reckoning (or Obsessed with high raw agg): anomaly was >= 4 — allow in Obsessed if EffectiveAggression >= 4.
            float anomaly = 0f;
            if (ModsConfig.AnomalyActive
                && (phase >= NemesisHuntPhase.Reckoning || _data.EffectiveAggression >= 4f))
                anomaly = 0.06f;
            float kidnap = phase >= NemesisHuntPhase.Obsessed ? (s?.actionWeightKidnap ?? 0.08f) : 0f;
            float sniper = s?.actionWeightSniper ?? 0.04f;
            float grave = s?.actionWeightGrave ?? 0.05f;
            float tamper = s?.actionWeightFoodTamper ?? 0.06f;
            float informant = 0f;
            if (_data.lastVisitorLeaveTick > 0)
            {
                int age = Find.TickManager.TicksGame - _data.lastVisitorLeaveTick;
                if (age >= 0 && age <= 60000 * 4)
                    informant = s?.actionWeightInformant ?? 0.05f;
            }

            if (_data.rogue)
            {
                raid *= 0.35f;
                assault *= 1.6f;
                sabotage *= 1.3f;
                kidnap *= 1.2f;
            }

            // Soft Strata: slight bias toward food/sabotage when harassing multi-level bases (surface map).
            if (SoftCompat.StrataActive)
                food *= 1.15f;

            float total = taunt + raid + assault + waste + fake + caravan + sabotage + food + anomaly
                + kidnap + sniper + grave + tamper + informant;
            if (total <= 0f) return NemesisAction.CommsTaunt;
            float roll = Rand.Value * total;

            if ((roll -= taunt) < 0f) return NemesisAction.CommsTaunt;
            if ((roll -= raid) < 0f) return NemesisAction.DirectRaid;
            if ((roll -= assault) < 0f) return NemesisAction.NemesisAssault;
            if ((roll -= waste) < 0f) return NemesisAction.WastePackDrop;
            if ((roll -= fake) < 0f) return NemesisAction.FakeSignalAmbush;
            if ((roll -= caravan) < 0f) return NemesisAction.CaravanHarass;
            if ((roll -= sabotage) < 0f) return NemesisAction.PowerSabotage;
            if ((roll -= food) < 0f) return NemesisAction.FoodStoreRaid;
            if ((roll -= anomaly) < 0f) return NemesisAction.AnomalyBait;
            if ((roll -= kidnap) < 0f) return NemesisAction.KidnapAttempt;
            if ((roll -= sniper) < 0f) return NemesisAction.SniperTerror;
            if ((roll -= grave) < 0f) return NemesisAction.GraveDesecration;
            if ((roll -= tamper) < 0f) return NemesisAction.FoodTampering;
            if ((roll -= informant) < 0f) return NemesisAction.InformantReveal;
            return NemesisAction.CommsTaunt;
        }

        private int ActionInterval() => Mathf.Max(
            NemesisMod.Settings?.minActionCooldownTicks ?? 90000,
            (int)(300000f / _data.EffectiveAggression));

        public Pawn FindNemesisPawn()
        {
            if (_data == null) return null;
            int id = _data.nemesisPawnId;

            if (NemesisRegistry.CachedNemesis != null
                && !NemesisRegistry.CachedNemesis.Destroyed
                && NemesisRegistry.CachedNemesis.thingIDNumber == id)
                return NemesisRegistry.CachedNemesis;

            Pawn found = FindPawnById(id);
            NemesisRegistry.CachedNemesis = found;
            NemesisRegistry.CachedNemesisId = found?.thingIDNumber ?? -1;
            return found;
        }

        public Pawn FindTargetPawn()
        {
            if (_data == null || _data.targetPawnId < 0) return null;
            int id = _data.targetPawnId;

            if (NemesisRegistry.CachedTarget != null
                && !NemesisRegistry.CachedTarget.Destroyed
                && NemesisRegistry.CachedTarget.thingIDNumber == id)
                return NemesisRegistry.CachedTarget;

            Pawn found = FindPawnById(id);
            NemesisRegistry.CachedTarget = found;
            NemesisRegistry.CachedTargetId = found?.thingIDNumber ?? -1;
            return found;
        }

        private static Pawn FindPawnById(int id)
        {
            List<Map> maps = Find.Maps;
            for (int m = 0; m < maps.Count; m++)
            {
                List<Pawn> pawns = maps[m].mapPawns.AllPawns;
                for (int i = 0; i < pawns.Count; i++)
                {
                    if (pawns[i].thingIDNumber == id)
                        return pawns[i];
                }
            }

            foreach (Pawn p in Find.WorldPawns.AllPawnsAlive)
            {
                if (p.thingIDNumber == id)
                    return p;
            }

            return null;
        }

        private void SendIntroLetter(Pawn targetPawn)
        {
            string body;
            if (_data.targetMode == NemesisTargetMode.Pawn && targetPawn != null)
            {
                body = _data.trigger switch
                {
                    NemesisTrigger.KilledAlly =>
                        "Nemesis_Intro_KilledAlly".Translate(targetPawn.LabelShort, _data.factionName, _data.nemesisName),
                    NemesisTrigger.WoundedAndEscaped =>
                        "Nemesis_Intro_Wounded".Translate(targetPawn.LabelShort, _data.nemesisName, _data.factionName),
                    NemesisTrigger.Fixation =>
                        "Nemesis_Intro_Fixation".Translate(_data.factionName, _data.nemesisName, targetPawn.LabelShort),
                    _ =>
                        "Nemesis_Intro_PawnDefault".Translate(_data.factionName, targetPawn.LabelShort, _data.nemesisName),
                };
            }
            else
            {
                body = _data.trigger switch
                {
                    NemesisTrigger.PrisonerEscaped =>
                        "Nemesis_Intro_Prisoner".Translate(_data.nemesisName, _data.factionName),
                    NemesisTrigger.SlaveEscaped =>
                        "Nemesis_Intro_Slave".Translate(_data.nemesisName, _data.factionName),
                    NemesisTrigger.FactionRetaliation =>
                        "Nemesis_Intro_Retaliation".Translate(_data.factionName, _data.nemesisName),
                    _ =>
                        "Nemesis_Intro_ColonyDefault".Translate(_data.nemesisName, _data.factionName),
                };
            }

            Find.LetterStack.ReceiveLetter(
                "Nemesis_Intro_Title".Translate(),
                body,
                LetterDefOf.ThreatBig,
                targetPawn);
        }

        public override void ExposeData()
        {
            base.ExposeData();
            Scribe_Deep.Look(ref _data, "nemesisData");
            if (_data == null)
                _data = new NemesisData();
            Scribe_Collections.Look(ref _trophies, "nemesisTrophies", LookMode.Deep);
            if (_trophies == null)
                _trophies = new List<NemesisTrophyEntry>();
        }
    }
}
