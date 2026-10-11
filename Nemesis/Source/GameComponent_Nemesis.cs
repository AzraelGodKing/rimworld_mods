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
        public List<NemesisEpitaph> epitaphs = new List<NemesisEpitaph>();
        public int bountySilver;

        public NemesisData Data => _data;

        public bool IsEngaged =>
            _data != null && (_data.active || _data.truceUntilTick > 0 || _data.pendingResolution);

        private static int MaxEscapes => NemesisMod.Settings?.maxEscapes ?? 4;
        private const int EscapeMoodThreshold = 2;

        public GameComponent_Nemesis(Game game)
        {
        }

        private string lastNewsVersion;

        public override void FinalizeInit()
        {
            base.FinalizeInit();
            SoftCompat.ResetCaches();
            NemesisRegistry.Clear();
            if (_data != null && _data.pendingResolution)
                TryOpenResolutionDialog();
            else if (_data != null && _data.active)
                NemesisTells.RollIfNeeded(_data, FindNemesisPawn());
            UpdateNewsLetter.TrySend(ref lastNewsVersion);
        }

        public override void GameComponentTick()
        {
            NemesisPlayerCommand.Drain();
            if (_data == null) return;

            int tick = Find.TickManager.TicksGame;

            if (!_data.active)
            {
                if (_data.pendingResolution)
                {
                    if (tick % 250 == 0)
                    {
                        Pawn captive = FindNemesisPawnAnywhere();
                        if (!IsHeldCaptive(captive))
                        {
                            HandleLostCustody(captive);
                            return;
                        }
                    }
                    if (tick % 60 == 0)
                        TryOpenResolutionDialog();
                    return;
                }
                if (_data.truceUntilTick > 0 && tick >= _data.truceUntilTick)
                    ResumeFromTruce();
                return;
            }

            // Daily aggression climb — exact day boundary.
            if (tick % 60000 == 0)
            {
                _data.aggressionLevel = Mathf.Min(
                    _data.aggressionLevel + (NemesisMod.Settings?.escalationRatePerDay ?? 0.06f),
                    10f);
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

            // Stagger health / end-condition work. Faster on the viewed map.
            // PreferHarassmentMap reflects into Strata per map, so only resolve it on candidate ticks.
            if (tick % 120 == 0 || tick % 300 == 0)
            {
                Map home = SoftCompat.PreferHarassmentMap(Find.AnyPlayerHomeMap);
                int healthInterval = NemesisRegistry.MapIsViewed(home) ? 120 : 300;
                if (tick % healthInterval == 0)
                    CheckNemesisHealth();
            }

            if (tick % 500 == 0 || NemesisRegistry.ResolutionDirty)
            {
                NemesisRegistry.ResolutionDirty = false;
                CheckEndConditions();
                CheckResolution();
            }

            if (tick % 2500 == 0 && !_data.rogue)
                CheckRogue();

            if (tick >= _data.nextActionTick)
                FireNextAction();
        }

        public void CreateNemesis(Pawn sourcePawn, NemesisTargetMode mode, NemesisTrigger trigger,
            Pawn targetPawn = null, Pawn useAsNemesis = null)
        {
            if (IsEngaged) return;

            // Exclusive claim: never start a hunt on a Rimesis / BFV captain.
            Pawn candidate = useAsNemesis ?? sourcePawn;
            if (SoftCompat.IsForeignAntagonistPawn(candidate))
            {
                return;
            }

            // Claim the hunt immediately so stacked Kill prefixes/postfixes in the
            // same combat beat cannot open a second hunt (duplicate "A Nemesis Emerges").
            _data = new NemesisData
            {
                active = true,
                trigger = trigger,
                targetMode = mode,
                aggressionLevel = 1f,
                nextActionTick = Find.TickManager.TicksGame + 120000,
                // Treat wounded-escape create as the escape beat so queued Kill
                // calls do not also spam "{name} Escapes".
                lastEscapeTick = trigger == NemesisTrigger.WoundedAndEscaped
                    ? Find.TickManager.TicksGame
                    : -999999,
            };

            Pawn nemesis;
            Faction faction;

            if (useAsNemesis != null && !useAsNemesis.Dead && !useAsNemesis.Destroyed
                && useAsNemesis.RaceProps.Humanlike
                && useAsNemesis.Faction != null && !useAsNemesis.Faction.IsPlayer)
            {
                nemesis = useAsNemesis;
                faction = nemesis.Faction;
                NemesisPawnUtil.ParkAsWorldNemesis(nemesis);
                NemesisPawnUtil.EnsureHuntFaction(nemesis, faction);
            }
            else
            {
                faction = sourcePawn?.Faction;
                if (faction == null || faction.IsPlayer)
                {
                    _data.active = false;
                    return;
                }

                PawnKindDef kind = faction.RandomPawnKind();
                if (kind == null || !kind.RaceProps.Humanlike)
                    kind = PawnKindDefOf.SpaceRefugee;

                PawnGenerationRequest request = new PawnGenerationRequest(
                    kind, faction, PawnGenerationContext.NonPlayer,
                    forceGenerateNewPawn: true, canGeneratePawnRelations: false, allowDead: false);

                nemesis = PawnGenerator.GeneratePawn(request);
                NemesisPawnUtil.ParkAsWorldNemesis(nemesis);
                NemesisPawnUtil.EnsureHuntFaction(nemesis, faction);
            }

            NemesisRegistry.CachedNemesis = nemesis;
            NemesisRegistry.CachedNemesisId = nemesis.thingIDNumber;
            NemesisRegistry.CachedTarget = targetPawn;
            NemesisRegistry.CachedTargetId = targetPawn?.thingIDNumber ?? -1;

            _data.nemesisPawnId = nemesis.thingIDNumber;
            _data.nemesisName = nemesis.Name?.ToStringShort ?? nemesis.LabelShort;
            _data.factionName = faction.Name;
            _data.faction = faction;
            _data.targetPawnId = targetPawn?.thingIDNumber ?? -1;
            _data.targetPawnName = targetPawn?.LabelShort;
            _data.combatFocus = NemesisProgression.RollCombatFocus();
            _data.progressionLevel = 0;
            _data.appliedProgressionLevel = -1;
            _data.huntStartTick = Find.TickManager.TicksGame;
            NemesisTells.RollIfNeeded(_data, nemesis);
            NemesisProgression.Apply(nemesis, _data);
            NemesisTells.RecordGear(_data, nemesis);

            SendIntroLetter(targetPawn);
        }

        private void ResumeFromTruce()
        {
            _data.active = true;
            _data.truceUntilTick = -1;
            _data.nextActionTick = Find.TickManager.TicksGame + 120000;

            Find.LetterStack.ReceiveLetter(
                "Nemesis_Letter_TruceBrokenTitle".Translate(_data.nemesisName),
                "Nemesis_Letter_TruceBrokenBody".Translate(_data.nemesisName),
                LetterDefOf.ThreatBig);
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
            if (_data == null || !_data.active || nemesis == null || nemesis.Destroyed) return;
            // Already fled — multi-hit Kill spam must not re-letter.
            if (!nemesis.Spawned) return;
            if (Find.TickManager.TicksGame - _data.lastEscapeTick < 180) return;

            if (_data.escapeCount >= MaxEscapes)
                SubdueNemesis(nemesis, fromLethalDamage: true);
            else
                FireEscape(nemesis);
        }

        private void FireEscape(Pawn nemesis)
        {
            if (_data == null || !_data.active || nemesis == null || nemesis.Destroyed) return;
            if (!nemesis.Spawned || nemesis.Map == null) return;
            if (!nemesis.Map.IsPlayerHome) return;
            if (nemesis.Faction == null || nemesis.Faction.IsPlayer || nemesis.IsPrisonerOfColony) return;
            if (Find.TickManager.TicksGame - _data.lastEscapeTick < 180) return;

            Map map = nemesis.Map;
            IntVec3 pos = nemesis.Position;

            // Must leave the assault lord before PassToWorld or LordTick spams
            // "owns a free world pawn".
            NemesisPawnUtil.ParkAsWorldNemesis(nemesis);
            NemesisPawnUtil.EnsureHuntFaction(nemesis, _data.faction);

            NemesisRegistry.CachedNemesis = nemesis;
            NemesisRegistry.CachedNemesisId = nemesis.thingIDNumber;

            _data.escapeCount++;
            _data.aggressionLevel = Mathf.Min(_data.aggressionLevel + 0.5f, 10f);
            _data.lastEscapeTick = Find.TickManager.TicksGame;
            _data.nextActionTick = Find.TickManager.TicksGame + 120000;
            NemesisTells.RecordSighting(_data, map);
            NemesisTells.RecordGear(_data, nemesis);
            NemesisProgression.LevelUpOnEscape(_data, nemesis);
            ApplyEscapeMoodFallout();

            GlobalTargetInfo lookTarget = map != null ? new GlobalTargetInfo(pos, map) : GlobalTargetInfo.Invalid;

            Find.LetterStack.ReceiveLetter(
                "Nemesis_Letter_EscapeTitle".Translate(_data.nemesisName),
                NemesisTaunts.EscapeLetterBody(_data),
                LetterDefOf.NeutralEvent,
                lookTarget);
        }

        /// <summary>AZR-388 — repeat escapes cost colony mood; the fixation target takes it hardest.</summary>
        private void ApplyEscapeMoodFallout()
        {
            if (_data.escapeCount < EscapeMoodThreshold) return;
            ThoughtDef def = DefDatabase<ThoughtDef>.GetNamedSilentFail("Nemesis_EscapedAgain");
            if (def == null) return;

            List<Pawn> colonists = PawnsFinder.AllMaps_FreeColonistsSpawned;
            for (int i = 0; i < colonists.Count; i++)
            {
                Pawn p = colonists[i];
                if (p?.needs?.mood?.thoughts?.memories == null) continue;
                int stage = IsTargetPawn(p) ? 1 : 0;
                p.needs.mood.thoughts.memories.TryGainMemory(ThoughtMaker.MakeThought(def, stage));
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
            else if (hp < NemesisTells.FleeHealth(_data))
            {
                // Flee-when-losing for on-map assaults (same latch as Kill spam).
                if (Find.TickManager.TicksGame - _data.lastEscapeTick >= 180)
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

                // Kidnapped colonists keep the player faction, so the hand-over check below misses them.
                if (target.IsKidnapped())
                {
                    EndHunt(NemesisEndReason.TargetHandedOver, look: null);
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

            // AZR-296 — keep pendingResolution across save so the dialog can reopen.
            _data.pendingResolution = true;
            _data.active = false;
            TryOpenResolutionDialog(nemesis);
        }

        /// <summary>Reopen capture dialog after load / if the modal was dismissed by a crash.</summary>
        public bool TryOpenResolutionDialog(Pawn nemesis = null)
        {
            if (_data == null || !_data.pendingResolution) return false;
            if (NemesisPlayerCommand.HasQueuedResolution) return false;
            nemesis ??= FindNemesisPawn();
            if (!IsHeldCaptive(nemesis)) return false;
            if (Find.WindowStack?.WindowOfType<Dialog_NemesisResolution>() != null) return true;
            Find.WindowStack.Add(new Dialog_NemesisResolution(_data, nemesis));
            return true;
        }

        private static bool IsHeldCaptive(Pawn pawn) =>
            pawn != null && !pawn.Dead && !pawn.Destroyed && pawn.IsPrisonerOfColony;

        /// <summary>Also checks carried / caravan / transporter / dead pawns that <see cref="FindNemesisPawn"/> skips.</summary>
        private Pawn FindNemesisPawnAnywhere()
        {
            Pawn found = FindNemesisPawn();
            if (found != null) return found;
            int id = _data.nemesisPawnId;
            if (id < 0) return null;
            foreach (Pawn p in PawnsFinder.AllMapsWorldAndTemporary_AliveOrDead)
            {
                if (p != null && p.thingIDNumber == id)
                    return p;
            }
            return null;
        }

        /// <summary>
        /// Captive left custody before the player chose a fate (died, destroyed, traded, freed by
        /// another mod). Without this the hunt stays pending forever and no new hunt can start.
        /// </summary>
        private void HandleLostCustody(Pawn nemesis)
        {
            string name = _data.nemesisName ?? "Nemesis_Phrase_Someone".Translate();
            int now = Find.TickManager.TicksGame;

            if (nemesis != null && !nemesis.Dead && !nemesis.Destroyed
                && nemesis.Faction != null && !nemesis.Faction.IsPlayer)
            {
                _data.pendingResolution = false;
                _data.active = true;
                _data.lastEscapeTick = now;
                _data.nextActionTick = now + 120000;
                NemesisRegistry.Clear();
                NemesisTells.RecordNote(_data, "Nemesis_Note_CustodyEscaped".Translate());
                Find.LetterStack.ReceiveLetter(
                    "Nemesis_Letter_CustodyEscapedTitle".Translate(name),
                    "Nemesis_Letter_CustodyEscapedBody".Translate(name),
                    LetterDefOf.ThreatBig,
                    nemesis.Spawned ? nemesis : null);
                return;
            }

            bool died = nemesis != null && nemesis.Dead;
            RecordEpitaph(died ? "Nemesis_End_DiedInCustody" : "Nemesis_End_LostCustody");
            _data.active = false;
            _data.pendingResolution = false;
            _data.pendingFakeAmbush = false;
            _data.truceUntilTick = -1;
            bountySilver = 0;
            NemesisRegistry.Clear();
            Find.LetterStack.ReceiveLetter(
                "Nemesis_Letter_CustodyLostTitle".Translate(name),
                (died ? "Nemesis_Letter_CustodyDiedBody" : "Nemesis_Letter_CustodyLostBody").Translate(name),
                LetterDefOf.NeutralEvent);
        }

        public void ClearPendingResolution()
        {
            if (_data != null)
                _data.pendingResolution = false;
        }

        public void EndHunt(NemesisEndReason reason, Thing look = null)
        {
            if (_data == null) return;

            string name = _data.nemesisName ?? "Nemesis_Phrase_Someone".Translate();
            RecordEpitaph(reason);
            _data.active = false;
            _data.pendingResolution = false;
            _data.pendingFakeAmbush = false;
            _data.truceUntilTick = -1;
            bountySilver = 0;

            switch (reason)
            {
                case NemesisEndReason.Killed:
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
                    ReleaseNemesisWorldPawn();
                    break;
                case NemesisEndReason.TargetHandedOver:
                    Find.LetterStack.ReceiveLetter(
                        "Nemesis_Letter_EndedHandedTitle".Translate(name),
                        "Nemesis_Letter_EndedHandedBody".Translate(name, _data.targetPawnName ?? "Nemesis_Phrase_Someone".Translate()),
                        LetterDefOf.NeutralEvent,
                        look);
                    ReleaseNemesisWorldPawn();
                    break;
                case NemesisEndReason.Cleared:
                    ReleaseNemesisWorldPawn();
                    break;
            }

            NemesisRegistry.Clear();
        }

        private void ReleaseNemesisWorldPawn()
        {
            Pawn nemesis = FindNemesisPawn();
            if (nemesis == null || nemesis.Dead) return;
            if (nemesis.IsPrisonerOfColony) return;
            NemesisPawnUtil.DetachFromLord(nemesis);
            if (nemesis.Spawned)
                nemesis.DeSpawn(DestroyMode.Vanish);
            // Leave KeepForever pin if present — WorldPawns has no clean unpin API.
            // Cache clear below is enough for hunt end; pawn can GC later if unpinned elsewhere.
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

            NemesisAction action = PickAction(map);
            NemesisActions.Execute(action, _data, map);
            _data.nextActionTick = Find.TickManager.TicksGame + ActionInterval();
        }

        private NemesisAction PickAction(Map map)
        {
            float agg = _data.EffectiveAggression;
            NemesisSettings s = NemesisMod.Settings;
            float taunt = s?.actionWeightTaunt ?? 0.35f;
            float raid = s?.actionWeightRaid ?? 0.15f;
            float assault = agg >= 3f ? (s?.actionWeightAssault ?? 0.15f) : 0f;
            float waste = ModsConfig.BiotechActive ? (s?.actionWeightWaste ?? 0.08f) : 0f;
            float fake = s?.actionWeightFakeSignal ?? 0.10f;
            float caravan = s?.actionWeightCaravan ?? 0.07f;
            float sabotage = agg >= 2f ? (s?.actionWeightSabotage ?? 0.05f) : 0f;
            float food = s?.actionWeightFood ?? 0.05f;
            float anomaly = ModsConfig.AnomalyActive && agg >= 4f
                ? (s?.actionWeightAnomaly ?? 0.06f)
                : 0f;
            float kidnap = agg >= 5f && NemesisActions.CanKidnap(_data, map)
                ? (s?.actionWeightKidnap ?? 0.05f)
                : 0f;

            if (_data.rogue)
            {
                raid *= 0.35f;
                assault *= 1.6f;
                sabotage *= 1.3f;
            }

            // After escapes: captain returns — army raids up, petty sabotage down.
            if (_data.escapeCount > 0)
            {
                float pettyMul = s?.postEscapeSabotageWeightMul ?? 0.35f;
                raid *= 2.8f;
                assault *= 1.35f;
                taunt *= 0.7f;
                waste *= pettyMul;
                fake *= pettyMul;
                sabotage *= pettyMul;
                food *= pettyMul;
                caravan *= Mathf.Lerp(1f, pettyMul, 0.5f);
            }

            // Soft Strata: slight bias toward food/sabotage when harassing multi-level bases (surface map).
            if (SoftCompat.StrataActive)
                food *= 1.15f;

            if (_data.habit == NemesisHabit.PowerFirst)
                sabotage *= 2.2f;
            else if (_data.habit == NemesisHabit.FoodFirst)
                food *= 2.2f;
            else if (_data.habit == NemesisHabit.SameBuilding)
                sabotage *= 1.4f;

            float total = taunt + raid + assault + waste + fake + caravan + sabotage + food + anomaly + kidnap;
            float roll = Rand.Value * total;

            if ((roll -= taunt) < 0f) return NemesisAction.CommsTaunt;
            if ((roll -= raid) < 0f) return NemesisAction.DirectRaid;
            if ((roll -= assault) < 0f) return NemesisAction.NemesisAssault;
            if ((roll -= waste) < 0f) return NemesisAction.WastePackDrop;
            if ((roll -= fake) < 0f) return NemesisAction.FakeSignalAmbush;
            if ((roll -= caravan) < 0f) return NemesisAction.CaravanHarass;
            if ((roll -= sabotage) < 0f) return NemesisAction.PowerSabotage;
            if ((roll -= food) < 0f) return NemesisAction.FoodStoreRaid;
            if ((roll -= kidnap) < 0f) return NemesisAction.KidnapAttempt;
            return NemesisAction.AnomalyBait;
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
            Scribe_Values.Look(ref lastNewsVersion, "lastNewsVersion");
            Scribe_Values.Look(ref bountySilver, "bountySilver", 0);
            Scribe_Collections.Look(ref epitaphs, "epitaphs", LookMode.Deep);
            if (_data == null)
                _data = new NemesisData();
            if (epitaphs == null)
                epitaphs = new List<NemesisEpitaph>();
        }

        public void RecordEpitaph(NemesisEndReason reason)
        {
            RecordEpitaph(EndReasonKey(reason));
        }

        public void RecordEpitaph(string endKey)
        {
            if (_data == null)
                return;
            int days = 0;
            if (_data.huntStartTick > 0)
                days = Mathf.Max(0, (Find.TickManager.TicksGame - _data.huntStartTick) / 60000);
            var epitaph = new NemesisEpitaph
            {
                name = _data.nemesisName,
                factionName = _data.factionName,
                focusKey = _data.FocusLabelKey,
                voiceKey = NemesisTells.VoiceKey(_data.voice),
                markKey = NemesisTells.MarkKey(_data.mark),
                habitKey = NemesisTells.HabitKey(_data.habit),
                targetName = _data.targetPawnName,
                endKey = endKey,
                escapes = _data.escapeCount,
                days = days,
                tick = Find.TickManager.TicksGame,
            };
            if (epitaphs == null)
                epitaphs = new List<NemesisEpitaph>();
            epitaphs.Add(epitaph);
            while (epitaphs.Count > 12)
                epitaphs.RemoveAt(0);

            string title = "Nemesis_Epitaph_Title".Translate(epitaph.name ?? "Nemesis_Phrase_Someone".Translate());
            string body = epitaph.SummaryLine();
            SoftCompat.OfferEpitaphToDeepColony(title, body);
        }

        static string EndReasonKey(NemesisEndReason reason) => reason switch
        {
            NemesisEndReason.Killed => "Nemesis_End_Killed",
            NemesisEndReason.TargetDied => "Nemesis_End_TheyWon",
            NemesisEndReason.TargetHandedOver => "Nemesis_End_TheyWon",
            NemesisEndReason.Captured => "Nemesis_End_Captured",
            _ => "Nemesis_End_Cleared",
        };
    }
}
