using System.Collections.Generic;
using RimWorld;
using Verse;

namespace DateNight
{
    public enum DateNightRomanceStage
    {
        None = 0,
        Friends = 1,
        Sweethearts = 2,
        Partners = 3
    }

    public class DateNightRomanceBond : IExposable
    {
        public int pawnA;
        public int pawnB;
        public DateNightRomanceStage stage = DateNightRomanceStage.Friends;
        public int progress;
        public int lastAdvanceTick;
        public int lastCoolTick;

        public void ExposeData()
        {
            Scribe_Values.Look(ref pawnA, "pawnA");
            Scribe_Values.Look(ref pawnB, "pawnB");
            Scribe_Values.Look(ref stage, "stage", DateNightRomanceStage.Friends);
            Scribe_Values.Look(ref progress, "progress");
            Scribe_Values.Look(ref lastAdvanceTick, "lastAdvanceTick");
            Scribe_Values.Look(ref lastCoolTick, "lastCoolTick");
        }

        public bool Matches(int a, int b)
        {
            return (pawnA == a && pawnB == b) || (pawnA == b && pawnB == a);
        }
    }

    /// <summary>
    /// Friends → sweethearts → boyfriend/girlfriend, then a vanilla Lover bond.
    /// Existing lovers/spouses skip the track. Lovin stays locked until Lover/Spouse.
    /// </summary>
    public static class DateNightRomance
    {
        public const int DatesToAdvance = 2;
        public const int OpinionFriends = 15;
        public const int OpinionSweethearts = 25;
        public const int OpinionPartners = 40;
        public const int OpinionLover = 50;
        public const int StepDownOpinion = 0;
        public const int StepDownCooldownTicks = 60000;

        private static List<DateNightRomanceBond> bonds = new List<DateNightRomanceBond>();
        private static int partnerCacheTick = -1;
        private static readonly Dictionary<int, Pawn> partnerCache = new Dictionary<int, Pawn>();

        public static bool Enabled
        {
            get { return DateNightMod.Settings == null || DateNightMod.Settings.enableGradualRomance; }
        }

        public static void Reset()
        {
            bonds = new List<DateNightRomanceBond>();
            InvalidatePartnerCache();
        }

        public static void ExposeData()
        {
            Scribe_Collections.Look(ref bonds, "dateNightRomanceBonds", LookMode.Deep);
            if (Scribe.mode == LoadSaveMode.PostLoadInit)
            {
                if (bonds == null)
                {
                    bonds = new List<DateNightRomanceBond>();
                }
                InvalidatePartnerCache();
            }
        }

        public static void PruneDeadPawns()
        {
            if (bonds == null || bonds.Count == 0)
            {
                return;
            }

            bool removed = false;
            for (int i = bonds.Count - 1; i >= 0; i--)
            {
                DateNightRomanceBond bond = bonds[i];
                if (bond == null
                    || DateNightDateUtility.FindPawnById(bond.pawnA) == null
                    || DateNightDateUtility.FindPawnById(bond.pawnB) == null)
                {
                    bonds.RemoveAt(i);
                    removed = true;
                }
            }

            if (removed)
            {
                InvalidatePartnerCache();
            }
        }

        public static DateNightRomanceStage GetStage(Pawn a, Pawn b)
        {
            if (a == null || b == null)
            {
                return DateNightRomanceStage.None;
            }
            if (LovePartnerRelationUtility.LovePartnerRelationExists(a, b))
            {
                return DateNightRomanceStage.Partners;
            }

            DateNightRomanceBond bond = GetBond(a, b);
            return bond == null ? DateNightRomanceStage.None : bond.stage;
        }

        public static bool CanDateTogether(Pawn a, Pawn b)
        {
            if (a == null || b == null)
            {
                return false;
            }
            if (LovePartnerRelationUtility.LovePartnerRelationExists(a, b))
            {
                return true;
            }
            if (!Enabled)
            {
                return false;
            }

            DateNightRomanceStage stage = GetStage(a, b);
            if (stage >= DateNightRomanceStage.Friends)
            {
                return true;
            }
            return EligibleForNewBond(a, b);
        }

        public static Pawn FindDatePartner(Pawn pawn)
        {
            if (pawn == null)
            {
                return null;
            }

            int now = Find.TickManager != null ? Find.TickManager.TicksGame : -1;
            if (partnerCacheTick != now)
            {
                partnerCache.Clear();
                partnerCacheTick = now;
            }

            if (partnerCache.TryGetValue(pawn.thingIDNumber, out Pawn cached))
            {
                return cached;
            }

            Pawn found = FindDatePartnerUncached(pawn);
            partnerCache[pawn.thingIDNumber] = found;
            return found;
        }

        public static string InspectLine(Pawn pawn)
        {
            if (!Enabled || pawn == null || !pawn.RaceProps.Humanlike)
            {
                return null;
            }

            Pawn other = FindDatePartner(pawn);
            if (other == null)
            {
                return null;
            }
            if (LovePartnerRelationUtility.LovePartnerRelationExists(pawn, other))
            {
                return null;
            }

            DateNightRomanceStage stage = GetStage(pawn, other);
            if (stage == DateNightRomanceStage.None)
            {
                return null;
            }

            switch (stage)
            {
                case DateNightRomanceStage.Friends:
                    return "DateNight_Inspect_Friends".Translate(other.LabelShort);
                case DateNightRomanceStage.Sweethearts:
                    return "DateNight_Inspect_Sweethearts".Translate(other.LabelShort);
                case DateNightRomanceStage.Partners:
                    return "DateNight_Inspect_Partners".Translate(other.LabelShort, PartnerWord(other));
                default:
                    return null;
            }
        }

        public static string DebugDescribe(Pawn pawn)
        {
            Pawn other = FindDatePartner(pawn);
            if (other == null)
            {
                return "none";
            }

            DateNightRomanceBond bond = GetBond(pawn, other);
            DateNightRomanceStage stage = GetStage(pawn, other);
            int progress = bond != null ? bond.progress : 0;
            return stage + " with " + other.LabelShort + " progress=" + progress
                + (LovePartnerRelationUtility.LovePartnerRelationExists(pawn, other) ? " (lovers)" : "");
        }

        public static void EnsureBondStarted(Pawn a, Pawn b)
        {
            if (!Enabled || a == null || b == null)
            {
                return;
            }
            if (LovePartnerRelationUtility.LovePartnerRelationExists(a, b))
            {
                return;
            }
            if (GetBond(a, b) != null)
            {
                return;
            }
            if (!EligibleForNewBond(a, b))
            {
                return;
            }

            bonds.Add(new DateNightRomanceBond
            {
                pawnA = a.thingIDNumber,
                pawnB = b.thingIDNumber,
                stage = DateNightRomanceStage.Friends,
                lastAdvanceTick = Find.TickManager.TicksGame
            });
            InvalidatePartnerCache();
            NotifyStageThoughts(a, b, DateNightDefOf.DateNight_BecameFriends);
            Messages.Message(
                "DateNight_Message_BecameFriends".Translate(a.LabelShort, b.LabelShort),
                new LookTargets(a, b),
                MessageTypeDefOf.PositiveEvent);
        }

        public static void NotifyDateFinished(Pawn pawn, Pawn partner, ThoughtDef quality)
        {
            if (!Enabled || pawn == null || partner == null)
            {
                return;
            }
            if (LovePartnerRelationUtility.LovePartnerRelationExists(pawn, partner))
            {
                return;
            }

            EnsureBondStarted(pawn, partner);
            DateNightRomanceBond bond = GetBond(pawn, partner);
            if (bond == null)
            {
                return;
            }

            int gain = 1;
            if (quality == DateNightDefOf.DateNight_DateWonderful)
            {
                gain = 2;
            }
            else if (quality == DateNightDefOf.DateNight_DateAwkward)
            {
                gain = 0;
            }
            if (gain <= 0)
            {
                return;
            }

            // Both drivers call NotifyDateFinished; count the date once.
            int now = Find.TickManager.TicksGame;
            if (now - bond.lastAdvanceTick < 60)
            {
                return;
            }

            bond.lastAdvanceTick = now;
            bond.progress += gain;
            TryAdvance(pawn, partner, bond);
        }

        public static void TickBonds()
        {
            if (!Enabled || bonds == null || bonds.Count == 0)
            {
                return;
            }

            int now = Find.TickManager.TicksGame;
            for (int i = 0; i < bonds.Count; i++)
            {
                DateNightRomanceBond bond = bonds[i];
                if (bond == null)
                {
                    continue;
                }

                Pawn a = DateNightDateUtility.FindPawnById(bond.pawnA);
                Pawn b = DateNightDateUtility.FindPawnById(bond.pawnB);
                if (a == null || b == null)
                {
                    continue;
                }

                if (LovePartnerRelationUtility.LovePartnerRelationExists(a, b))
                {
                    bond.stage = DateNightRomanceStage.Partners;
                    bond.progress = 0;
                    continue;
                }

                if (now - bond.lastCoolTick < StepDownCooldownTicks)
                {
                    continue;
                }

                int opA = a.relations != null ? a.relations.OpinionOf(b) : 0;
                int opB = b.relations != null ? b.relations.OpinionOf(a) : 0;
                if (opA < StepDownOpinion || opB < StepDownOpinion)
                {
                    StepDown(a, b, bond);
                    bond.lastCoolTick = now;
                }
            }
        }

        public static void NotifyBreakup(Pawn a, Pawn b)
        {
            DateNightRomanceBond bond = GetBond(a, b);
            if (bond == null)
            {
                return;
            }
            if (bond.stage > DateNightRomanceStage.Sweethearts)
            {
                bond.stage = DateNightRomanceStage.Sweethearts;
                bond.progress = 0;
                bond.lastCoolTick = Find.TickManager.TicksGame;
                InvalidatePartnerCache();
            }
        }

        public static bool TrySetStage(Pawn a, Pawn b, DateNightRomanceStage stage)
        {
            if (a == null || b == null || a == b)
            {
                return false;
            }
            if (stage == DateNightRomanceStage.None)
            {
                DateNightRomanceBond existing = GetBond(a, b);
                if (existing != null)
                {
                    bonds.Remove(existing);
                    InvalidatePartnerCache();
                }
                return true;
            }

            DateNightRomanceBond bond = GetBond(a, b);
            if (bond == null)
            {
                bond = new DateNightRomanceBond
                {
                    pawnA = a.thingIDNumber,
                    pawnB = b.thingIDNumber
                };
                bonds.Add(bond);
            }

            bond.stage = stage;
            bond.progress = 0;
            bond.lastAdvanceTick = Find.TickManager.TicksGame;
            InvalidatePartnerCache();
            return true;
        }

        private static Pawn FindDatePartnerUncached(Pawn pawn)
        {
            Pawn love = LovePartnerRelationUtility.ExistingMostLikedLovePartner(pawn, allowDead: false);
            if (love != null && !love.Dead)
            {
                return love;
            }

            if (!Enabled)
            {
                return null;
            }

            Pawn bestBond = null;
            int bestStage = 0;
            int bestOpinion = int.MinValue;
            if (bonds != null)
            {
                for (int i = 0; i < bonds.Count; i++)
                {
                    DateNightRomanceBond bond = bonds[i];
                    if (bond == null)
                    {
                        continue;
                    }

                    Pawn other = OtherOf(bond, pawn.thingIDNumber);
                    if (other == null || other.Dead || !other.RaceProps.Humanlike)
                    {
                        continue;
                    }

                    int stage = (int)bond.stage;
                    int opinion = pawn.relations != null ? pawn.relations.OpinionOf(other) : 0;
                    if (stage > bestStage || (stage == bestStage && opinion > bestOpinion))
                    {
                        bestBond = other;
                        bestStage = stage;
                        bestOpinion = opinion;
                    }
                }
            }

            if (bestBond != null)
            {
                return bestBond;
            }

            return FindNewFriendCandidate(pawn);
        }

        private static Pawn FindNewFriendCandidate(Pawn pawn)
        {
            if (pawn.Map == null || pawn.relations == null)
            {
                return null;
            }

            List<Pawn> colonists = pawn.Map.mapPawns?.FreeColonistsSpawned;
            if (colonists == null)
            {
                return null;
            }

            Pawn best = null;
            float bestScore = 0f;
            for (int i = 0; i < colonists.Count; i++)
            {
                Pawn other = colonists[i];
                if (!EligibleForNewBond(pawn, other))
                {
                    continue;
                }
                if (!DateNightUtility.IsDateSchedule(other) && !DateNightUtility.IsLovinSchedule(other))
                {
                    continue;
                }

                float chance = RomanceChance(pawn, other);
                int opinion = pawn.relations.OpinionOf(other);
                float score = chance * 100f + opinion;
                if (best == null || score > bestScore)
                {
                    best = other;
                    bestScore = score;
                }
            }

            return best;
        }

        private static void TryAdvance(Pawn a, Pawn b, DateNightRomanceBond bond)
        {
            if (bond.progress < DatesToAdvance)
            {
                return;
            }

            int minOpinion = MinOpinion(a, b);
            if (bond.stage == DateNightRomanceStage.Friends && minOpinion >= OpinionSweethearts)
            {
                SetStage(a, b, bond, DateNightRomanceStage.Sweethearts,
                    DateNightDefOf.DateNight_BecameSweethearts,
                    "DateNight_Message_BecameSweethearts");
                return;
            }

            if (bond.stage == DateNightRomanceStage.Sweethearts && minOpinion >= OpinionPartners)
            {
                SetStage(a, b, bond, DateNightRomanceStage.Partners,
                    DateNightDefOf.DateNight_BecamePartners,
                    "DateNight_Message_BecamePartners");
                return;
            }

            if (bond.stage == DateNightRomanceStage.Partners && minOpinion >= OpinionLover)
            {
                TryBecomeLovers(a, b, bond);
            }
        }

        private static void SetStage(Pawn a, Pawn b, DateNightRomanceBond bond,
            DateNightRomanceStage next, ThoughtDef thought, string messageKey)
        {
            bond.stage = next;
            bond.progress = 0;
            bond.lastAdvanceTick = Find.TickManager.TicksGame;
            InvalidatePartnerCache();
            NotifyStageThoughts(a, b, thought);
            Messages.Message(
                messageKey.Translate(a.LabelShort, b.LabelShort),
                new LookTargets(a, b),
                MessageTypeDefOf.PositiveEvent);
        }

        private static void TryBecomeLovers(Pawn a, Pawn b, DateNightRomanceBond bond)
        {
            if (a.relations == null || b.relations == null)
            {
                return;
            }
            if (LovePartnerRelationUtility.LovePartnerRelationExists(a, b))
            {
                return;
            }
            if (HasOtherLovePartner(a, b) || HasOtherLovePartner(b, a))
            {
                return;
            }
            if (RomanceChance(a, b) <= 0f || RomanceChance(b, a) <= 0f)
            {
                return;
            }

            a.relations.AddDirectRelation(PawnRelationDefOf.Lover, b);
            bond.stage = DateNightRomanceStage.Partners;
            bond.progress = 0;
            bond.lastAdvanceTick = Find.TickManager.TicksGame;
            InvalidatePartnerCache();
            NotifyStageThoughts(a, b, DateNightDefOf.DateNight_BecameLovers);
            Find.LetterStack.ReceiveLetter(
                "DateNight_Letter_BecameLoversLabel".Translate(),
                "DateNight_Letter_BecameLoversText".Translate(a.Named("PAWN"), b.Named("PARTNER")),
                LetterDefOf.PositiveEvent,
                new LookTargets(a, b));
        }

        private static void StepDown(Pawn a, Pawn b, DateNightRomanceBond bond)
        {
            if (bond.stage <= DateNightRomanceStage.Friends)
            {
                bonds.Remove(bond);
                InvalidatePartnerCache();
                return;
            }

            bond.stage = (DateNightRomanceStage)((int)bond.stage - 1);
            bond.progress = 0;
            InvalidatePartnerCache();
        }

        private static bool EligibleForNewBond(Pawn a, Pawn b)
        {
            if (a == null || b == null || a == b)
            {
                return false;
            }
            if (!a.RaceProps.Humanlike || !b.RaceProps.Humanlike)
            {
                return false;
            }
            if (a.Dead || b.Dead || a.Downed || b.Downed)
            {
                return false;
            }
            if (a.ageTracker == null || b.ageTracker == null
                || !a.ageTracker.Adult || !b.ageTracker.Adult)
            {
                return false;
            }
            if (!a.DevelopmentalStage.Adult() || !b.DevelopmentalStage.Adult())
            {
                return false;
            }
            if (a.relations == null || b.relations == null)
            {
                return false;
            }
            if (HasOtherLovePartner(a, b) || HasOtherLovePartner(b, a))
            {
                return false;
            }
            if (AlreadyBondedToSomeoneElse(a, b) || AlreadyBondedToSomeoneElse(b, a))
            {
                return false;
            }
            if (IsIncestuous(a, b))
            {
                return false;
            }
            if (a.relations.OpinionOf(b) < OpinionFriends || b.relations.OpinionOf(a) < OpinionFriends)
            {
                return false;
            }
            if (RomanceChance(a, b) <= 0f || RomanceChance(b, a) <= 0f)
            {
                return false;
            }
            return true;
        }

        private static bool AlreadyBondedToSomeoneElse(Pawn pawn, Pawn except)
        {
            if (bonds == null)
            {
                return false;
            }

            int id = pawn.thingIDNumber;
            int exceptId = except.thingIDNumber;
            for (int i = 0; i < bonds.Count; i++)
            {
                DateNightRomanceBond bond = bonds[i];
                if (bond == null || bond.Matches(id, exceptId))
                {
                    continue;
                }
                if (bond.pawnA == id || bond.pawnB == id)
                {
                    return true;
                }
            }

            return false;
        }

        private static bool HasOtherLovePartner(Pawn pawn, Pawn except)
        {
            Pawn existing = LovePartnerRelationUtility.ExistingMostLikedLovePartner(pawn, allowDead: false);
            return existing != null && existing != except;
        }

        private static bool IsIncestuous(Pawn a, Pawn b)
        {
            try
            {
                foreach (PawnRelationDef rel in a.GetRelations(b))
                {
                    if (rel != null && rel.familyByBloodRelation)
                    {
                        return true;
                    }
                }
            }
            catch
            {
                return false;
            }

            return false;
        }

        private static float RomanceChance(Pawn a, Pawn b)
        {
            try
            {
                return a.relations.SecondaryRomanceChanceFactor(b);
            }
            catch
            {
                return 1f;
            }
        }

        private static int MinOpinion(Pawn a, Pawn b)
        {
            int left = a.relations != null ? a.relations.OpinionOf(b) : 0;
            int right = b.relations != null ? b.relations.OpinionOf(a) : 0;
            return left < right ? left : right;
        }

        private static DateNightRomanceBond GetBond(Pawn a, Pawn b)
        {
            if (a == null || b == null || bonds == null)
            {
                return null;
            }

            int idA = a.thingIDNumber;
            int idB = b.thingIDNumber;
            for (int i = 0; i < bonds.Count; i++)
            {
                DateNightRomanceBond bond = bonds[i];
                if (bond != null && bond.Matches(idA, idB))
                {
                    return bond;
                }
            }

            return null;
        }

        private static Pawn OtherOf(DateNightRomanceBond bond, int id)
        {
            int otherId = bond.pawnA == id ? bond.pawnB : bond.pawnB == id ? bond.pawnA : 0;
            return otherId == 0 ? null : DateNightDateUtility.FindPawnById(otherId);
        }

        private static void NotifyStageThoughts(Pawn a, Pawn b, ThoughtDef def)
        {
            DateNightDateUtility.TryGainThought(a, b, def);
            DateNightDateUtility.TryGainThought(b, a, def);
        }

        private static string PartnerWord(Pawn other)
        {
            if (other.gender == Gender.Female)
            {
                return "DateNight_Girlfriend".Translate();
            }
            if (other.gender == Gender.Male)
            {
                return "DateNight_Boyfriend".Translate();
            }
            return "DateNight_Partner".Translate();
        }

        private static void InvalidatePartnerCache()
        {
            partnerCache.Clear();
            partnerCacheTick = -1;
        }
    }
}
