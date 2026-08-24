using System.Collections.Generic;
using System.Text;
using RimWorld;
using Verse;

namespace DeepColony
{
    public enum TouchComfortTier : byte
    {
        Distant = 0,
        Familiar = 1,
        AtEase = 2,
        FineWithTouch = 3,
        Intimate = 4
    }

    /// <summary>
    /// F01 / F02 — touch-need traits share a per-person comfort meter with named
    /// tiers. Touch-averse degrees gate romance on a required tier. Touch-starved
    /// is the inverse (needs trusted contact). Date Night still owns schedules.
    /// </summary>
    public static class TouchAverseUtility
    {
        public const string AverseDefName = "DC_TouchAverse";
        public const string StarvedDefName = "DC_TouchStarved";
        public const string TactileDefName = "DC_Tactile";
        public const string CuddlyDefName = "DC_Cuddly";
        public const string TraitDefName = AverseDefName;

        public const float ComfortMax = 1f;
        public const int TouchChebyshev = 1;
        public const int StarvedLonelyTicks = 60000;
        private const int CheckInterval = 2500;
        private const float OpinionGainFloor = 0f;

        public static TraitDef Trait => AverseTrait;
        public static TraitDef AverseTrait =>
            DefDatabase<TraitDef>.GetNamedSilentFail(AverseDefName);
        public static TraitDef StarvedTrait =>
            DefDatabase<TraitDef>.GetNamedSilentFail(StarvedDefName);
        public static TraitDef TactileTrait =>
            DefDatabase<TraitDef>.GetNamedSilentFail(TactileDefName);
        public static TraitDef CuddlyTrait =>
            DefDatabase<TraitDef>.GetNamedSilentFail(CuddlyDefName);

        public static bool Enabled => DeepColonySettings.Get.enableTouchAverse;

        public static float RomanceThreshold =>
            DeepColonySettings.Get.touchComfortThreshold;

        public static bool HasTrait(Pawn pawn) => HasAverse(pawn);

        public static bool HasAverse(Pawn pawn) => HasDef(pawn, AverseTrait);
        public static bool HasStarved(Pawn pawn) => HasDef(pawn, StarvedTrait);
        public static bool HasTactile(Pawn pawn) => HasDef(pawn, TactileTrait);
        public static bool HasCuddly(Pawn pawn) => HasDef(pawn, CuddlyTrait);

        public static bool HasTouchNeed(Pawn pawn) =>
            HasAverse(pawn) || HasStarved(pawn) || HasTactile(pawn) || HasCuddly(pawn);

        public static bool NeedsRomanceGate(Pawn pawn) =>
            HasAverse(pawn) || HasStarved(pawn);

        public static int AverseDegree(Pawn pawn)
        {
            if (pawn?.story?.traits == null || AverseTrait == null) return 0;
            Trait trait = pawn.story.traits.GetTrait(AverseTrait);
            return trait == null ? 0 : trait.Degree;
        }

        public static bool IsLoveRelation(PawnRelationDef def)
        {
            if (def == null) return false;
            if (def == PawnRelationDefOf.Lover) return true;
            if (PawnRelationDefOf.Fiance != null && def == PawnRelationDefOf.Fiance)
                return true;
            if (def == PawnRelationDefOf.Spouse) return true;
            return false;
        }

        public static bool AlreadyLovePartners(Pawn a, Pawn b)
        {
            if (a?.relations == null || b == null) return false;
            return LovePartnerRelationUtility.LovePartnerRelationExists(a, b);
        }

        public static bool IsInTouchRange(Pawn a, Pawn b)
        {
            if (a == null || b == null) return false;
            if (!a.Spawned || !b.Spawned) return false;
            if (a.Map != b.Map) return false;
            return Chebyshev(a.Position, b.Position) <= TouchChebyshev;
        }

        public static int Chebyshev(IntVec3 a, IntVec3 b)
        {
            int dx = a.x > b.x ? a.x - b.x : b.x - a.x;
            int dz = a.z > b.z ? a.z - b.z : b.z - a.z;
            return dx > dz ? dx : dz;
        }

        public static float TierMin(TouchComfortTier tier)
        {
            float t = RomanceThreshold;
            if (t < 0.20f) t = 0.20f;
            if (t > 0.90f) t = 0.90f;
            switch (tier)
            {
                case TouchComfortTier.Distant: return 0f;
                case TouchComfortTier.Familiar: return t * 0.38f;
                case TouchComfortTier.AtEase: return t * 0.69f;
                case TouchComfortTier.FineWithTouch: return t;
                default: return t + (1f - t) * 0.57f;
            }
        }

        public static TouchComfortTier TierOf(float comfort)
        {
            if (comfort >= TierMin(TouchComfortTier.Intimate)) return TouchComfortTier.Intimate;
            if (comfort >= TierMin(TouchComfortTier.FineWithTouch)) return TouchComfortTier.FineWithTouch;
            if (comfort >= TierMin(TouchComfortTier.AtEase)) return TouchComfortTier.AtEase;
            if (comfort >= TierMin(TouchComfortTier.Familiar)) return TouchComfortTier.Familiar;
            return TouchComfortTier.Distant;
        }

        public static TouchComfortTier RequiredRomanceTier(Pawn pawn)
        {
            if (HasStarved(pawn)) return TouchComfortTier.AtEase;
            if (!HasAverse(pawn)) return TouchComfortTier.Distant;
            int degree = AverseDegree(pawn);
            if (degree <= -1) return TouchComfortTier.AtEase;
            if (degree >= 1) return TouchComfortTier.Intimate;
            return TouchComfortTier.FineWithTouch;
        }

        public static string TierLabel(TouchComfortTier tier)
        {
            switch (tier)
            {
                case TouchComfortTier.Familiar: return "DC_TouchTier_Familiar".Translate();
                case TouchComfortTier.AtEase: return "DC_TouchTier_AtEase".Translate();
                case TouchComfortTier.FineWithTouch: return "DC_TouchTier_FineWithTouch".Translate();
                case TouchComfortTier.Intimate: return "DC_TouchTier_Intimate".Translate();
                default: return "DC_TouchTier_Distant".Translate();
            }
        }

        public static bool MeetsRomanceTier(Pawn self, Pawn other)
        {
            if (self == null || other == null || self == other) return false;
            if (!Enabled || !NeedsRomanceGate(self)) return true;
            if (AlreadyLovePartners(self, other)) return true;
            EnsureSeeded(self, other);
            return TierOf(GetComfort(self, other)) >= RequiredRomanceTier(self);
        }

        /// <summary>
        /// Fine with this person's physical closeness (mood, beds). Romance uses
        /// <see cref="MeetsRomanceTier"/>, which is the same bar for averse degrees.
        /// </summary>
        public static bool IsFineBeingTouchedBy(Pawn averse, Pawn other)
        {
            return MeetsRomanceTier(averse, other);
        }

        public static bool CanFormLoveRelation(Pawn a, Pawn b)
        {
            if (a == null || b == null || a == b) return false;
            if (!Enabled) return true;
            if (Current.ProgramState != ProgramState.Playing) return true;
            if (AlreadyLovePartners(a, b)) return true;
            if (!MeetsRomanceTier(a, b)) return false;
            if (!MeetsRomanceTier(b, a)) return false;
            return true;
        }

        public static bool CanAttemptRomance(Pawn initiator, Pawn recipient)
        {
            if (initiator == null || recipient == null) return false;
            if (!Enabled) return true;
            if (!NeedsRomanceGate(initiator) && !NeedsRomanceGate(recipient)) return true;
            if (AlreadyLovePartners(initiator, recipient)) return true;
            if (!CanFormLoveRelation(initiator, recipient)) return false;
            return IsInTouchRange(initiator, recipient);
        }

        public static bool RefusesToShareBed(Pawn pawn, Pawn other)
        {
            if (!Enabled || pawn == null || other == null || pawn == other)
                return false;
            if (HasAverse(pawn) && !MeetsRomanceTier(pawn, other)) return true;
            if (HasAverse(other) && !MeetsRomanceTier(other, pawn)) return true;
            return false;
        }

        public static bool IsTrustedForContact(Pawn self, Pawn other)
        {
            if (self == null || other == null || self == other) return false;
            if (AlreadyLovePartners(self, other)) return true;
            EnsureSeeded(self, other);
            return TierOf(GetComfort(self, other)) >= TouchComfortTier.AtEase;
        }

        public static float GetComfort(Pawn self, Pawn other)
        {
            if (self == null || other == null) return 0f;
            var comp = self.TryGetComp<Comp_DeepColony>();
            if (comp?.touchComfortByPawn == null) return 0f;
            return comp.touchComfortByPawn.TryGetValue(other.thingIDNumber, out float v)
                ? v
                : 0f;
        }

        public static void SetComfort(Pawn self, Pawn other, float value)
        {
            if (self == null || other == null || self == other) return;
            var comp = self.TryGetComp<Comp_DeepColony>();
            if (comp == null) return;
            if (comp.touchComfortByPawn == null)
                comp.touchComfortByPawn = new Dictionary<int, float>();
            float clamped = value < 0f ? 0f : (value > ComfortMax ? ComfortMax : value);
            comp.touchComfortByPawn[other.thingIDNumber] = clamped;
        }

        public static void NotifyLoveRelationFormed(Pawn a, Pawn b)
        {
            if (a == null || b == null) return;
            if (HasTouchNeed(a)) SetComfort(a, b, ComfortMax);
            if (HasTouchNeed(b)) SetComfort(b, a, ComfortMax);
        }

        public static void NotifyTrustedBond(Pawn a, Pawn b)
        {
            if (!Enabled || a == null || b == null) return;
            BoostTowardThreshold(a, b, 0.25f);
            BoostTowardThreshold(b, a, 0.25f);
        }

        public static void GameTick()
        {
            if (!Enabled) return;
            if (Find.TickManager == null) return;
            if (Find.TickManager.TicksGame % CheckInterval != 0) return;

            foreach (Map map in Find.Maps)
            {
                if (map?.mapPawns?.AllPawnsSpawned == null) continue;
                IReadOnlyList<Pawn> spawned = map.mapPawns.AllPawnsSpawned;
                for (int i = 0; i < spawned.Count; i++)
                {
                    Pawn pawn = spawned[i];
                    if (!IsHumanlikeAlive(pawn)) continue;
                    if (!HasTouchNeed(pawn)) continue;
                    TickNeedPawn(pawn, spawned);
                }
            }
        }

        public static string InspectString(Pawn pawn)
        {
            if (!Enabled || !HasTouchNeed(pawn)) return null;
            List<string> names = ComfortableNames(pawn);
            string list = names.Count == 0
                ? "DC_InspectTouchNeedNone".Translate()
                : string.Join(", ", names);
            string head = HasStarved(pawn)
                ? "DC_InspectTouchStarved".Translate(list)
                : HasTactile(pawn)
                    ? "DC_InspectTactile".Translate(list)
                    : HasCuddly(pawn)
                        ? "DC_InspectCuddly".Translate(list)
                        : AverseDegree(pawn) <= -1
                            ? "DC_InspectReserved".Translate(list)
                            : AverseDegree(pawn) >= 1
                                ? "DC_InspectTouchIntolerant".Translate(list)
                                : "DC_InspectTouchAverse".Translate(list);
            if (!HasStarved(pawn)) return head;
            string extra = StarvedContactLine(pawn);
            if (extra.NullOrEmpty()) return head;
            return head + "\n" + extra;
        }

        public static string DebugDump(Pawn pawn)
        {
            var sb = new StringBuilder();
            sb.AppendLine("[DeepColony] Touch-need dump for "
                + (pawn?.LabelShort ?? "(null)"));
            sb.AppendLine("  enabled=" + Enabled
                + " averse=" + HasAverse(pawn)
                + " degree=" + (HasAverse(pawn) ? AverseDegree(pawn).ToString() : "-")
                + " starved=" + HasStarved(pawn)
                + " tactile=" + HasTactile(pawn)
                + " cuddly=" + HasCuddly(pawn)
                + " needTier=" + RequiredRomanceTier(pawn)
                + " threshold=" + RomanceThreshold.ToString("F2")
                + " days=" + DeepColonySettings.Get.touchComfortDays.ToString("F1"));
            if (pawn == null) return sb.ToString();
            var comp = pawn.TryGetComp<Comp_DeepColony>();
            if (comp != null)
            {
                sb.AppendLine("  lastTrustedTouchTick=" + comp.lastTrustedTouchTick);
            }
            if (comp?.touchComfortByPawn == null || comp.touchComfortByPawn.Count == 0)
            {
                sb.AppendLine("  (no comfort entries)");
                return sb.ToString();
            }
            foreach (var kv in comp.touchComfortByPawn)
            {
                Pawn other = FindPawn(kv.Key);
                string name = other?.LabelShort ?? ("#" + kv.Key);
                TouchComfortTier tier = TierOf(kv.Value);
                bool romance = other != null && MeetsRomanceTier(pawn, other);
                bool touch = other != null && IsInTouchRange(pawn, other);
                sb.AppendLine("  " + name
                    + " comfort=" + kv.Value.ToString("F2")
                    + " tier=" + tier
                    + " romance=" + romance
                    + " adjacent=" + touch);
            }
            return sb.ToString();
        }

        public static int MaxComfortNearby(Pawn pawn)
        {
            if (pawn?.Map == null) return 0;
            int n = 0;
            foreach (Pawn other in pawn.Map.mapPawns.AllPawnsSpawned)
            {
                if (!IsHumanlikeAlive(other) || other == pawn) continue;
                SetComfort(pawn, other, ComfortMax);
                n++;
            }
            StampTrustedContact(pawn);
            return n;
        }

        public static void RemoveTouchNeedTraits(Pawn pawn)
        {
            if (pawn?.story?.traits == null) return;
            TryRemove(pawn, AverseTrait);
            TryRemove(pawn, StarvedTrait);
            TryRemove(pawn, TactileTrait);
            TryRemove(pawn, CuddlyTrait);
        }

        internal static float SameRoomGainPerInterval()
        {
            float days = DeepColonySettings.Get.touchComfortDays;
            if (days < 0.5f) days = 0.5f;
            float intervals = days * (60000f / CheckInterval);
            float threshold = RomanceThreshold;
            if (threshold < 0.05f) threshold = 0.05f;
            return threshold / intervals;
        }

        internal static float NextComfort(
            float current,
            bool adjacent,
            bool sameRoom,
            float opinion,
            float speedMul)
        {
            if (speedMul < 0.1f) speedMul = 0.1f;
            float next = current;
            if (adjacent || sameRoom)
            {
                if (opinion >= OpinionGainFloor)
                {
                    float opinionMul = OpinionMultiplier(opinion);
                    float baseGain = SameRoomGainPerInterval();
                    if (adjacent) baseGain *= 2.5f;
                    next += baseGain * opinionMul * speedMul;
                }
                else if (adjacent)
                {
                    next -= SameRoomGainPerInterval() * 0.35f * speedMul;
                }
            }
            else
            {
                next -= SameRoomGainPerInterval() * 0.05f;
            }
            if (next < 0f) return 0f;
            if (next > ComfortMax) return ComfortMax;
            return next;
        }

        internal static float TraitSpeed(Pawn pawn)
        {
            if (HasStarved(pawn)) return 2.0f;
            if (HasCuddly(pawn)) return 1.75f;
            if (HasTactile(pawn)) return 1.5f;
            if (!HasAverse(pawn)) return 1f;
            int degree = AverseDegree(pawn);
            if (degree <= -1) return 1.2f;
            if (degree >= 1) return 0.6f;
            return 1f;
        }

        private static void TickNeedPawn(Pawn pawn, IReadOnlyList<Pawn> spawned)
        {
            bool trustedContact = false;
            for (int i = 0; i < spawned.Count; i++)
            {
                Pawn other = spawned[i];
                if (!IsHumanlikeAlive(other) || other == pawn) continue;
                EnsureSeeded(pawn, other);
                if (AlreadyLovePartners(pawn, other))
                {
                    SetComfort(pawn, other, ComfortMax);
                    if (IsInTouchRange(pawn, other)) trustedContact = true;
                    continue;
                }

                float before = GetComfort(pawn, other);
                bool adjacent = IsInTouchRange(pawn, other);
                bool sameRoom = !adjacent && SameIndoorRoom(pawn, other);
                float opinion = pawn.relations != null
                    ? pawn.relations.OpinionOf(other)
                    : 0f;
                float speed = BondSpeed(pawn, other) * TraitSpeed(pawn);
                float next = NextComfort(before, adjacent, sameRoom, opinion, speed);
                SetComfort(pawn, other, next);
                MaybeAnnounceTier(pawn, other, before, next);
                if (adjacent && IsTrustedForContact(pawn, other))
                    trustedContact = true;
            }

            if (HasStarved(pawn))
            {
                if (trustedContact) StampTrustedContact(pawn);
                else EnsureStarvedGrace(pawn);
            }
        }

        private static void MaybeAnnounceTier(
            Pawn self, Pawn other, float before, float after)
        {
            if (!NeedsRomanceGate(self)) return;
            TouchComfortTier need = RequiredRomanceTier(self);
            if (TierOf(before) >= need || TierOf(after) < need) return;
            Messages.Message(
                "DC_TouchAverseNowFine".Translate(
                    self.LabelShort.Named("PAWN"),
                    other.LabelShort.Named("OTHER"),
                    TierLabel(need).Named("TIER")),
                new LookTargets(self, other),
                MessageTypeDefOf.PositiveEvent,
                false);
        }

        private static void BoostTowardThreshold(Pawn self, Pawn other, float amount)
        {
            if (!HasTouchNeed(self)) return;
            EnsureSeeded(self, other);
            float before = GetComfort(self, other);
            SetComfort(self, other, before + amount);
            MaybeAnnounceTier(self, other, before, GetComfort(self, other));
        }

        private static void EnsureSeeded(Pawn self, Pawn other)
        {
            var comp = self.TryGetComp<Comp_DeepColony>();
            if (comp == null) return;
            if (comp.touchComfortByPawn == null)
                comp.touchComfortByPawn = new Dictionary<int, float>();
            if (comp.touchComfortByPawn.ContainsKey(other.thingIDNumber))
                return;

            float seed = 0f;
            if (AlreadyLovePartners(self, other) || IsExLovePartner(self, other))
                seed = ComfortMax;
            else if (MentorshipUtility.IsLineagePair(self, other))
                seed = TierMin(TouchComfortTier.Intimate);
            comp.touchComfortByPawn[other.thingIDNumber] = seed;
        }

        private static bool IsExLovePartner(Pawn a, Pawn b)
        {
            if (a?.relations == null || b == null) return false;
            if (a.relations.DirectRelationExists(PawnRelationDefOf.ExLover, b))
                return true;
            if (PawnRelationDefOf.ExSpouse != null
                && a.relations.DirectRelationExists(PawnRelationDefOf.ExSpouse, b))
                return true;
            return false;
        }

        private static bool SameIndoorRoom(Pawn a, Pawn b)
        {
            if (a.Map != b.Map) return false;
            Room room = a.GetRoom();
            if (room == null || room.PsychologicallyOutdoors) return false;
            return b.GetRoom() == room;
        }

        private static float BondSpeed(Pawn self, Pawn other)
        {
            float mul = 1f;
            if (ConfidantUtility.AreConfidants(self, other)) mul *= 1.5f;
            var comp = self.TryGetComp<Comp_DeepColony>();
            if (comp != null && (comp.mentor == other
                || (other.TryGetComp<Comp_DeepColony>() is Comp_DeepColony oc
                    && oc.mentor == self)))
                mul *= 1.35f;
            return mul;
        }

        private static float OpinionMultiplier(float opinion)
        {
            float m = 0.7f + opinion * 0.009f;
            if (m < 0.4f) return 0.4f;
            if (m > 1.6f) return 1.6f;
            return m;
        }

        private static bool IsHumanlikeAlive(Pawn pawn)
        {
            if (pawn == null || pawn.Dead) return false;
            if (pawn.RaceProps == null || !pawn.RaceProps.Humanlike) return false;
            return pawn.Spawned;
        }

        private static bool HasDef(Pawn pawn, TraitDef def)
        {
            return def != null
                && pawn?.story?.traits != null
                && pawn.story.traits.HasTrait(def);
        }

        private static void TryRemove(Pawn pawn, TraitDef def)
        {
            if (def == null || pawn.story?.traits == null) return;
            Trait existing = pawn.story.traits.GetTrait(def);
            if (existing != null) pawn.story.traits.RemoveTrait(existing);
        }

        private static void StampTrustedContact(Pawn pawn)
        {
            var comp = pawn.TryGetComp<Comp_DeepColony>();
            if (comp == null || Find.TickManager == null) return;
            comp.lastTrustedTouchTick = Find.TickManager.TicksGame;
        }

        private static void EnsureStarvedGrace(Pawn pawn)
        {
            var comp = pawn.TryGetComp<Comp_DeepColony>();
            if (comp == null || Find.TickManager == null) return;
            if (comp.lastTrustedTouchTick >= 0) return;
            comp.lastTrustedTouchTick = Find.TickManager.TicksGame;
        }

        public static bool StarvedIsLonely(Pawn pawn)
        {
            if (!HasStarved(pawn) || pawn == null || !pawn.Spawned) return false;
            var comp = pawn.TryGetComp<Comp_DeepColony>();
            if (comp == null || Find.TickManager == null) return false;
            if (comp.lastTrustedTouchTick < 0) return false;
            return Find.TickManager.TicksGame - comp.lastTrustedTouchTick >= StarvedLonelyTicks;
        }

        private static string StarvedContactLine(Pawn pawn)
        {
            var comp = pawn.TryGetComp<Comp_DeepColony>();
            if (comp == null || Find.TickManager == null) return null;
            if (comp.lastTrustedTouchTick < 0)
                return "DC_InspectTouchStarvedGrace".Translate();
            int ago = Find.TickManager.TicksGame - comp.lastTrustedTouchTick;
            if (ago < CheckInterval * 2)
                return "DC_InspectTouchStarvedNow".Translate();
            int hours = ago / 2500;
            if (hours < 1) hours = 1;
            return "DC_InspectTouchStarvedLast".Translate(hours);
        }

        private static List<string> ComfortableNames(Pawn pawn)
        {
            var names = new List<string>();
            var seen = new HashSet<int>();
            TouchComfortTier min = NeedsRomanceGate(pawn)
                ? RequiredRomanceTier(pawn)
                : TouchComfortTier.AtEase;

            if (pawn.relations != null)
            {
                List<DirectPawnRelation> partners =
                    LovePartnerRelationUtility.ExistingLovePartners(pawn, allowDead: false);
                if (partners != null)
                {
                    for (int i = 0; i < partners.Count; i++)
                    {
                        Pawn other = partners[i].otherPawn;
                        if (other == null || other.Dead) continue;
                        if (!seen.Add(other.thingIDNumber)) continue;
                        names.Add(other.LabelShort + " (" + TierLabel(TouchComfortTier.Intimate) + ")");
                    }
                }
            }

            var comp = pawn.TryGetComp<Comp_DeepColony>();
            if (comp?.touchComfortByPawn == null) return names;
            foreach (var kv in comp.touchComfortByPawn)
            {
                TouchComfortTier tier = TierOf(kv.Value);
                if (tier < min) continue;
                if (!seen.Add(kv.Key)) continue;
                Pawn other = FindPawn(kv.Key);
                if (other == null || other.Dead) continue;
                names.Add(other.LabelShort + " (" + TierLabel(tier) + ")");
                if (names.Count >= 4) break;
            }
            return names;
        }

        private static Pawn FindPawn(int id)
        {
            if (Find.Maps != null)
            {
                foreach (Map map in Find.Maps)
                {
                    if (map?.mapPawns?.AllPawnsSpawned == null) continue;
                    foreach (Pawn x in map.mapPawns.AllPawnsSpawned)
                    {
                        if (x != null && x.thingIDNumber == id) return x;
                    }
                }
            }
            if (Find.WorldPawns == null) return null;
            foreach (Pawn p in Find.WorldPawns.AllPawnsAlive)
            {
                if (p != null && p.thingIDNumber == id) return p;
            }
            return null;
        }
    }
}
