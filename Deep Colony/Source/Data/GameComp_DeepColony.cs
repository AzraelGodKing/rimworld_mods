using System.Collections.Generic;
using DeepColony.Patches;
using RimWorld;
using Verse;

namespace DeepColony
{
    public class GameComp_DeepColony : GameComponent
    {
        public static GameComp_DeepColony Instance =>
            Verse.Current.Game?.GetComponent<GameComp_DeepColony>();

        private HashSet<int> inheritanceProcessed = new HashSet<int>();
        private HashSet<int> formerPlayerColonists = new HashSet<int>();
        private Dictionary<int, float> factionDriftBuffer = new Dictionary<int, float>();
        public List<FactionRepLedgerEntry> factionRepLedger = new List<FactionRepLedgerEntry>();

        /// <summary>B10 — thingIDNumber → owner name (heirloom markers).</summary>
        private Dictionary<int, string> heirloomOwners = new Dictionary<int, string>();
        private Dictionary<int, string> heirloomEchoPerks = new Dictionary<int, string>();
        /// <summary>AZR-70 — thingIDNumber → "A → B" carrier history.</summary>
        private Dictionary<int, string> heirloomLineage = new Dictionary<int, string>();
        private Dictionary<int, int> heirloomLastCarrier = new Dictionary<int, int>();
        /// <summary>AZR-70 — bed thingIDNumber → dead owner name.</summary>
        private Dictionary<int, string> unclaimedBeds = new Dictionary<int, string>();

        private List<int> recentColonistDeathTimestamps = new List<int>();
        private bool massacreTriggeredThisWindow;
        public string founderSurname;
        public List<RemembranceEntry> remembranceEntries = new List<RemembranceEntry>();
        public int lastRemembranceDayOfYear = -1;
        public List<FamilyLetterEntry> familyLetters = new List<FamilyLetterEntry>();
        public int lastFamilyLetterTick = -1;
        public HashSet<int> funeralProcessedCorpses = new HashSet<int>();
        public bool firstHarvestLetterSent;
        public bool autoPerksNewsLetterSent;
        public bool patch161NewsLetterSent;
        public int lastEnvoyVisitTick = -1;

        private const int DriftIntervalTicks = 2500;
        private const int MassacreWindowTicks = 60000;
        private const int MaxLedgerEntries = 240;
        private const int AggregateWindowTicks = 60000; // merge same reason within a day

        public GameComp_DeepColony(Game game) { }

        private static int MassacreDeathThreshold =>
            DeepColonySettings.Get.massacreDeathThreshold;

        public void EnsureFounderSurname()
        {
            if (!founderSurname.NullOrEmpty()) return;
            Map map = Find.CurrentMap ?? (Find.Maps.Count > 0 ? Find.Maps[0] : null);
            if (map == null) return;
            foreach (Pawn p in map.mapPawns.FreeColonists)
            {
                if (p.Name is NameTriple triple && !triple.Last.NullOrEmpty())
                {
                    founderSurname = triple.Last;
                    return;
                }
            }
        }

        public string GetFounderSurname()
        {
            EnsureFounderSurname();
            return founderSurname;
        }

        public bool HasProcessedInheritance(Pawn pawn) =>
            inheritanceProcessed.Contains(pawn.thingIDNumber);

        public void MarkInheritanceProcessed(Pawn pawn) =>
            inheritanceProcessed.Add(pawn.thingIDNumber);

        public bool WasEverPlayerColonist(Pawn pawn) =>
            pawn != null && formerPlayerColonists.Contains(pawn.thingIDNumber);

        public void MarkFormerPlayerColonist(Pawn pawn)
        {
            if (pawn != null) formerPlayerColonists.Add(pawn.thingIDNumber);
        }

        public void AddFactionDrift(Faction faction, float amount, FactionRepReason reason = FactionRepReason.Other)
        {
            if (!DeepColonySettings.Get.enableFactionRep) return;
            if (faction == null || faction.IsPlayer) return;
            if (System.Math.Abs(amount) < 0.0001f) return;

            if (!factionDriftBuffer.ContainsKey(faction.loadID))
                factionDriftBuffer[faction.loadID] = 0f;
            factionDriftBuffer[faction.loadID] += amount;
            RecordLedger(faction.loadID, amount, reason);
        }

        public float GetPendingDrift(Faction faction)
        {
            if (faction == null) return 0f;
            return factionDriftBuffer != null && factionDriftBuffer.TryGetValue(faction.loadID, out float v)
                ? v : 0f;
        }

        public IEnumerable<FactionRepLedgerEntry> GetLedger(Faction faction)
        {
            if (factionRepLedger == null || faction == null)
                yield break;
            for (int i = factionRepLedger.Count - 1; i >= 0; i--)
            {
                FactionRepLedgerEntry e = factionRepLedger[i];
                if (e.factionId == faction.loadID)
                    yield return e;
            }
        }

        public float SumLedger(Faction faction, FactionRepReason reason)
        {
            if (factionRepLedger == null || faction == null) return 0f;
            float sum = 0f;
            for (int i = 0; i < factionRepLedger.Count; i++)
            {
                FactionRepLedgerEntry e = factionRepLedger[i];
                if (e.factionId == faction.loadID && e.reason == reason)
                    sum += e.amount;
            }
            return sum;
        }

        private void RecordLedger(int factionId, float amount, FactionRepReason reason)
        {
            if (factionRepLedger == null)
                factionRepLedger = new List<FactionRepLedgerEntry>();

            int now = Find.TickManager?.TicksGame ?? 0;
            for (int i = factionRepLedger.Count - 1; i >= 0; i--)
            {
                FactionRepLedgerEntry e = factionRepLedger[i];
                if (e.factionId != factionId || e.reason != reason) continue;
                if (now - e.ticksGame > AggregateWindowTicks) continue;
                if ((e.amount >= 0f) != (amount >= 0f)) continue;
                e.amount += amount;
                e.count++;
                e.ticksGame = now;
                return;
            }

            factionRepLedger.Add(new FactionRepLedgerEntry
            {
                factionId = factionId,
                reason = reason,
                amount = amount,
                ticksGame = now,
                count = 1
            });

            while (factionRepLedger.Count > MaxLedgerEntries)
                factionRepLedger.RemoveAt(0);
        }

        public void NotifyColonistDied(Pawn victim)
        {
            if (!DeepColonySettings.Get.enableTrauma) return;
            if (victim == null || !victim.RaceProps.Humanlike) return;

            RemembranceUtility.NotifyColonistDied(victim);

            int now = Find.TickManager.TicksGame;
            recentColonistDeathTimestamps.Add(now);
            PruneDeathWindow(now);

            if (massacreTriggeredThisWindow) return;
            if (recentColonistDeathTimestamps.Count < MassacreDeathThreshold) return;

            massacreTriggeredThisWindow = true;
            TraumaDef massacre = DC_DefOf.DC_Trauma_Massacre;
            if (massacre == null) return;

            Map map = victim.MapHeld;
            if (map == null) return;

            foreach (Pawn colonist in map.mapPawns.FreeColonistsSpawned)
            {
                if (colonist.Dead || colonist == victim) continue;
                TraumaUtility.ApplyTrauma(colonist, massacre);
            }
        }

        private void PruneDeathWindow(int now)
        {
            recentColonistDeathTimestamps.RemoveAll(t => now - t > MassacreWindowTicks);
            // Allow another massacre once the dense cluster has aged out of the window
            // (not only when the list is completely empty — trickle deaths used to latch forever).
            if (recentColonistDeathTimestamps.Count < MassacreDeathThreshold)
                massacreTriggeredThisWindow = false;
        }

        public override void GameComponentTick()
        {
            if (DeepColonySettings.Get.enableTrauma)
            {
                FlashbackUtility.GameTick();
                RemembranceUtility.GameTick();
                FamilyLetterUtility.GameTick();
                AnomalyOdysseyTraumaUtility.GameTick();
            }

            // One clock: TicksGame. A saved driftTickCounter started at 0 on mid-save
            // install and never lined up with TicksGame % 2500, so rivalry / family /
            // touch / reconcile never ran. Flashback and letters still self-throttle.
            if (Find.TickManager.TicksGame % DriftIntervalTicks != 0)
                return;

            if (DeepColonySettings.Get.enableTrauma)
            {
                TickTraumaSystems();
                TickToxicBuildupTrauma();
                PruneDeathWindow(Find.TickManager.TicksGame);
            }

            if (DeepColonySettings.Get.enableFactionRep)
            {
                ProcessFactionDrift();
                FactionEnvoyUtility.GameTick();
                EnvoyVisitUtility.GameTick();
            }

            RivalryUtility.GameTick();
            TickElders();
            if (DeepColonySettings.Get.enableHeirlooms)
                TickHeirlooms();
            FamilyJoinUtility.GameTick();
            ExLoverReconcileUtility.GameTick();
            TouchAverseUtility.GameTick();
            FamilyLifeUtility.GameTick();
            FamilyEchoUtility.GameTick();
        }

        public void RegisterHeirloom(int thingId, string ownerName, string echoPerkDefName)
        {
            if (heirloomOwners == null) heirloomOwners = new Dictionary<int, string>();
            if (heirloomEchoPerks == null) heirloomEchoPerks = new Dictionary<int, string>();
            if (heirloomLineage == null) heirloomLineage = new Dictionary<int, string>();
            heirloomOwners[thingId] = ownerName ?? "";
            if (!echoPerkDefName.NullOrEmpty())
                heirloomEchoPerks[thingId] = echoPerkDefName;
            if (!ownerName.NullOrEmpty() && !heirloomLineage.ContainsKey(thingId))
                heirloomLineage[thingId] = ownerName;
        }

        public bool IsHeirloom(int thingId) =>
            heirloomOwners != null && heirloomOwners.ContainsKey(thingId);

        public string GetHeirloomLineage(int thingId)
        {
            if (heirloomLineage != null && heirloomLineage.TryGetValue(thingId, out string line))
                return line;
            if (heirloomOwners != null && heirloomOwners.TryGetValue(thingId, out string owner))
                return owner;
            return null;
        }

        public void NoteHeirloomCarrier(int thingId, Pawn carrier)
        {
            if (carrier == null || !IsHeirloom(thingId)) return;
            if (heirloomLastCarrier == null) heirloomLastCarrier = new Dictionary<int, int>();
            if (heirloomLineage == null) heirloomLineage = new Dictionary<int, string>();
            string name = carrier.Name?.ToStringShort ?? carrier.LabelShort;
            if (heirloomLastCarrier.TryGetValue(thingId, out int lastId) && lastId == carrier.thingIDNumber)
                return;
            heirloomLastCarrier[thingId] = carrier.thingIDNumber;
            if (heirloomLineage.TryGetValue(thingId, out string line) && !line.NullOrEmpty())
            {
                if (!line.EndsWith(name))
                    heirloomLineage[thingId] = line + " → " + name;
            }
            else if (heirloomOwners != null && heirloomOwners.TryGetValue(thingId, out string owner)
                && !owner.NullOrEmpty() && owner != name)
            {
                heirloomLineage[thingId] = owner + " → " + name;
            }
            else
            {
                heirloomLineage[thingId] = name;
            }
        }

        public string FormatHeirloomChronicle()
        {
            if (heirloomLineage == null || heirloomLineage.Count == 0) return null;
            var sb = new System.Text.StringBuilder();
            foreach (var kv in heirloomLineage)
                sb.Append("- ").AppendLine(kv.Value);
            return sb.ToString();
        }

        public void NoteUnclaimedBed(int bedId, string ownerName)
        {
            if (unclaimedBeds == null) unclaimedBeds = new Dictionary<int, string>();
            if (bedId < 0 || ownerName.NullOrEmpty()) return;
            unclaimedBeds[bedId] = ownerName;
        }

        public bool TryClaimUnclaimedBed(int bedId, out string formerOwner)
        {
            formerOwner = null;
            if (unclaimedBeds == null) return false;
            if (!unclaimedBeds.TryGetValue(bedId, out formerOwner)) return false;
            unclaimedBeds.Remove(bedId);
            return !formerOwner.NullOrEmpty();
        }

        private static void TickHeirlooms()
        {
            foreach (Map map in Find.Maps)
            {
                foreach (Pawn p in map.mapPawns.FreeColonistsSpawned)
                    HeirloomUtility.TickCarrier(p);
            }
        }

        private static void TickTraumaSystems()
        {
            foreach (Map map in Find.Maps)
            {
                foreach (Pawn p in map.mapPawns.FreeColonistsSpawned)
                {
                    TraumaRecoveryUtility.TickNaturalRecovery(p);
                    TraumaCombatUtility.TickPawn(p);
                }
            }
        }

        private static void TickToxicBuildupTrauma()
        {
            foreach (Map map in Find.Maps)
            {
                List<Pawn> colonists = map.mapPawns?.FreeColonistsSpawned;
                if (colonists == null) continue;
                for (int i = 0; i < colonists.Count; i++)
                    TraumaEventUtility.TryToxicBuildupTrauma(colonists[i]);
            }
        }

        private static void TickElders()
        {
            foreach (Map map in Find.Maps)
            {
                foreach (Pawn p in map.mapPawns.FreeColonistsSpawned)
                    ElderUtility.TickPawn(p);
            }
        }

        private void ProcessFactionDrift()
        {
            var keys = new List<int>(factionDriftBuffer.Keys);
            foreach (int id in keys)
            {
                float amount = factionDriftBuffer[id];
                Faction faction = Find.FactionManager.AllFactionsListForReading
                    .Find(f => f.loadID == id);
                if (faction == null)
                {
                    factionDriftBuffer.Remove(id);
                    continue;
                }

                int whole = (int)amount; // truncates toward zero; keep fractional remainder
                if (whole != 0)
                {
                    faction.TryAffectGoodwillWith(Faction.OfPlayer, whole, canSendMessage: false);
                    amount -= whole;
                }

                if (System.Math.Abs(amount) < 0.01f)
                    factionDriftBuffer.Remove(id);
                else
                    factionDriftBuffer[id] = amount;
            }

            var settings = DeepColonySettings.Get;
            foreach (Faction faction in Find.FactionManager.AllFactionsListForReading)
            {
                if (faction.IsPlayer || faction.defeated) continue;

                int goodwill = faction.GoodwillWith(Faction.OfPlayer);
                if (goodwill > 40)
                {
                    if (Rand.MTBEventOccurs(settings.allyDriftMtbDays, 60000f, DriftIntervalTicks))
                        AddFactionDrift(faction, -1f, FactionRepReason.IdleAlly);
                }
                else if (goodwill < -40)
                {
                    if (Rand.MTBEventOccurs(settings.enemyDriftMtbDays, 60000f, DriftIntervalTicks))
                        AddFactionDrift(faction, 1f, FactionRepReason.IdleEnemy);
                }
            }
        }

        public override void ExposeData()
        {
            base.ExposeData();
            Scribe_Collections.Look(ref inheritanceProcessed, "inheritanceProcessed", LookMode.Value);
            Scribe_Collections.Look(ref formerPlayerColonists, "formerPlayerColonists", LookMode.Value);
            Scribe_Collections.Look(ref factionDriftBuffer, "factionDriftBuffer",
                LookMode.Value, LookMode.Value);
            Scribe_Collections.Look(ref factionRepLedger, "factionRepLedger", LookMode.Deep);
            Scribe_Collections.Look(ref heirloomOwners, "heirloomOwners", LookMode.Value, LookMode.Value);
            Scribe_Collections.Look(ref heirloomEchoPerks, "heirloomEchoPerks", LookMode.Value, LookMode.Value);
            Scribe_Collections.Look(ref heirloomLineage, "heirloomLineage", LookMode.Value, LookMode.Value);
            Scribe_Collections.Look(ref heirloomLastCarrier, "heirloomLastCarrier", LookMode.Value, LookMode.Value);
            Scribe_Collections.Look(ref unclaimedBeds, "unclaimedBeds", LookMode.Value, LookMode.Value);
            Scribe_Collections.Look(ref recentColonistDeathTimestamps, "recentColonistDeaths",
                LookMode.Value);
            Scribe_Values.Look(ref massacreTriggeredThisWindow, "massacreTriggered", false);
            Scribe_Values.Look(ref founderSurname, "founderSurname");
            Scribe_Collections.Look(ref remembranceEntries, "remembranceEntries", LookMode.Deep);
            Scribe_Values.Look(ref lastRemembranceDayOfYear, "lastRemembranceDayOfYear", -1);
            Scribe_Collections.Look(ref familyLetters, "familyLetters", LookMode.Deep);
            Scribe_Values.Look(ref lastFamilyLetterTick, "lastFamilyLetterTick", -1);
            Scribe_Collections.Look(ref funeralProcessedCorpses, "funeralProcessedCorpses", LookMode.Value);
            Scribe_Values.Look(ref lastEnvoyVisitTick, "lastEnvoyVisitTick", -1);
            Scribe_Values.Look(ref firstHarvestLetterSent, "firstHarvestLetterSent", false);
            Scribe_Values.Look(ref autoPerksNewsLetterSent, "autoPerksNewsLetterSent", false);
            Scribe_Values.Look(ref patch161NewsLetterSent, "patch161NewsLetterSent", false);

            if (inheritanceProcessed == null) inheritanceProcessed = new HashSet<int>();
            if (formerPlayerColonists == null) formerPlayerColonists = new HashSet<int>();
            if (factionDriftBuffer == null) factionDriftBuffer = new Dictionary<int, float>();
            if (factionRepLedger == null) factionRepLedger = new List<FactionRepLedgerEntry>();
            if (heirloomOwners == null) heirloomOwners = new Dictionary<int, string>();
            if (heirloomEchoPerks == null) heirloomEchoPerks = new Dictionary<int, string>();
            if (heirloomLineage == null) heirloomLineage = new Dictionary<int, string>();
            if (heirloomLastCarrier == null) heirloomLastCarrier = new Dictionary<int, int>();
            if (unclaimedBeds == null) unclaimedBeds = new Dictionary<int, string>();
            if (recentColonistDeathTimestamps == null)
                recentColonistDeathTimestamps = new List<int>();
            if (remembranceEntries == null)
                remembranceEntries = new List<RemembranceEntry>();
            if (familyLetters == null)
                familyLetters = new List<FamilyLetterEntry>();
            if (funeralProcessedCorpses == null)
                funeralProcessedCorpses = new HashSet<int>();
        }

        public override void FinalizeInit()
        {
            if (inheritanceProcessed == null) inheritanceProcessed = new HashSet<int>();
            if (formerPlayerColonists == null) formerPlayerColonists = new HashSet<int>();
            if (factionDriftBuffer == null) factionDriftBuffer = new Dictionary<int, float>();
            if (factionRepLedger == null) factionRepLedger = new List<FactionRepLedgerEntry>();
            if (heirloomOwners == null) heirloomOwners = new Dictionary<int, string>();
            if (heirloomEchoPerks == null) heirloomEchoPerks = new Dictionary<int, string>();
            if (heirloomLineage == null) heirloomLineage = new Dictionary<int, string>();
            if (heirloomLastCarrier == null) heirloomLastCarrier = new Dictionary<int, int>();
            if (unclaimedBeds == null) unclaimedBeds = new Dictionary<int, string>();
            if (recentColonistDeathTimestamps == null)
                recentColonistDeathTimestamps = new List<int>();
            if (remembranceEntries == null)
                remembranceEntries = new List<RemembranceEntry>();
            if (familyLetters == null)
                familyLetters = new List<FamilyLetterEntry>();
            if (funeralProcessedCorpses == null)
                funeralProcessedCorpses = new HashSet<int>();
            ActiveMentoringSession.ResetSession();
            EnsureFounderSurname();
            TrySendPatch161NewsLetter();
        }

        private void TrySendPatch161NewsLetter()
        {
            if (patch161NewsLetterSent) return;
            if (Find.LetterStack == null) return;

            // Mark first so a letter exception cannot spam every load.
            patch161NewsLetterSent = true;

            Find.LetterStack.ReceiveLetter(
                "DC_Patch161LetterLabel".Translate(),
                "DC_Patch161LetterText".Translate(),
                LetterDefOf.PositiveEvent);
        }
    }
}
