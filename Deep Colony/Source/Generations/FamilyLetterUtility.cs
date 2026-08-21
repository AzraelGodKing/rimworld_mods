using System.Collections.Generic;
using RimWorld;
using Verse;

namespace DeepColony
{
    public class FamilyLetterEntry : IExposable
    {
        public string title;
        public string body;
        public int ticksGame;

        public void ExposeData()
        {
            Scribe_Values.Look(ref title, "title");
            Scribe_Values.Look(ref body, "body");
            Scribe_Values.Look(ref ticksGame, "ticksGame", 0);
        }
    }

    /// <summary>C11 — rare family notes on the Legacy tab (capped, not world news).</summary>
    public static class FamilyLetterUtility
    {
        private const int MinDaysBetween = 10;
        private const int MaxStored = 8;
        private const int CheckInterval = 2500;

        public static void GameTick()
        {
            if (!DeepColonySettings.Get.enableInheritance) return;
            if (Find.TickManager.TicksGame % CheckInterval != 0) return;

            var gc = GameComp_DeepColony.Instance;
            if (gc == null) return;

            int now = Find.TickManager.TicksGame;
            if (gc.lastFamilyLetterTick >= 0
                && now - gc.lastFamilyLetterTick < MinDaysBetween * 60000)
                return;

            foreach (Map map in Find.Maps)
            {
                foreach (Pawn p in map.mapPawns.FreeColonistsSpawned)
                {
                    if (p.Dead || !p.RaceProps.Humanlike) continue;
                    if (TryBirthday(p, gc, now)) return;
                    if (TryAnniversary(p, gc, now)) return;
                }
            }
        }

        private static bool TryBirthday(Pawn pawn, GameComp_DeepColony gc, int now)
        {
            if (pawn.ageTracker == null) return false;
            long ageTicks = pawn.ageTracker.AgeBiologicalTicks;
            long year = 3600000L;
            if (ageTicks < year) return false;
            long intoYear = ageTicks % year;
            // Birthday window: first ~6 hours of the biological year.
            if (intoYear > 15000L) return false;
            if (!HasLivingFamily(pawn)) return false;

            string title = "DC_FamilyLetter_BirthdayLabel".Translate(pawn.LabelShort.Named("PAWN"));
            string body = "DC_FamilyLetter_BirthdayBody".Translate(pawn.LabelShort.Named("PAWN"));
            Post(gc, title, body, now, pawn);
            return true;
        }

        private static bool TryAnniversary(Pawn pawn, GameComp_DeepColony gc, int now)
        {
            if (pawn.relations == null) return false;
            Pawn lover = LovePartnerRelationUtility.ExistingMostLikedLovePartner(pawn, allowDead: false);
            if (lover == null || lover.Dead || !lover.IsColonistPlayerControlled) return false;
            if (pawn.thingIDNumber > lover.thingIDNumber) return false; // one letter per pair

            DirectPawnRelation rel = pawn.relations.GetDirectRelation(PawnRelationDefOf.Lover, lover)
                ?? pawn.relations.GetDirectRelation(PawnRelationDefOf.Fiance, lover)
                ?? pawn.relations.GetDirectRelation(PawnRelationDefOf.Spouse, lover);
            if (rel == null) return false;
            int start = rel.startTicks;
            if (start <= 0) return false;
            int elapsed = now - start;
            if (elapsed < 3600000) return false; // at least one year
            int intoYear = elapsed % 3600000;
            if (intoYear > 15000) return false;

            string title = "DC_FamilyLetter_AnniversaryLabel".Translate();
            string body = "DC_FamilyLetter_AnniversaryBody".Translate(
                pawn.LabelShort.Named("A"),
                lover.LabelShort.Named("B"));
            Post(gc, title, body, now, pawn);
            return true;
        }

        private static bool HasLivingFamily(Pawn pawn)
        {
            if (pawn.relations == null) return false;
            IEnumerable<Pawn> kin = CandidateKin();
            foreach (Pawn other in kin)
            {
                if (other == pawn || other.Dead) continue;
                if (!other.IsColonistPlayerControlled) continue;
                if (MentorshipUtility.IsLineagePair(pawn, other)) return true;
                if (LovePartnerRelationUtility.LovePartnerRelationExists(pawn, other)) return true;
            }
            return false;
        }

        private static IEnumerable<Pawn> CandidateKin()
        {
            List<Pawn> found = PawnsFinder.AllMapsCaravansAndTravellingTransporters_Alive_FreeColonists;
            if (found != null) return found;
            var fallback = new List<Pawn>();
            foreach (Map map in Find.Maps)
            {
                if (map?.mapPawns?.FreeColonists == null) continue;
                fallback.AddRange(map.mapPawns.FreeColonists);
            }
            return fallback;
        }

        private static void Post(GameComp_DeepColony gc, string title, string body, int now, Pawn look)
        {
            if (gc.familyLetters == null)
                gc.familyLetters = new List<FamilyLetterEntry>();
            gc.familyLetters.Add(new FamilyLetterEntry
            {
                title = title,
                body = body,
                ticksGame = now
            });
            while (gc.familyLetters.Count > MaxStored)
                gc.familyLetters.RemoveAt(0);
            gc.lastFamilyLetterTick = now;

            Find.LetterStack.ReceiveLetter(title, body, LetterDefOf.PositiveEvent, look);
        }

        public static void NotifyGrandchildBorn(Pawn baby, Pawn grandparent)
        {
            if (!DeepColonySettings.Get.enableFamilyJoin) return;
            if (baby == null || grandparent == null) return;
            var gc = GameComp_DeepColony.Instance;
            if (gc == null) return;
            int now = Find.TickManager.TicksGame;
            if (gc.lastFamilyLetterTick >= 0 && now - gc.lastFamilyLetterTick < 2500)
                return;
            string title = "DC_FamilyLetter_GrandchildLabel".Translate(baby.LabelShort.Named("BABY"));
            string body = "DC_FamilyLetter_GrandchildBody".Translate(
                grandparent.LabelShort.Named("KIN"),
                baby.LabelShort.Named("BABY"));
            Post(gc, title, body, now, grandparent);
        }

        public static void NotifyComingOfAge(Pawn pawn)
        {
            if (!DeepColonySettings.Get.enableInheritance) return;
            if (pawn == null) return;
            var gc = GameComp_DeepColony.Instance;
            if (gc == null) return;
            string title = "DC_FamilyLetter_ComingOfAgeLabel".Translate(pawn.LabelShort.Named("PAWN"));
            string body = "DC_FamilyLetter_ComingOfAgeBody".Translate(pawn.LabelShort.Named("PAWN"));
            Post(gc, title, body, Find.TickManager.TicksGame, pawn);
        }

        public static void NotifyMarriage(Pawn a, Pawn b)
        {
            if (!DeepColonySettings.Get.enableInheritance) return;
            if (a == null || b == null) return;
            if (!a.IsColonistPlayerControlled || !b.IsColonistPlayerControlled) return;
            if (a.thingIDNumber > b.thingIDNumber) { Pawn tmp = a; a = b; b = tmp; }

            var gc = GameComp_DeepColony.Instance;
            if (gc == null) return;
            int now = Find.TickManager.TicksGame;
            if (gc.lastFamilyLetterTick >= 0 && now - gc.lastFamilyLetterTick < 2500)
                return;

            string title = "DC_FamilyLetter_MarriageLabel".Translate();
            string body = "DC_FamilyLetter_MarriageBody".Translate(
                a.LabelShort.Named("A"),
                b.LabelShort.Named("B"));
            Post(gc, title, body, now, a);
        }

        public static void NotifyFirstHarvest(Pawn harvester, Thing plant)
        {
            if (!DeepColonySettings.Get.enableInheritance) return;
            if (harvester == null || !harvester.IsColonistPlayerControlled) return;
            if (plant is not Plant p) return;
            if (!p.sown && !IsHomesteaderPlant(p)) return;
            if (!LooksLikeFoodCrop(p)) return;

            var gc = GameComp_DeepColony.Instance;
            if (gc == null || gc.firstHarvestLetterSent) return;
            if (!HasLivingFamily(harvester)) return;

            gc.firstHarvestLetterSent = true;
            string title = "DC_FamilyLetter_HarvestLabel".Translate();
            string body = "DC_FamilyLetter_HarvestBody".Translate(harvester.LabelShort.Named("PAWN"));
            Post(gc, title, body, Find.TickManager.TicksGame, harvester);
        }

        private static bool IsHomesteaderPlant(Plant plant)
        {
            string n = plant?.def?.defName ?? "";
            return n.StartsWith("Homesteader_");
        }

        private static bool LooksLikeFoodCrop(Plant plant)
        {
            if (plant?.def?.plant == null) return false;
            if (plant.def.plant.harvestedThingDef == null) return false;
            return plant.def.plant.harvestedThingDef.IsNutritionGivingIngestible
                || plant.def.plant.harvestedThingDef.IsIngestible;
        }
    }
}
