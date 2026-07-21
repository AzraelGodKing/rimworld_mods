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

        public bool IsEngaged => _data != null
            && (_data.active || _data.truceUntilTick > 0 || _data.captivityActive);

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

                // Prisoner arc while hunt is inactive but captivity pin is held.
                if (_data.captivityActive)
                    TickCaptivity(tick);
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

            // Rival cameo leave window (does not use sniper plumbing for hostility).
            if (_data.rivalCameoActive && tick % 60 == 0)
                TickRivalCameo(tick);

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

            // Caravan route tracking (caches / soft ambushes) while Testing+.
            if (tick % 2500 == 0)
                NemesisCampUtility.TickCaravanTracking(_data);

            // Auto-drip intel scraps at Obsessed+ if the player hasn't investigated.
            if (tick % 60000 == 0 && _data.Phase >= NemesisHuntPhase.Obsessed
                && _data.intelLevel < NemesisCampUtility.MaxIntel && Rand.Chance(0.28f))
                NemesisCampUtility.TryAdvanceIntel(_data);

            // Nightmares — staggered, Obsessed+.
            if ((NemesisMod.Settings?.nightmaresEnabled ?? true)
                && _data.Phase >= NemesisHuntPhase.Obsessed
                && tick % 2500 == 0)
                TickNightmares(tick);

            // Endgame crasher — ship countdown / gravship launch probe.
            if (tick % 2500 == 0)
                TickEndgameCrasher();
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
                archetype = NemesisData.RollArchetype(),
                nextCaravanTrackTick = Find.TickManager.TicksGame + Rand.RangeInclusive(90000, 150000),
            };

            NemesisIdentity.RollAndStore(_data);
            NemesisIdentity.Apply(nemesis, _data);
            NemesisSocial.EnsureRivalry(_data, nemesis, targetPawn);

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

        /// <summary>Spouse/lover/best friend gains vengeance thought; hunt stays closed with a remember letter.</summary>
        private void ApplyVendettaInheritance()
        {
            Pawn dead = FindTargetPawn();
            // Target may already be dead — still find relations from corpse/pawn record.
            Pawn heir = FindVendettaHeir(dead);
            if (heir?.needs?.mood?.thoughts?.memories != null)
            {
                ThoughtDef vengeance = DefDatabase<ThoughtDef>.GetNamedSilentFail("Nemesis_Vengeance");
                if (vengeance != null)
                    heir.needs.mood.thoughts.memories.TryGainMemory(vengeance);
                Find.LetterStack.ReceiveLetter(
                    "Nemesis_Letter_VendettaTitle".Translate(heir.LabelShort),
                    "Nemesis_Letter_VendettaBody".Translate(
                        heir.LabelShort, _data.nemesisName,
                        _data.targetPawnName ?? "Nemesis_Phrase_Someone".Translate()),
                    LetterDefOf.NeutralEvent,
                    heir);
            }
            else
            {
                Find.LetterStack.ReceiveLetter(
                    "Nemesis_Letter_TheyRememberTitle".Translate(_data.nemesisName),
                    "Nemesis_Letter_TheyRememberBody".Translate(_data.nemesisName),
                    LetterDefOf.NeutralEvent);
            }
        }

        private static Pawn FindVendettaHeir(Pawn dead)
        {
            if (dead?.relations?.DirectRelations == null)
            {
                // Try any colonist who shared a relation via world — fail-open null.
                return null;
            }

            PawnRelationDef spouseDef = DefDatabase<PawnRelationDef>.GetNamedSilentFail("Spouse");
            PawnRelationDef loverDef = DefDatabase<PawnRelationDef>.GetNamedSilentFail("Lover");
            PawnRelationDef fianceDef = DefDatabase<PawnRelationDef>.GetNamedSilentFail("Fiance");
            List<DirectPawnRelation> rels = dead.relations.DirectRelations;

            for (int i = 0; i < rels.Count; i++)
            {
                DirectPawnRelation r = rels[i];
                if (r?.otherPawn == null || r.otherPawn.Dead) continue;
                if (!r.otherPawn.IsColonist) continue;
                if (r.def == spouseDef || r.def == loverDef || r.def == fianceDef)
                    return r.otherPawn;
            }

            // Best friend: highest opinion among colonists.
            Pawn best = null;
            int bestOpinion = 40;
            List<Map> maps = Find.Maps;
            for (int m = 0; m < maps.Count; m++)
            {
                List<Pawn> colonists = maps[m]?.mapPawns?.FreeColonistsSpawned;
                if (colonists == null) continue;
                for (int i = 0; i < colonists.Count; i++)
                {
                    Pawn p = colonists[i];
                    if (p == null || p.Dead || p == dead) continue;
                    int op = p.relations?.OpinionOf(dead) ?? 0;
                    if (op > bestOpinion)
                    {
                        bestOpinion = op;
                        best = p;
                    }
                }
            }
            return best;
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
            // Rival cameo is a cameo — leave without counting as an escape.
            if (_data.rivalCameoActive)
            {
                NemesisActions.EndRivalCameo(_data, nemesis);
                return;
            }
            TelegraphImmortality(nemesis);
            if (_data.escapeCount >= MaxEscapes)
                SubdueNemesis(nemesis, fromLethalDamage: true);
            else
                FireEscape(nemesis);
        }

        private static void TelegraphImmortality(Pawn nemesis)
        {
            if (nemesis == null || !nemesis.Spawned || nemesis.Map == null) return;
            try
            {
                FleckMaker.Static(nemesis.Position, nemesis.Map, FleckDefOf.ExplosionFlash, 3f);
                FleckMaker.ThrowDustPuffThick(nemesis.DrawPos, nemesis.Map, 2f, new Color(0.7f, 0.15f, 0.15f));
            }
            catch
            {
                // Fail-open if fleck defs differ.
            }
            Messages.Message(
                "Nemesis_Message_Immortal".Translate(nemesis.LabelShort),
                nemesis,
                MessageTypeDefOf.ThreatSmall);
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

            string upgradeLine = NemesisUpgrades.TryApplyEscapeUpgrade(nemesis);
            string body = NemesisTaunts.EscapeLetterBody(_data);
            if (!string.IsNullOrEmpty(upgradeLine))
                body += "\n\n" + upgradeLine;

            GlobalTargetInfo lookTarget = map != null ? new GlobalTargetInfo(pos, map) : GlobalTargetInfo.Invalid;

            Find.LetterStack.ReceiveLetter(
                "Nemesis_Letter_EscapeTitle".Translate(_data.nemesisName),
                body,
                LetterDefOf.NeutralEvent,
                lookTarget);

            // Calling card left behind on escape (physical evidence).
            if (map != null && Rand.Chance(0.65f))
                NemesisEvidence.DropCallingCard(_data, map,
                    "Nemesis_Letter_CallingCardTitle", "Nemesis_Letter_CallingCardBody");

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
            // Already in KeepPrisoner aftermath — do not reopen the dialog.
            if (_data.captivityActive) return;

            _data.active = false;
            // Trophy + hunt-end moods fire from Dialog_NemesisResolution.ApplyOutcome
            // (skipped for Truce so a resumed hunt still records exactly once later).
            Find.WindowStack.Add(new Dialog_NemesisResolution(_data, nemesis));
        }

        /// <summary>Start KeepPrisoner aftermath — hunt inactive but pin retained.</summary>
        public void BeginCaptivity(Pawn nemesis)
        {
            if (_data == null) return;
            _data.active = false;
            _data.captivityActive = true;
            _data.captivityStartTick = Find.TickManager.TicksGame;
            _data.nextJailbreakCheckTick = Find.TickManager.TicksGame + Rand.RangeInclusive(60000, 180000);
            _data.moleLetterSent = false;
            _data.moleColonistId = -1;
            _data.moleSabotageTick = -1;
            // Keep registry pin so FindNemesisPawn still works.
            if (nemesis != null)
            {
                NemesisRegistry.CachedNemesis = nemesis;
                NemesisRegistry.CachedNemesisId = nemesis.thingIDNumber;
            }
        }

        public void ClearCaptivityAndEnd(NemesisEndReason reason, Thing look = null)
        {
            if (_data == null) return;
            _data.captivityActive = false;
            _data.moleLetterSent = false;
            _data.moleColonistId = -1;
            _data.moleSabotageTick = -1;
            EndHunt(reason, look);
        }

        private void TickCaptivity(int tick)
        {
            Pawn nemesis = FindNemesisPawn();
            if (nemesis == null || nemesis.Dead)
            {
                ClearCaptivityAndEnd(NemesisEndReason.Killed, nemesis);
                return;
            }

            // Cleared via normal prisoner UI (released / recruited / sold).
            if (!nemesis.IsPrisonerOfColony)
            {
                ClearCaptivityAndEnd(NemesisEndReason.Cleared, nemesis);
                return;
            }

            if (tick % 500 == 0)
                NemesisMood.NotifyCellmateAura(_data, nemesis);

            if (_data.nextJailbreakCheckTick > 0 && tick >= _data.nextJailbreakCheckTick)
            {
                _data.nextJailbreakCheckTick = tick + Rand.RangeInclusive(90000, 180000);
                TryJailbreak(nemesis);
            }

            TickMole(tick, nemesis);
        }

        private void TryJailbreak(Pawn nemesis)
        {
            if (nemesis == null || !nemesis.Spawned || nemesis.Map == null) return;
            int heldDays = _data.captivityStartTick > 0
                ? (Find.TickManager.TicksGame - _data.captivityStartTick) / 60000
                : 0;
            if (heldDays < 2 && _data.EffectiveAggression < 3f) return;
            if (!Rand.Chance(0.35f + Mathf.Min(0.35f, _data.EffectiveAggression * 0.05f))) return;

            Map map = SoftCompat.PreferHarassmentMap(nemesis.Map);
            Faction faction = NemesisActions.FindFaction(_data);
            if (map == null || faction == null || faction.IsPlayer) return;

            IncidentDef raidDef = DefDatabase<IncidentDef>.GetNamedSilentFail("RaidEnemy");
            if (raidDef == null) return;

            IncidentParms parms = StorytellerUtility.DefaultParmsNow(IncidentCategoryDefOf.ThreatBig, map);
            parms.faction = faction;
            parms.points = Mathf.Max(200f, parms.points * 0.45f);
            parms.customLetterLabel = "Nemesis_Letter_JailbreakTitle".Translate(_data.nemesisName);
            parms.customLetterText = "Nemesis_Letter_JailbreakBody".Translate(_data.nemesisName);
            NemesisActions.ApplyKillboxAwareSpawnPublic(parms, map);

            // Prefer kidnap-capable small squad via Lord if raid worker fails soft.
            if (!raidDef.Worker.TryExecute(parms))
            {
                NemesisActions.FireJailbreakSquad(_data, map, faction, nemesis);
            }
            else
            {
                Find.LetterStack.ReceiveLetter(
                    "Nemesis_Letter_JailbreakTitle".Translate(_data.nemesisName),
                    "Nemesis_Letter_JailbreakBody".Translate(_data.nemesisName),
                    LetterDefOf.ThreatBig,
                    nemesis);
            }
        }

        private void TickMole(int tick, Pawn nemesis)
        {
            if (!(NemesisMod.Settings?.moleEnabled ?? true)) return;

            // Start window: name a miserable colonist.
            if (!_data.moleLetterSent && _data.moleColonistId < 0
                && tick > _data.captivityStartTick + 30000
                && Rand.Chance(NemesisMod.Settings?.moleChance ?? 0.08f))
            {
                Pawn mole = PickMiserableColonist(nemesis?.Map);
                if (mole == null) return;
                _data.moleLetterSent = true;
                _data.moleColonistId = mole.thingIDNumber;
                _data.moleSabotageTick = tick + Rand.RangeInclusive(15000, 40000);
                Find.LetterStack.ReceiveLetter(
                    "Nemesis_Letter_MoleWarnTitle".Translate(_data.nemesisName),
                    "Nemesis_Letter_MoleWarnBody".Translate(mole.LabelShort, _data.nemesisName),
                    LetterDefOf.ThreatSmall,
                    mole);
                return;
            }

            // Sabotage letter — attributed, agency-safe (no forced job).
            if (_data.moleColonistId > 0 && _data.moleSabotageTick > 0 && tick >= _data.moleSabotageTick)
            {
                _data.moleSabotageTick = -1;
                Pawn mole = FindPawnById(_data.moleColonistId);
                string moleName = mole?.LabelShort ?? "Nemesis_Phrase_Someone".Translate();
                Find.LetterStack.ReceiveLetter(
                    "Nemesis_Letter_MoleActTitle".Translate(moleName),
                    "Nemesis_Letter_MoleActBody".Translate(moleName, _data.nemesisName),
                    LetterDefOf.NegativeEvent,
                    mole);

                if (mole?.needs?.mood?.thoughts?.memories != null)
                {
                    ThoughtDef guilt = DefDatabase<ThoughtDef>.GetNamedSilentFail("Nemesis_MoleGuilt");
                    if (guilt != null)
                        mole.needs.mood.thoughts.memories.TryGainMemory(guilt);
                }
                // Tiny goodwill sting if faction exists — optional.
                Faction f = NemesisActions.FindFaction(_data);
                f?.TryAffectGoodwillWith(Faction.OfPlayer, -3, canSendMessage: false, canSendHostilityLetter: false);
                _data.moleColonistId = -1;
            }
        }

        private static Pawn PickMiserableColonist(Map map)
        {
            map = SoftCompat.PreferHarassmentMap(map ?? Find.AnyPlayerHomeMap);
            if (map?.mapPawns?.FreeColonistsSpawned == null) return null;
            List<Pawn> colonists = map.mapPawns.FreeColonistsSpawned;
            Pawn worst = null;
            float worstMood = 999f;
            for (int i = 0; i < colonists.Count; i++)
            {
                Pawn p = colonists[i];
                if (p?.needs?.mood == null || p.Dead) continue;
                float m = p.needs.mood.CurLevelPercentage;
                if (m < worstMood)
                {
                    worstMood = m;
                    worst = p;
                }
            }
            // Prefer miserable; otherwise any colonist with lowish mood.
            if (worst != null && worstMood < 0.55f) return worst;
            return colonists.Count > 0 ? colonists[Rand.Range(0, colonists.Count)] : null;
        }

        public void EndHunt(NemesisEndReason reason, Thing look = null)
        {
            if (_data == null) return;

            // Mid-cameo hunt end must restore the scribed original faction before despawn/release.
            if (_data.rivalCameoActive || _data.rivalCameoOriginalFaction != null)
            {
                Pawn cameoPawn = FindNemesisPawn();
                NemesisActions.EndRivalCameo(_data, cameoPawn);
            }

            string name = _data.nemesisName ?? "Nemesis_Phrase_Someone".Translate();
            RecordTrophy(reason);
            NemesisMood.NotifyHuntEnded(_data, reason);
            NemesisCampUtility.ClearCamp(_data);
            _data.active = false;
            _data.pendingFakeAmbush = false;
            _data.truceUntilTick = -1;
            _data.finaleDuelActive = false;
            _data.silenceUntilTick = -1;
            _data.captivityActive = false;
            _data.moleLetterSent = false;
            _data.moleColonistId = -1;
            _data.moleSabotageTick = -1;

            switch (reason)
            {
                case NemesisEndReason.Killed:
                    _data.sniperActive = false;
                    {
                        Map deathMap = look?.MapHeld ?? SoftCompat.PreferHarassmentMap(Find.AnyPlayerHomeMap);
                        IntVec3 deathPos = look != null && look.Spawned ? look.Position : IntVec3.Invalid;
                        NemesisEvidence.DropJournalOnDeath(_data, deathMap, deathPos);
                    }
                    Find.LetterStack.ReceiveLetter(
                        "Nemesis_Letter_EndedKilledTitle".Translate(name),
                        "Nemesis_Letter_EndedKilledBody".Translate(name),
                        LetterDefOf.PositiveEvent,
                        look);
                    break;
                case NemesisEndReason.TargetDied:
                    if (_data.targetKilledByNemesis)
                    {
                        // Satisfied win — nemesis walks away; no robbed grave visit.
                        _data.sniperActive = false;
                        _data.graveVisitPending = false;
                        Find.LetterStack.ReceiveLetter(
                            "Nemesis_Letter_SatisfiedWinTitle".Translate(name),
                            "Nemesis_Letter_SatisfiedWinBody".Translate(
                                name, _data.targetPawnName ?? "Nemesis_Phrase_Someone".Translate()),
                            LetterDefOf.NegativeEvent);
                        ApplyVendettaInheritance();
                        ReleaseNemesisWorldPawn();
                        // Despawn living nemesis quietly if still around.
                        Pawn n = FindNemesisPawn();
                        if (n != null && !n.Dead && n.Spawned)
                        {
                            n.DeSpawn(DestroyMode.WillReplace);
                            if (!n.IsWorldPawn())
                                Find.WorldPawns.PassToWorld(n, PawnDiscardDecideMode.Decide);
                        }
                    }
                    else
                    {
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
                    }
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

            // Opportunist bias: if conditions are calm, sometimes wait for a better window.
            if (!SoftCompat.IsOpportunistWindow(map) && Rand.Chance(0.35f))
            {
                _data.nextActionTick = Find.TickManager.TicksGame + Rand.RangeInclusive(7500, 20000);
                return;
            }

            // Anniversary: every 60 hunt-days force a commemorative taunt / light strike.
            if ((NemesisMod.Settings?.anniversaryEnabled ?? true) && TryFireAnniversary(map))
            {
                _data.nextActionTick = Find.TickManager.TicksGame + ActionInterval();
                return;
            }

            NemesisAction action = PickAction();

            // Soft DLC seasoning before execute.
            if (action == NemesisAction.CommsTaunt
                && SoftCompat.NemesisHasRoyalTitle(nemesisPawn)
                && !_data.royaltyDuelFormalized
                && Rand.Chance(0.35f))
            {
                _data.royaltyDuelFormalized = true;
                Find.LetterStack.ReceiveLetter(
                    "Nemesis_Letter_RoyalDuelTitle".Translate(_data.nemesisName),
                    "Nemesis_Letter_RoyalDuelBody".Translate(
                        _data.nemesisName, NemesisTaunts.TargetPhrase(_data)),
                    LetterDefOf.ThreatSmall);
            }
            if ((action == NemesisAction.TrophyTheft || action == NemesisAction.GraveDesecration)
                && SoftCompat.TryFireRelicTheftLetter(_data))
            {
                _data.nextActionTick = Find.TickManager.TicksGame + ActionInterval();
                return;
            }

            NemesisActions.Execute(action, _data, map);
            _data.nextActionTick = Find.TickManager.TicksGame + ActionInterval();
        }

        private bool TryFireAnniversary(Map map)
        {
            if (_data.huntStartTick <= 0) return false;
            int days = _data.HuntDays;
            if (days < 60 || days % 60 != 0) return false;
            if (_data.lastAnniversaryDay == days) return false;
            _data.lastAnniversaryDay = days;

            Find.LetterStack.ReceiveLetter(
                "Nemesis_Letter_AnniversaryTitle".Translate(_data.nemesisName, days),
                "Nemesis_Letter_AnniversaryBody".Translate(
                    _data.nemesisName, NemesisTaunts.TargetPhrase(_data), days),
                LetterDefOf.ThreatSmall);

            if (Rand.Chance(0.55f))
                NemesisActions.CommsTaunt(_data, map);
            else if (_data.Phase >= NemesisHuntPhase.Testing)
                NemesisActions.Execute(NemesisAction.PowerSabotage, _data, map);
            else
                NemesisActions.CommsTaunt(_data, map);
            return true;
        }

        private void TickNightmares(int tick)
        {
            if (_data.targetMode != NemesisTargetMode.Pawn) return;
            if (_data.nextNightmareCheckTick > 0 && tick < _data.nextNightmareCheckTick) return;
            _data.nextNightmareCheckTick = tick + Rand.RangeInclusive(60000, 120000);

            Pawn target = FindTargetPawn();
            if (target?.needs?.mood?.thoughts?.memories == null) return;
            if (!Rand.Chance(0.4f)) return;

            ThoughtDef nightmare = DefDatabase<ThoughtDef>.GetNamedSilentFail("Nemesis_Nightmare");
            if (nightmare == null) return;
            target.needs.mood.thoughts.memories.TryGainMemory(nightmare);
            Messages.Message(
                "Nemesis_Message_Nightmare".Translate(target.LabelShort, _data.nemesisName),
                target,
                MessageTypeDefOf.NegativeEvent);
        }

        private void TickEndgameCrasher()
        {
            if (_data.endgameFinaleScheduled) return;
            if (!SoftCompat.IsEndgameCountdownActive()) return;
            _data.endgameFinaleScheduled = true;
            Map map = SoftCompat.PreferHarassmentMap(Find.AnyPlayerHomeMap);
            if (map == null) return;

            Find.LetterStack.ReceiveLetter(
                "Nemesis_Letter_EndgameTitle".Translate(_data.nemesisName),
                "Nemesis_Letter_EndgameBody".Translate(_data.nemesisName, NemesisTaunts.TargetPhrase(_data)),
                LetterDefOf.ThreatBig);

            NemesisActions.ExecuteFinaleAssault(_data, map);
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

            // Grave visits reuse sniper despawn with sniperShotsLeft = 0; do not treat
            // that as "shots exhausted" or the presence ends on the first tick.
            bool graveLeave = _data.lastActionKind == -2;
            bool shotsExhausted = !graveLeave && _data.sniperShotsLeft <= 0;
            if (approached || tick >= _data.sniperUntilTick || shotsExhausted)
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

        private void TickRivalCameo(int tick)
        {
            Pawn nemesis = FindNemesisPawn();
            if (nemesis == null || !nemesis.Spawned || nemesis.Dead || nemesis.IsPrisonerOfColony
                || tick >= _data.rivalCameoUntilTick)
            {
                NemesisActions.EndRivalCameo(_data, nemesis);
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
                anomaly = s?.actionWeightAnomaly ?? 0.06f;
            float kidnap = phase >= NemesisHuntPhase.Obsessed ? (s?.actionWeightKidnap ?? 0.08f) : 0f;
            float sniper = s?.actionWeightSniper ?? 0.04f;
            if (s != null && !s.kidnapEnabled) kidnap = 0f;
            if (s != null && !s.sniperEnabled) sniper = 0f;
            float grave = s?.actionWeightGrave ?? 0.05f;
            float tamper = s?.actionWeightFoodTamper ?? 0.06f;
            float informant = 0f;
            if (_data.lastVisitorLeaveTick > 0)
            {
                int age = Find.TickManager.TicksGame - _data.lastVisitorLeaveTick;
                if (age >= 0 && age <= 60000 * 4)
                    informant = s?.actionWeightInformant ?? 0.05f;
            }
            // Soft Strata burrow — only when probe succeeds and phase is Obsessed+.
            float burrow = 0f;
            if (SoftCompat.StrataBurrowAvailable && phase >= NemesisHuntPhase.Obsessed
                && _data.EffectiveAggression >= 3.5f)
                burrow = s?.actionWeightBurrow ?? 0.04f;

            // Trophy theft — Saboteur/Trickster Obsessed+, or any archetype at high agg.
            float trophy = 0f;
            if (phase >= NemesisHuntPhase.Obsessed && _data.stolenTrophyThingId < 0)
            {
                float trophyW = s?.actionWeightTrophy ?? 0.08f;
                if (_data.archetype == NemesisArchetype.Saboteur || _data.archetype == NemesisArchetype.Trickster)
                    trophy = trophyW;
                else if (_data.EffectiveAggression >= 4f)
                    trophy = trophyW * 0.5f;
            }

            // Progressive camp intel — Testing+, until camp placed.
            float intel = 0f;
            float intelW = s?.actionWeightIntel ?? 0.07f;
            if (phase >= NemesisHuntPhase.Testing && _data.intelLevel < NemesisCampUtility.MaxIntel)
                intel = intelW;
            else if (phase >= NemesisHuntPhase.Obsessed && _data.campWorldObjectId < 0 && !_data.campResolved)
                intel = intelW * (5f / 7f);

            if (_data.rogue)
            {
                raid *= 0.35f;
                assault *= 1.6f;
                sabotage *= 1.3f;
                kidnap *= 1.2f;
            }

            // Soft Strata: slight bias toward food/sabotage when harassing multi-level bases (surface map).
            if (SoftCompat.StrataActive)
            {
                food *= 1.15f;
                if (SoftCompat.MapHasStrataStairs(SoftCompat.PreferHarassmentMap(Find.AnyPlayerHomeMap)))
                    sabotage *= 1.1f;
            }

            // Stormproof ion bait: push sabotage while ion conditions are active.
            Map baitMap = SoftCompat.PreferHarassmentMap(Find.AnyPlayerHomeMap);
            if (SoftCompat.ShouldIonStormBait(_data, baitMap))
                sabotage *= 1.75f;

            // Archetype personality bias (Phase 1).
            ApplyArchetypeBias(_data.archetype, ref taunt, ref raid, ref assault, ref fake,
                ref sabotage, ref food, ref kidnap, ref sniper, ref grave, ref tamper, ref informant);

            // Live trait leak — no LINQ; fail-open if pawn/traits missing.
            ApplyTraitBias(FindNemesisPawn(), ref assault, ref sabotage);

            // Variety guard: heavily downweight the last action so it never fires twice in a row.
            int last = _data.lastActionKind;
            if (last >= 0)
            {
                NemesisAction lastKind = (NemesisAction)last;
                if (lastKind == NemesisAction.CommsTaunt) taunt = 0f;
                else if (lastKind == NemesisAction.DirectRaid) raid = 0f;
                else if (lastKind == NemesisAction.NemesisAssault) assault = 0f;
                else if (lastKind == NemesisAction.WastePackDrop) waste = 0f;
                else if (lastKind == NemesisAction.FakeSignalAmbush) fake = 0f;
                else if (lastKind == NemesisAction.CaravanHarass) caravan = 0f;
                else if (lastKind == NemesisAction.PowerSabotage) sabotage = 0f;
                else if (lastKind == NemesisAction.FoodStoreRaid) food = 0f;
                else if (lastKind == NemesisAction.AnomalyBait) anomaly = 0f;
                else if (lastKind == NemesisAction.KidnapAttempt) kidnap = 0f;
                else if (lastKind == NemesisAction.SniperTerror) sniper = 0f;
                else if (lastKind == NemesisAction.GraveDesecration) grave = 0f;
                else if (lastKind == NemesisAction.FoodTampering) tamper = 0f;
                else if (lastKind == NemesisAction.InformantReveal) informant = 0f;
                else if (lastKind == NemesisAction.StrataBurrow) burrow = 0f;
                else if (lastKind == NemesisAction.TrophyTheft) trophy = 0f;
                else if (lastKind == NemesisAction.CampIntel) intel = 0f;
            }

            float total = taunt + raid + assault + waste + fake + caravan + sabotage + food + anomaly
                + kidnap + sniper + grave + tamper + informant + burrow + trophy + intel;
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
            if ((roll -= burrow) < 0f) return NemesisAction.StrataBurrow;
            if ((roll -= trophy) < 0f) return NemesisAction.TrophyTheft;
            if ((roll -= intel) < 0f) return NemesisAction.CampIntel;
            return NemesisAction.CommsTaunt;
        }

        private static void ApplyArchetypeBias(NemesisArchetype arch,
            ref float taunt, ref float raid, ref float assault, ref float fake,
            ref float sabotage, ref float food, ref float kidnap, ref float sniper,
            ref float grave, ref float tamper, ref float informant)
        {
            switch (arch)
            {
                case NemesisArchetype.Stalker:
                    taunt *= 1.45f;
                    informant *= 1.5f;
                    sniper *= 1.55f;
                    break;
                case NemesisArchetype.Butcher:
                    raid *= 1.4f;
                    assault *= 1.55f;
                    kidnap *= 1.5f;
                    break;
                case NemesisArchetype.Saboteur:
                    sabotage *= 1.55f;
                    food *= 1.4f;
                    tamper *= 1.5f;
                    break;
                case NemesisArchetype.Trickster:
                    fake *= 1.5f;
                    informant *= 1.4f;
                    grave *= 1.45f;
                    break;
            }
        }

        /// <summary>Read live pawn traits once — Bloodlust bumps assault; Pyromaniac bumps sabotage.</summary>
        private static void ApplyTraitBias(Pawn nemesis, ref float assault, ref float sabotage)
        {
            if (nemesis?.story?.traits == null) return;
            TraitSet traits = nemesis.story.traits;
            if (traits.HasTrait(TraitDefOf.Bloodlust))
                assault *= 1.45f;
            TraitDef pyro = DefDatabase<TraitDef>.GetNamedSilentFail("Pyromaniac");
            if (pyro != null && traits.HasTrait(pyro))
                sabotage *= 1.35f;
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
