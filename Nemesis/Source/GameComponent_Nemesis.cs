using System.Collections.Generic;
using RimWorld;
using RimWorld.Planet;
using UnityEngine;
using Verse;

namespace Nemesis
{
    /// <summary>
    /// Hunt runtime. Core loop and persistence from Dredd; extended with end conditions,
    /// deferred fake ambushes, target tracking, and several concurrent hunts (one per hostile
    /// faction, capped in settings). All timing is keyed off synced game state, never the
    /// map a client is looking at, so Multiplayer clients tick the same way.
    /// </summary>
    public class GameComponent_Nemesis : GameComponent
    {
        public static GameComponent_Nemesis Instance => Current.Game?.GetComponent<GameComponent_Nemesis>();

        /// <summary>Save layout. 1 = 1.x single <c>nemesisData</c>; 2 = hunt list (2.0.0).</summary>
        private const int CurrentSaveVersion = 2;

        private List<NemesisData> hunts = new List<NemesisData>();
        public List<NemesisEpitaph> epitaphs = new List<NemesisEpitaph>();
        private int nextHuntId = 1;
        private int lastHuntStartTick = -9999999;
        private int lastThreatTick = -9999999;
        private int saveVersion = CurrentSaveVersion;
        private string lastNewsVersion;

        // 1.x layout, read once on load and folded into the hunt list.
        private NemesisData legacyData;
        private int legacyBounty;

        public IReadOnlyList<NemesisData> Hunts => hunts;

        /// <summary>Any hunt is active, in truce, or waiting on the capture dialog.</summary>
        public bool IsEngaged
        {
            get
            {
                for (int i = 0; i < hunts.Count; i++)
                {
                    if (hunts[i].Engaged) return true;
                }
                return false;
            }
        }

        /// <summary>Tick of the last nemesis raid or assault, any hunt.</summary>
        public int LastThreatTick => lastThreatTick;

        private static int MaxEscapes => NemesisMod.Settings?.maxEscapes ?? 4;
        private const int EscapeMoodThreshold = 2;
        private const int HealthCheckInterval = 120;
        private const int ThreatSpacingTicks = 30000;

        public GameComponent_Nemesis(Game game)
        {
        }

        public override void FinalizeInit()
        {
            base.FinalizeInit();
            SoftCompat.ResetCaches();
            NemesisRegistry.Clear();
            if (saveVersion < CurrentSaveVersion)
            {
                UnpinLeakedNemeses();
                saveVersion = CurrentSaveVersion;
            }
            for (int i = 0; i < hunts.Count; i++)
            {
                NemesisData h = hunts[i];
                h.ClearCaches();
                if (h.pendingResolution)
                    TryOpenResolutionDialog(h);
                else if (h.active)
                {
                    NemesisTells.RollIfNeeded(h, FindNemesisPawnFor(h));
                    CheckNemesisStillExists(h);
                }
            }
            PruneEndedHunts();
            UpdateNewsLetter.TrySend(ref lastNewsVersion);
        }

        public override void GameComponentTick()
        {
            if (hunts.Count == 0) return;
            int tick = Find.TickManager.TicksGame;
            bool resolveDirty = NemesisRegistry.ResolutionDirty;
            NemesisRegistry.ResolutionDirty = false;

            for (int i = 0; i < hunts.Count; i++)
                TickHunt(hunts[i], tick, resolveDirty);

            PruneEndedHunts();
        }

        private void TickHunt(NemesisData h, int tick, bool resolveDirty)
        {
            // Spread each hunt's periodic work over different ticks.
            int t = tick + h.huntId * 37;

            if (!h.active)
            {
                if (h.pendingResolution)
                {
                    if (t % 250 == 0)
                    {
                        Pawn captive = FindNemesisPawnAnywhere(h);
                        if (!IsHeldCaptive(captive))
                        {
                            HandleLostCustody(h, captive);
                            return;
                        }
                    }
                    if (t % 60 == 0)
                        TryOpenResolutionDialog(h);
                    return;
                }
                if (h.truceUntilTick > 0 && tick >= h.truceUntilTick)
                    ResumeFromTruce(h);
                return;
            }

            // Daily aggression climb.
            if (t % 60000 == 0)
            {
                h.aggressionLevel = Mathf.Min(
                    h.aggressionLevel + (NemesisMod.Settings?.escalationRatePerDay ?? 0.06f),
                    10f);
            }

            if (h.pendingFakeAmbush && tick >= h.fakeAmbushTick)
            {
                Map ambushMap = SoftCompat.PreferHarassmentMap(Find.AnyPlayerHomeMap);
                if (ambushMap != null)
                {
                    NemesisActions.ResolvePendingFakeAmbush(h, ambushMap);
                    NoteThreat();
                }
                else
                {
                    h.pendingFakeAmbush = false;
                    h.fakeAmbushTick = -1;
                }
            }

            // Fixed cadence: a per-client view (Find.CurrentMap) must never pick the tick (AZR-658).
            if (t % HealthCheckInterval == 0)
                CheckNemesisHealth(h);
            if (!h.active) return;

            if (t % 500 == 0 || resolveDirty)
            {
                CheckEndConditions(h);
                if (h.active)
                    CheckResolution(h);
            }
            if (!h.active) return;

            if (t % 2500 == 0)
            {
                CheckNemesisStillExists(h);
                if (h.active && !h.rogue)
                    CheckRogue(h);
            }

            if (h.active && tick >= h.nextActionTick)
                FireNextAction(h);
        }

        private void PruneEndedHunts()
        {
            for (int i = hunts.Count - 1; i >= 0; i--)
            {
                if (!hunts[i].Engaged)
                    hunts.RemoveAt(i);
            }
        }

        // --- Hunt start ---

        /// <summary>
        /// True when a new hunt for <paramref name="faction"/> may start: under the global cap,
        /// past the cooldown since the last hunt began, and that faction has no hunt yet.
        /// </summary>
        public bool CanStartHunt(Faction faction) => CanStartHunt(faction, ignoreLimits: false);

        private bool CanStartHunt(Faction faction, bool ignoreLimits)
        {
            if (faction == null || faction.IsPlayer) return false;
            NemesisSettings s = NemesisMod.Settings;
            int cap = Mathf.Clamp(s?.maxConcurrentHunts ?? 2, 1, 3);
            int engaged = 0;
            for (int i = 0; i < hunts.Count; i++)
            {
                NemesisData h = hunts[i];
                if (!h.Engaged) continue;
                engaged++;
                if (h.faction == faction) return false;
            }
            if (ignoreLimits) return true;
            if (engaged >= cap) return false;
            int cooldown = Mathf.RoundToInt((s?.huntCooldownDays ?? 3f) * 60000f);
            return Find.TickManager.TicksGame - lastHuntStartTick >= cooldown;
        }

        /// <summary>Starts a hunt; returns it, or null when none could start.</summary>
        /// <param name="ignoreLimits">Dev mode: skip the global cap and cooldown (one hunt per faction still holds).</param>
        public NemesisData CreateNemesis(Pawn sourcePawn, NemesisTargetMode mode, NemesisTrigger trigger,
            Pawn targetPawn = null, Pawn useAsNemesis = null, bool ignoreLimits = false)
        {
            Pawn candidate = useAsNemesis ?? sourcePawn;
            Faction faction = candidate?.Faction;
            if (!CanStartHunt(faction, ignoreLimits)) return null;
            // Exclusive claim: never start a hunt on a Rimesis / BFV captain, or on another hunt's nemesis.
            if (SoftCompat.IsForeignAntagonistPawn(candidate) || EngagedHuntForNemesis(candidate) != null) return null;

            Pawn nemesis;
            if (useAsNemesis != null && !useAsNemesis.Dead && !useAsNemesis.Destroyed
                && useAsNemesis.RaceProps.Humanlike)
            {
                nemesis = useAsNemesis;
            }
            else
            {
                PawnKindDef kind = faction.RandomPawnKind();
                if (kind == null || !kind.RaceProps.Humanlike)
                    kind = PawnKindDefOf.SpaceRefugee;

                PawnGenerationRequest request = new PawnGenerationRequest(
                    kind, faction, PawnGenerationContext.NonPlayer,
                    forceGenerateNewPawn: true, canGeneratePawnRelations: false, allowDead: false);
                nemesis = PawnGenerator.GeneratePawn(request);
            }

            int now = Find.TickManager.TicksGame;
            // Claim the hunt before parking so stacked Kill prefixes/postfixes in the
            // same combat beat cannot open a second hunt for this faction.
            NemesisData h = new NemesisData
            {
                huntId = nextHuntId++,
                active = true,
                trigger = trigger,
                targetMode = mode,
                aggressionLevel = 1f,
                nextActionTick = now + 120000,
                // Treat wounded-escape create as the escape beat so queued Kill
                // calls do not also spam "{name} Escapes".
                lastEscapeTick = trigger == NemesisTrigger.WoundedAndEscaped ? now : -999999,
                faction = faction,
                factionName = faction.Name,
                nemesisPawnId = nemesis.thingIDNumber,
                nemesisPawn = nemesis,
                nemesisName = nemesis.Name?.ToStringShort ?? nemesis.LabelShort,
                targetPawnId = targetPawn?.thingIDNumber ?? -1,
                targetPawn = targetPawn,
                targetPawnName = targetPawn?.LabelShort,
                combatFocus = NemesisProgression.RollCombatFocus(),
                progressionLevel = 0,
                appliedProgressionLevel = -1,
                huntStartTick = now,
                cachedNemesis = nemesis,
                cachedTarget = targetPawn,
            };
            hunts.Add(h);
            lastHuntStartTick = now;

            NemesisPawnUtil.ParkAsWorldNemesis(nemesis);
            NemesisPawnUtil.EnsureHuntFaction(nemesis, faction);
            NemesisTells.RollIfNeeded(h, nemesis);
            NemesisProgression.Apply(nemesis, h);
            NemesisTells.RecordGear(h, nemesis);

            SendIntroLetter(h, targetPawn);
            return h;
        }

        private void ResumeFromTruce(NemesisData h)
        {
            h.active = true;
            h.truceUntilTick = -1;
            h.nextActionTick = Find.TickManager.TicksGame + 120000;

            Find.LetterStack.ReceiveLetter(
                "Nemesis_Letter_TruceBrokenTitle".Translate(h.ColoredName),
                "Nemesis_Letter_TruceBrokenBody".Translate(h.nemesisName),
                LetterDefOf.ThreatBig);
        }

        // --- Lookups ---

        /// <summary>The active hunt whose nemesis this is, or null.</summary>
        public NemesisData HuntForNemesis(Pawn pawn)
        {
            if (pawn == null) return null;
            int id = pawn.thingIDNumber;
            for (int i = 0; i < hunts.Count; i++)
            {
                if (hunts[i].active && hunts[i].nemesisPawnId == id) return hunts[i];
            }
            return null;
        }

        /// <summary>Any engaged hunt (active, truce, pending) for this pawn.</summary>
        public NemesisData EngagedHuntForNemesis(Pawn pawn)
        {
            if (pawn == null) return null;
            int id = pawn.thingIDNumber;
            for (int i = 0; i < hunts.Count; i++)
            {
                if (hunts[i].Engaged && hunts[i].nemesisPawnId == id) return hunts[i];
            }
            return null;
        }

        public NemesisData HuntById(int huntId)
        {
            for (int i = 0; i < hunts.Count; i++)
            {
                if (hunts[i].huntId == huntId) return hunts[i];
            }
            return null;
        }

        public bool IsNemesisPawn(Pawn pawn) => HuntForNemesis(pawn) != null;

        public bool IsTargetPawn(Pawn pawn) => HuntsTargeting(pawn) != null;

        /// <summary>First active pawn-mode hunt fixated on this colonist.</summary>
        public NemesisData HuntsTargeting(Pawn pawn)
        {
            if (pawn == null) return null;
            int id = pawn.thingIDNumber;
            for (int i = 0; i < hunts.Count; i++)
            {
                NemesisData h = hunts[i];
                if (h.active && h.targetMode == NemesisTargetMode.Pawn && h.targetPawnId == id)
                    return h;
            }
            return null;
        }

        // --- Combat ---

        /// <summary>True cancels the killing blow. Off-map and unspawned hits are allowed to land.</summary>
        public bool CancelLethalHit(Pawn nemesis)
        {
            NemesisData h = HuntForNemesis(nemesis);
            if (h == null || nemesis.Destroyed) return false;
            if (!nemesis.Spawned || nemesis.Map == null)
            {
                EndHunt(h, NemesisEndReason.Killed, nemesis);
                return false;
            }
            HandleLethalDamage(h, nemesis);
            return true;
        }

        internal void HandleLethalDamage(NemesisData h, Pawn nemesis)
        {
            // Already fled — multi-hit Kill spam must not re-letter.
            if (!nemesis.Spawned || nemesis.Map == null) return;
            if (Find.TickManager.TicksGame - h.lastEscapeTick < 180) return;

            if (h.escapeCount >= MaxEscapes)
                SubdueNemesis(h, nemesis, fromLethalDamage: true);
            else
                FireEscape(h, nemesis);
        }

        private void FireEscape(NemesisData h, Pawn nemesis)
        {
            if (!h.active || nemesis == null || nemesis.Destroyed) return;
            if (!nemesis.Spawned || nemesis.Map == null) return;
            if (nemesis.Faction == null || nemesis.Faction.IsPlayer || nemesis.IsPrisonerOfColony) return;
            if (Find.TickManager.TicksGame - h.lastEscapeTick < 180) return;

            Map map = nemesis.Map;
            IntVec3 pos = nemesis.Position;

            // Must leave the assault lord before PassToWorld or LordTick spams
            // "owns a free world pawn".
            NemesisPawnUtil.ParkAsWorldNemesis(nemesis);
            NemesisPawnUtil.EnsureHuntFaction(nemesis, h.faction);
            h.cachedNemesis = nemesis;

            h.escapeCount++;
            h.aggressionLevel = Mathf.Min(h.aggressionLevel + 0.5f, 10f);
            h.lastEscapeTick = Find.TickManager.TicksGame;
            h.nextActionTick = Find.TickManager.TicksGame + 120000;
            NemesisTells.RecordSighting(h, map);
            NemesisTells.RecordGear(h, nemesis);
            NemesisProgression.LevelUpOnEscape(h, nemesis);
            ApplyEscapeMoodFallout(h);

            Find.LetterStack.ReceiveLetter(
                "Nemesis_Letter_EscapeTitle".Translate(h.ColoredName),
                NemesisTaunts.EscapeLetterBody(h),
                LetterDefOf.NeutralEvent,
                new GlobalTargetInfo(pos, map));
        }

        /// <summary>Repeat escapes cost colony mood; the fixation target takes it hardest.</summary>
        private static void ApplyEscapeMoodFallout(NemesisData h)
        {
            if (h.escapeCount < EscapeMoodThreshold) return;
            ThoughtDef def = DefDatabase<ThoughtDef>.GetNamedSilentFail("Nemesis_EscapedAgain");
            if (def == null) return;

            List<Pawn> colonists = PawnsFinder.AllMaps_FreeColonistsSpawned;
            for (int i = 0; i < colonists.Count; i++)
            {
                Pawn p = colonists[i];
                if (p?.needs?.mood?.thoughts?.memories == null) continue;
                int stage = h.targetMode == NemesisTargetMode.Pawn && p.thingIDNumber == h.targetPawnId ? 1 : 0;
                p.needs.mood.thoughts.memories.TryGainMemory(ThoughtMaker.MakeThought(def, stage));
            }
        }

        private static void SubdueNemesis(NemesisData h, Pawn nemesis, bool fromLethalDamage)
        {
            if (nemesis == null || nemesis.Dead) return;

            List<Hediff> hediffs = nemesis.health.hediffSet.hediffs;
            for (int i = hediffs.Count - 1; i >= 0; i--)
            {
                if (hediffs[i] is Hediff_Injury injury && injury.Bleeding)
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

            if (!h.corneredAnnounced)
            {
                h.corneredAnnounced = true;
                Find.LetterStack.ReceiveLetter(
                    "Nemesis_Letter_CorneredTitle".Translate(h.ColoredName),
                    "Nemesis_Letter_CorneredBody".Translate(h.nemesisName),
                    LetterDefOf.PositiveEvent,
                    nemesis);
            }
        }

        private void CheckNemesisHealth(NemesisData h)
        {
            Pawn nemesis = FindNemesisPawnFor(h);
            if (nemesis == null || !nemesis.Spawned || nemesis.Dead || nemesis.IsPrisonerOfColony) return;

            float hp = nemesis.health.summaryHealth.SummaryHealthPercent;

            if (h.escapeCount >= MaxEscapes)
            {
                if (hp < 0.45f && !nemesis.Downed)
                    SubdueNemesis(h, nemesis, fromLethalDamage: false);
            }
            else if (hp < NemesisTells.FleeHealth(h))
            {
                // Flee-when-losing for on-map assaults (same latch as Kill spam).
                if (Find.TickManager.TicksGame - h.lastEscapeTick >= 180)
                    FireEscape(h, nemesis);
            }
        }

        // --- End conditions ---

        private void CheckEndConditions(NemesisData h)
        {
            if (!h.active) return;

            if (h.targetMode == NemesisTargetMode.Pawn && h.targetPawnId > 0)
            {
                Pawn target = FindTargetPawn(h);
                if (target == null || target.Dead)
                {
                    EndHunt(h, NemesisEndReason.TargetDied, look: null);
                    return;
                }

                // Kidnapped colonists keep the player faction, so the hand-over check below misses them.
                if (target.IsKidnapped())
                {
                    EndHunt(h, NemesisEndReason.TargetHandedOver, look: null);
                    return;
                }

                if (target.Faction != Faction.OfPlayer && !target.IsColonist && !target.IsPrisonerOfColony)
                {
                    EndHunt(h, NemesisEndReason.TargetHandedOver, target);
                    return;
                }
            }

            Pawn nemesis = FindNemesisPawnFor(h);
            if (nemesis != null && nemesis.Dead && h.active)
                EndHunt(h, NemesisEndReason.Killed, nemesis);
        }

        private void CheckNemesisStillExists(NemesisData h)
        {
            if (!h.active || h.nemesisPawnId <= 0) return;
            Pawn nemesis = FindNemesisPawnAnywhere(h);
            if (nemesis == null)
                EndHunt(h, NemesisEndReason.Vanished, null);
            else if (nemesis.Dead)
                EndHunt(h, NemesisEndReason.Killed, nemesis);
        }

        private void CheckResolution(NemesisData h)
        {
            Pawn nemesis = FindNemesisPawnFor(h);
            if (nemesis == null || !nemesis.IsPrisonerOfColony) return;

            // Keep pendingResolution across save so the dialog can reopen.
            h.pendingResolution = true;
            h.active = false;
            TryOpenResolutionDialog(h, nemesis);
        }

        /// <summary>Reopen the capture dialog after load or if the modal was dismissed.</summary>
        public bool TryOpenResolutionDialog(NemesisData h, Pawn nemesis = null)
        {
            if (h == null || !h.pendingResolution) return false;
            nemesis ??= FindNemesisPawnFor(h);
            if (!IsHeldCaptive(nemesis)) return false;
            WindowStack stack = Find.WindowStack;
            if (stack == null) return false;
            for (int i = 0; i < stack.Windows.Count; i++)
            {
                if (stack.Windows[i] is Dialog_NemesisResolution open && open.HuntId == h.huntId)
                    return true;
            }
            stack.Add(new Dialog_NemesisResolution(h, nemesis));
            return true;
        }

        private static bool IsHeldCaptive(Pawn pawn) =>
            pawn != null && !pawn.Dead && !pawn.Destroyed && pawn.IsPrisonerOfColony;

        /// <summary>Also checks carried / caravan / transporter / dead pawns that <see cref="FindNemesisPawnFor(NemesisData)"/> skips.</summary>
        private Pawn FindNemesisPawnAnywhere(NemesisData h)
        {
            Pawn found = FindNemesisPawnFor(h);
            if (found != null) return found;
            int id = h.nemesisPawnId;
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
        /// another mod). Without this the hunt stays pending forever.
        /// </summary>
        private void HandleLostCustody(NemesisData h, Pawn nemesis)
        {
            string name = h.nemesisName ?? "Nemesis_Phrase_Someone".Translate();
            int now = Find.TickManager.TicksGame;

            if (nemesis != null && !nemesis.Dead && !nemesis.Destroyed
                && nemesis.Faction != null && !nemesis.Faction.IsPlayer)
            {
                h.pendingResolution = false;
                h.active = true;
                h.lastEscapeTick = now;
                h.nextActionTick = now + 120000;
                h.ClearCaches();
                NemesisTells.RecordNote(h, "Nemesis_Note_CustodyEscaped".Translate());
                Find.LetterStack.ReceiveLetter(
                    "Nemesis_Letter_CustodyEscapedTitle".Translate(h.ColoredName),
                    "Nemesis_Letter_CustodyEscapedBody".Translate(name),
                    LetterDefOf.ThreatBig,
                    nemesis.Spawned ? nemesis : null);
                return;
            }

            bool died = nemesis != null && nemesis.Dead;
            RecordEpitaph(h, died ? "Nemesis_End_DiedInCustody" : "Nemesis_End_LostCustody");
            CloseHunt(h);
            Find.LetterStack.ReceiveLetter(
                "Nemesis_Letter_CustodyLostTitle".Translate(h.ColoredName),
                (died ? "Nemesis_Letter_CustodyDiedBody" : "Nemesis_Letter_CustodyLostBody").Translate(name),
                LetterDefOf.NeutralEvent);
        }

        private static void CloseHunt(NemesisData h)
        {
            h.active = false;
            h.pendingResolution = false;
            h.pendingFakeAmbush = false;
            h.truceUntilTick = -1;
            h.bountySilver = 0;
            h.ClearCaches();
        }

        public void EndHunt(NemesisData h, NemesisEndReason reason, Thing look = null)
        {
            if (h == null) return;

            string name = h.nemesisName ?? "Nemesis_Phrase_Someone".Translate();
            string title = h.ColoredName;
            RecordEpitaph(h, reason);
            Pawn nemesis = FindNemesisPawnFor(h);
            CloseHunt(h);

            switch (reason)
            {
                case NemesisEndReason.Killed:
                    Find.LetterStack.ReceiveLetter(
                        "Nemesis_Letter_EndedKilledTitle".Translate(title),
                        "Nemesis_Letter_EndedKilledBody".Translate(name),
                        LetterDefOf.PositiveEvent,
                        look);
                    if (look is not Pawn body || !body.Spawned)
                        ReleaseNemesisWorldPawn(nemesis);
                    break;
                case NemesisEndReason.Vanished:
                    Find.LetterStack.ReceiveLetter(
                        "Nemesis_Letter_EndedVanishedTitle".Translate(title),
                        "Nemesis_Letter_EndedVanishedBody".Translate(name),
                        LetterDefOf.NeutralEvent);
                    ReleaseNemesisWorldPawn(nemesis);
                    break;
                case NemesisEndReason.TargetDied:
                    Find.LetterStack.ReceiveLetter(
                        "Nemesis_Letter_EndedTargetDiedTitle".Translate(title),
                        "Nemesis_Letter_EndedTargetDiedBody".Translate(name, h.targetPawnName ?? "Nemesis_Phrase_Someone".Translate()),
                        LetterDefOf.NegativeEvent);
                    ReleaseNemesisWorldPawn(nemesis);
                    break;
                case NemesisEndReason.TargetHandedOver:
                    Find.LetterStack.ReceiveLetter(
                        "Nemesis_Letter_EndedHandedTitle".Translate(title),
                        "Nemesis_Letter_EndedHandedBody".Translate(name, h.targetPawnName ?? "Nemesis_Phrase_Someone".Translate()),
                        LetterDefOf.NeutralEvent,
                        look);
                    ReleaseNemesisWorldPawn(nemesis);
                    break;
                case NemesisEndReason.Cleared:
                    ReleaseNemesisWorldPawn(nemesis);
                    break;
            }
        }

        /// <summary>Ends every engaged hunt (debug and the series hub's clear-all).</summary>
        public void EndAllHunts(NemesisEndReason reason)
        {
            for (int i = 0; i < hunts.Count; i++)
            {
                if (hunts[i].Engaged)
                    EndHunt(hunts[i], reason);
            }
            PruneEndedHunts();
        }

        private static void ReleaseNemesisWorldPawn(Pawn nemesis)
        {
            if (nemesis == null || nemesis.Dead) return;
            if (nemesis.IsPrisonerOfColony) return;
            NemesisPawnUtil.DetachFromLord(nemesis);
            if (nemesis.Spawned && !nemesis.Dead)
                nemesis.DeSpawn(DestroyMode.Vanish);
            if (nemesis.IsWorldPawn())
                Find.WorldPawns.RemovePawn(nemesis);
            if (!nemesis.Dead && !nemesis.Destroyed && !nemesis.IsWorldPawn())
                Find.WorldPawns.PassToWorld(nemesis, PawnDiscardDecideMode.Decide);
        }

        /// <summary>
        /// 1.x leaked KeepForever pins on finished nemeses. On the first 2.0 load, unpin any
        /// battle-hardened world pawn that no hunt owns so world-pawn GC can collect it.
        /// </summary>
        private void UnpinLeakedNemeses()
        {
            HediffDef marker = DefDatabase<HediffDef>.GetNamedSilentFail("Nemesis_BattleHardened");
            if (marker == null || Find.WorldPawns == null) return;
            List<Pawn> kept = new List<Pawn>(Find.WorldPawns.ForcefullyKeptPawns);
            int unpinned = 0;
            for (int i = 0; i < kept.Count; i++)
            {
                Pawn p = kept[i];
                if (p == null || p.Destroyed || EngagedHuntForNemesis(p) != null) continue;
                if (p.health?.hediffSet?.HasHediff(marker) != true) continue;
                Find.WorldPawns.RemovePawn(p);
                Find.WorldPawns.PassToWorld(p, PawnDiscardDecideMode.Decide);
                unpinned++;
            }
            if (unpinned > 0)
                Log.Message("[Nemesis] Save upgrade: released " + unpinned + " finished nemesis world pawn(s).");
        }

        // --- Rogue ---

        private void CheckRogue(NemesisData h)
        {
            if (h.faction != null && !h.faction.HostileTo(Faction.OfPlayer))
                GoRogue(h);
        }

        private void GoRogue(NemesisData h)
        {
            Faction old = h.faction;
            h.rogue = true;

            Faction rogueFaction = null;
            foreach (Faction f in Find.FactionManager.AllFactions)
            {
                if (f == null || f.IsPlayer || f.defeated || f.Hidden || f == old) continue;
                if (!f.def.humanlikeFaction || !f.HostileTo(Faction.OfPlayer)) continue;
                // A faction with its own hunt keeps it; the rogue looks elsewhere.
                if (HasHuntForFaction(f)) continue;
                rogueFaction = f;
                break;
            }

            string oldName = old?.Name ?? "Nemesis_Phrase_TheirFaction".Translate();
            if (rogueFaction == null)
            {
                Find.LetterStack.ReceiveLetter(
                    "Nemesis_Letter_RogueTitle".Translate(h.ColoredName),
                    "Nemesis_Letter_RogueBodyAlone".Translate(h.nemesisName, oldName),
                    LetterDefOf.ThreatBig);
                EndHunt(h, NemesisEndReason.Cleared, null);
                return;
            }

            FindNemesisPawnFor(h)?.SetFaction(rogueFaction);
            h.faction = rogueFaction;
            h.factionName = rogueFaction.Name;

            Find.LetterStack.ReceiveLetter(
                "Nemesis_Letter_RogueTitle".Translate(h.ColoredName),
                "Nemesis_Letter_RogueBodyFaction".Translate(h.nemesisName, oldName, rogueFaction.Name),
                LetterDefOf.ThreatBig);
        }

        private bool HasHuntForFaction(Faction f)
        {
            for (int i = 0; i < hunts.Count; i++)
            {
                if (hunts[i].Engaged && hunts[i].faction == f) return true;
            }
            return false;
        }

        // --- Actions ---

        private void NoteThreat() => lastThreatTick = Find.TickManager.TicksGame;

        private void FireNextAction(NemesisData h)
        {
            int now = Find.TickManager.TicksGame;
            Map map = SoftCompat.PreferHarassmentMap(Find.AnyPlayerHomeMap);
            if (map == null)
            {
                h.nextActionTick = now + 60000;
                return;
            }

            Pawn nemesisPawn = FindNemesisPawnFor(h);
            if (nemesisPawn != null && nemesisPawn.IsPrisonerOfColony)
            {
                h.nextActionTick = now + 1000;
                NemesisRegistry.ResolutionDirty = true;
                return;
            }

            // Mod-local: defer piling onto an already-hot raid map.
            if (NemesisRegistry.ShouldDeferActions(map))
            {
                h.nextActionTick = now + 7500;
                return;
            }

            // Don't fire while nemesis is personally on-map fighting / fleeing.
            if (nemesisPawn != null && nemesisPawn.Spawned && !nemesisPawn.Downed)
            {
                h.nextActionTick = now + 5000;
                return;
            }

            NemesisAction action = PickAction(h, map);
            // Two hunts never raid the colony within half a day of each other.
            if (IsThreat(action) && now - lastThreatTick < ThreatSpacingTicks)
            {
                h.nextActionTick = lastThreatTick + ThreatSpacingTicks + 2500;
                return;
            }

            NemesisActions.Execute(action, h, map);
            if (IsThreat(action))
                NoteThreat();
            int wait = ActionInterval(h);
            if (action == NemesisAction.DoNothing)
                wait = Mathf.Max(15000, Mathf.RoundToInt(wait * Rand.Range(0.5f, 1.85f)));
            h.nextActionTick = now + wait;
        }

        private static bool IsThreat(NemesisAction a) =>
            a == NemesisAction.DirectRaid || a == NemesisAction.NemesisAssault
            || a == NemesisAction.KidnapAttempt || a == NemesisAction.AnomalyBait;

        private static NemesisAction PickAction(NemesisData h, Map map)
        {
            float agg = h.EffectiveAggression;
            NemesisSettings s = NemesisMod.Settings;
            float taunt = s?.actionWeightTaunt ?? 0.35f;
            float idle = s?.actionWeightIdle ?? 0.22f;
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
            float kidnap = agg >= 5f && NemesisActions.CanKidnap(h, map)
                ? (s?.actionWeightKidnap ?? 0.05f)
                : 0f;

            if (h.rogue)
            {
                raid *= 0.35f;
                assault *= 1.6f;
                sabotage *= 1.3f;
            }

            // After escapes: captain returns — army raids up, petty sabotage down.
            if (h.escapeCount > 0)
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

            // Soft Strata: slight bias toward food raids when harassing multi-level bases.
            if (SoftCompat.StrataActive)
                food *= 1.15f;

            if (!NemesisActions.HasCommsConsole(map))
            {
                idle += taunt;
                taunt = 0f;
            }

            if (h.habit == NemesisHabit.PowerFirst)
                sabotage *= 2.2f;
            else if (h.habit == NemesisHabit.FoodFirst)
                food *= 2.2f;
            else if (h.habit == NemesisHabit.SameBuilding)
                sabotage *= 1.4f;

            float total = idle + taunt + raid + assault + waste + fake + caravan + sabotage + food + anomaly + kidnap;
            if (total <= 0f)
                return NemesisAction.DoNothing;
            float roll = Rand.Value * total;

            if ((roll -= idle) < 0f) return NemesisAction.DoNothing;
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

        private static int ActionInterval(NemesisData h) => Mathf.Max(
            NemesisMod.Settings?.minActionCooldownTicks ?? 90000,
            (int)(300000f / h.EffectiveAggression));

        // --- Pawn lookup ---

        /// <summary>
        /// Nemesis of the first engaged hunt. Kept for the Azrael series hub, which reads it
        /// by reflection without argument types, so it must stay the only FindNemesisPawn overload.
        /// Nemesis code uses <see cref="FindNemesisPawnFor(NemesisData)"/>.
        /// </summary>
        public Pawn FindNemesisPawn()
        {
            for (int i = 0; i < hunts.Count; i++)
            {
                if (hunts[i].Engaged) return FindNemesisPawnFor(hunts[i]);
            }
            return null;
        }

        public Pawn FindNemesisPawnFor(NemesisData h)
        {
            if (h == null) return null;
            int id = h.nemesisPawnId;
            Pawn cached = h.cachedNemesis;
            if (cached != null && !cached.Destroyed && cached.thingIDNumber == id)
                return cached;
            Pawn saved = h.nemesisPawn;
            Pawn found = saved != null && !saved.Destroyed && saved.thingIDNumber == id
                ? saved
                : FindPawnById(id);
            h.cachedNemesis = found;
            if (found != null) h.nemesisPawn = found;
            return found;
        }

        public Pawn FindTargetPawn(NemesisData h)
        {
            if (h == null || h.targetPawnId < 0) return null;
            int id = h.targetPawnId;
            Pawn cached = h.cachedTarget;
            if (cached != null && !cached.Destroyed && cached.thingIDNumber == id)
                return cached;
            Pawn saved = h.targetPawn;
            Pawn found = saved != null && !saved.Destroyed && saved.thingIDNumber == id
                ? saved
                : FindPawnById(id);
            h.cachedTarget = found;
            if (found != null) h.targetPawn = found;
            return found;
        }

        private static Pawn FindPawnById(int id)
        {
            if (id < 0) return null;
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

        private static void SendIntroLetter(NemesisData h, Pawn targetPawn)
        {
            string body;
            string fac = h.factionName;
            string nem = h.nemesisName;
            if (h.targetMode == NemesisTargetMode.Pawn && targetPawn != null)
            {
                body = h.trigger switch
                {
                    NemesisTrigger.KilledAlly =>
                        "Nemesis_Intro_KilledAlly".Translate(targetPawn.LabelShort, fac, nem),
                    NemesisTrigger.WoundedAndEscaped =>
                        "Nemesis_Intro_Wounded".Translate(targetPawn.LabelShort, nem, fac),
                    NemesisTrigger.Fixation =>
                        "Nemesis_Intro_Fixation".Translate(fac, nem, targetPawn.LabelShort),
                    _ =>
                        "Nemesis_Intro_PawnDefault".Translate(fac, targetPawn.LabelShort, nem),
                };
            }
            else
            {
                body = h.trigger switch
                {
                    NemesisTrigger.PrisonerEscaped =>
                        "Nemesis_Intro_Prisoner".Translate(nem, fac),
                    NemesisTrigger.SlaveEscaped =>
                        "Nemesis_Intro_Slave".Translate(nem, fac),
                    NemesisTrigger.FactionRetaliation =>
                        "Nemesis_Intro_Retaliation".Translate(fac, nem),
                    _ =>
                        "Nemesis_Intro_ColonyDefault".Translate(nem, fac),
                };
            }

            Find.LetterStack.ReceiveLetter(
                "Nemesis_Intro_TitleNamed".Translate(h.ColoredName),
                body,
                LetterDefOf.ThreatBig,
                targetPawn);
        }

        // --- Save ---

        public override void ExposeData()
        {
            base.ExposeData();
            // Absent in 1.x saves, so it loads as 1 there.
            Scribe_Values.Look(ref saveVersion, "saveVersion", 1);
            Scribe_Collections.Look(ref hunts, "hunts", LookMode.Deep);
            Scribe_Values.Look(ref nextHuntId, "nextHuntId", 1);
            Scribe_Values.Look(ref lastHuntStartTick, "lastHuntStartTick", -9999999);
            Scribe_Values.Look(ref lastThreatTick, "lastThreatTick", -9999999);
            Scribe_Values.Look(ref lastNewsVersion, "lastNewsVersion");
            Scribe_Collections.Look(ref epitaphs, "epitaphs", LookMode.Deep);

            if (Scribe.mode != LoadSaveMode.Saving && saveVersion < CurrentSaveVersion)
            {
                // 1.x: one hunt in "nemesisData", bounty on the component (AZR-552).
                Scribe_Deep.Look(ref legacyData, "nemesisData");
                Scribe_Values.Look(ref legacyBounty, "bountySilver", 0);
            }

            if (Scribe.mode == LoadSaveMode.PostLoadInit)
            {
                hunts ??= new List<NemesisData>();
                epitaphs ??= new List<NemesisEpitaph>();
                if (legacyData != null)
                {
                    if (legacyData.Engaged && hunts.Count == 0)
                    {
                        legacyData.huntId = nextHuntId++;
                        legacyData.bountySilver = legacyBounty;
                        hunts.Add(legacyData);
                    }
                    legacyData = null;
                }
                hunts.RemoveAll(h => h == null);
                for (int i = 0; i < hunts.Count; i++)
                {
                    if (hunts[i].huntId < 0) hunts[i].huntId = nextHuntId++;
                    if (hunts[i].huntId >= nextHuntId) nextHuntId = hunts[i].huntId + 1;
                }
            }
        }

        // --- Epitaphs ---

        public void RecordEpitaph(NemesisData h, NemesisEndReason reason) =>
            RecordEpitaph(h, EndReasonKey(reason));

        public void RecordEpitaph(NemesisData h, string endKey)
        {
            if (h == null) return;
            int days = 0;
            if (h.huntStartTick > 0)
                days = Mathf.Max(0, (Find.TickManager.TicksGame - h.huntStartTick) / 60000);
            var epitaph = new NemesisEpitaph
            {
                name = h.nemesisName,
                factionName = h.factionName,
                focusKey = h.FocusLabelKey,
                voiceKey = NemesisTells.VoiceKey(h.voice),
                markKey = NemesisTells.MarkKey(h.mark),
                habitKey = NemesisTells.HabitKey(h.habit),
                targetName = h.targetPawnName,
                endKey = endKey,
                escapes = h.escapeCount,
                days = days,
                tick = Find.TickManager.TicksGame,
            };
            epitaphs ??= new List<NemesisEpitaph>();
            epitaphs.Add(epitaph);
            while (epitaphs.Count > 12)
                epitaphs.RemoveAt(0);

            string title = "Nemesis_Epitaph_Title".Translate(epitaph.name ?? "Nemesis_Phrase_Someone".Translate());
            SoftCompat.OfferEpitaphToDeepColony(title, epitaph.SummaryLine());
        }

        private static string EndReasonKey(NemesisEndReason reason) => reason switch
        {
            NemesisEndReason.Killed => "Nemesis_End_Killed",
            NemesisEndReason.TargetDied => "Nemesis_End_TheyWon",
            NemesisEndReason.TargetHandedOver => "Nemesis_End_TheyWon",
            NemesisEndReason.Captured => "Nemesis_End_Captured",
            NemesisEndReason.Vanished => "Nemesis_End_Vanished",
            _ => "Nemesis_End_Cleared",
        };
    }
}
