using System.Collections.Generic;
using RimWorld;
using RimWorld.Planet;
using Verse;

namespace DeepColony
{
    /// <summary>
    /// E06–E10 — prison visit, release kin, tradition teach, kin downed beside you,
    /// and empty nest.
    /// </summary>
    public static class FamilyEchoUtility
    {
        private const int VisitInterval = 2500;
        private const int VisitCooldownTicks = 60000; // 1 day
        private const int VisitRange = 2;
        private const int DownedRange = 12;
        private const int DownedCooldownTicks = 60000;
        private const int EmptyNestCooldownTicks = 600000; // 10 days

        private static bool Enabled => DeepColonySettings.Get.enableFamilyJoin;

        public static void GameTick()
        {
            if (!Enabled) return;
            if (!TickPhase.Due(835)) return;
            foreach (Map map in Find.Maps)
            {
                if (map?.IsPlayerHome != true) continue;
                if (map.mapPawns == null) continue;
                if (map.mapPawns.PrisonersOfColonySpawned == null) continue;
                foreach (Pawn prisoner in map.mapPawns.PrisonersOfColonySpawned)
                    TryProximityVisit(prisoner);
            }
        }

        public static void NotifyPrisonVisit(Pawn visitor, Pawn prisoner)
        {
            if (!Enabled) return;
            if (visitor == null || prisoner == null || visitor == prisoner) return;
            if (!visitor.IsColonistPlayerControlled) return;
            if (!prisoner.IsPrisonerOfColony) return;
            if (visitor.Dead || prisoner.Dead || visitor.Downed || prisoner.Downed) return;
            if (FamilyJoinUtility.KinWeight(visitor, prisoner) <= 0.001f) return;

            var comp = prisoner.TryGetComp<Comp_DeepColony>();
            if (comp == null) return;
            int now = Find.TickManager?.TicksGame ?? 0;
            if (comp.lastFamilyVisitTick >= 0 && now - comp.lastFamilyVisitTick < VisitCooldownTicks)
                return;
            comp.lastFamilyVisitTick = now;

            Gain(prisoner, DC_DefOf.DC_Thought_FamilyPrisonVisit, visitor);
            Gain(visitor, DC_DefOf.DC_Thought_VisitedKinPrisoner, prisoner);
        }

        public static void NotifyReleased(Pawn prisoner)
        {
            if (!Enabled) return;
            if (prisoner == null || !prisoner.RaceProps.Humanlike) return;

            Pawn first = null;
            int count = 0;
            foreach (Pawn colonist in ColonyHumanlikes())
            {
                if (colonist.Dead || colonist == prisoner) continue;
                if (!colonist.IsColonistPlayerControlled) continue;
                if (!FamilyBeatsUtility.IsFamilyOrEx(colonist, prisoner)) continue;
                Gain(colonist, DC_DefOf.DC_Thought_KinReleased, prisoner);
                if (first == null) first = colonist;
                count++;
            }
            if (count == 0 || first == null) return;

            Find.LetterStack.ReceiveLetter(
                "DC_Letter_KinReleasedLabel".Translate(prisoner.LabelShort.Named("PAWN")),
                "DC_Letter_KinReleasedBody".Translate(
                    prisoner.LabelShort.Named("PAWN"),
                    first.LabelShort.Named("KIN")),
                LetterDefOf.PositiveEvent,
                new LookTargets(first, prisoner));
        }

        public static void NotifyTraditionGate(Pawn apprentice, SkillDef skill, int newLevel)
        {
            if (!Enabled) return;
            if (!DeepColonySettings.Get.enableMentoring) return;
            if (apprentice == null || skill == null) return;
            if (newLevel != 5 && newLevel != 15 && newLevel != 20) return;
            TryNoteTradition(apprentice, apprentice.TryGetComp<Comp_DeepColony>()?.mentor, skill);
        }

        public static void NotifyTraditionTaught(Pawn mentor, Pawn apprentice, PerkDef perk)
        {
            if (perk?.skill == null) return;
            TryNoteTradition(apprentice, mentor, perk.skill);
        }

        public static void NotifyKinDowned(Pawn victim, DamageInfo? dinfo)
        {
            if (!Enabled) return;
            if (victim == null || victim.Dead) return;
            if (!victim.RaceProps.Humanlike) return;
            if (!victim.IsColonistPlayerControlled && !victim.IsPrisonerOfColony) return;
            Map map = victim.MapHeld;
            if (map?.mapPawns?.FreeColonistsSpawned == null) return;
            if (!LooksLikeFight(victim, dinfo)) return;

            int now = Find.TickManager?.TicksGame ?? 0;
            Pawn first = null;
            foreach (Pawn witness in map.mapPawns.FreeColonistsSpawned)
            {
                if (witness == victim || witness.Dead || witness.Downed) continue;
                if (!witness.IsColonistPlayerControlled) continue;
                if (!IsAdult(witness)) continue;
                if (FamilyJoinUtility.KinWeight(witness, victim) <= 0.001f) continue;
                if (!witness.Position.InHorDistOf(victim.Position, DownedRange)) continue;

                var comp = witness.TryGetComp<Comp_DeepColony>();
                if (comp == null) continue;
                if (comp.lastKinDownedTick >= 0 && now - comp.lastKinDownedTick < DownedCooldownTicks)
                    continue;
                comp.lastKinDownedTick = now;
                Gain(witness, DC_DefOf.DC_Thought_KinDownedBeside, victim);
                if (first == null) first = witness;
            }
            if (first == null) return;
            Messages.Message(
                "DC_KinDownedBeside".Translate(
                    first.LabelShort.Named("PAWN"),
                    victim.LabelShort.Named("KIN")),
                new LookTargets(first, victim),
                MessageTypeDefOf.NegativeEvent,
                false);
        }

        public static void NotifyChildLeft(Pawn child)
        {
            if (!Enabled) return;
            if (child == null || child.Dead) return;
            if (!child.RaceProps.Humanlike) return;
            if (!IsAdult(child)) return;
            if (child.IsPrisonerOfColony) return;
            var childComp = child.TryGetComp<Comp_DeepColony>();
            if (childComp != null && childComp.kinTakenTick >= 0) return;
            Map home = child.Map;
            if (home == null || !home.IsPlayerHome) return;

            int now = Find.TickManager?.TicksGame ?? 0;
            Pawn firstParent = null;
            foreach (Pawn parent in FamilyTreeUtility.DirectParents(child))
            {
                if (parent == null || parent.Dead) continue;
                if (!parent.IsColonistPlayerControlled) continue;
                if (!IsOnHomeMap(parent, home)) continue;
                // Newborns, kids, and other adults still on the home tile block this.
                if (AnyChildStillOnHomeMap(parent, child, home)) continue;

                var comp = parent.TryGetComp<Comp_DeepColony>();
                if (comp == null) continue;
                if (comp.lastEmptyNestTick >= 0 && now - comp.lastEmptyNestTick < EmptyNestCooldownTicks)
                    continue;
                comp.lastEmptyNestTick = now;
                Gain(parent, DC_DefOf.DC_Thought_EmptyNest, child);
                if (firstParent == null) firstParent = parent;
            }
            if (firstParent == null) return;
            FamilyLetterUtility.NotifyEmptyNest(firstParent, child);
        }

        private static void TryProximityVisit(Pawn prisoner)
        {
            if (prisoner == null || prisoner.Dead || !prisoner.Spawned) return;
            if (!prisoner.IsPrisonerOfColony) return;
            Map map = prisoner.Map;
            if (map?.mapPawns?.FreeColonistsSpawned == null) return;
            foreach (Pawn visitor in map.mapPawns.FreeColonistsSpawned)
            {
                if (visitor == prisoner || visitor.Dead || visitor.Downed) continue;
                if (!visitor.Position.InHorDistOf(prisoner.Position, VisitRange)) continue;
                NotifyPrisonVisit(visitor, prisoner);
            }
        }

        private static void TryNoteTradition(Pawn apprentice, Pawn mentor, SkillDef skill)
        {
            if (!Enabled) return;
            if (!DeepColonySettings.Get.enableMentoring) return;
            if (apprentice == null || skill == null) return;
            var appComp = apprentice.TryGetComp<Comp_DeepColony>();
            if (appComp == null || appComp.traditionTeachNoted) return;
            if (appComp.mentor == null && mentor == null) return;

            Pawn teacher = mentor ?? appComp.mentor;
            SkillDef focus = appComp.GetMentoredSkill();
            if (focus != null && focus != skill) return;
            if (!MatchesTradition(appComp, teacher, skill)) return;

            appComp.traditionTeachNoted = true;
            Gain(apprentice, DC_DefOf.DC_Thought_TraditionTaught, teacher);
            if (teacher != null)
                Gain(teacher, DC_DefOf.DC_Thought_TraditionTaught, apprentice);
            FamilyLetterUtility.NotifyTraditionTaught(teacher ?? apprentice, apprentice, skill);
        }

        private static bool MatchesTradition(Comp_DeepColony apprentice, Pawn mentor, SkillDef skill)
        {
            if (apprentice != null && apprentice.familyTraditionSkillDefName == skill.defName)
                return true;
            var mentorComp = mentor?.TryGetComp<Comp_DeepColony>();
            if (mentorComp != null && mentorComp.familyTraditionSkillDefName == skill.defName)
                return true;
            return false;
        }

        private static bool AnyChildStillOnHomeMap(Pawn parent, Pawn leaving, Map home)
        {
            foreach (Pawn other in FamilyTreeUtility.DirectChildren(parent))
            {
                if (other == null || other == leaving || other.Dead) continue;
                if (IsOnHomeMap(other, home)) return true;
            }
            return false;
        }

        /// <summary>
        /// Still "at home": spawned or carried on the home map, another player-home map,
        /// or a pocket map under that home (Strata floors, gravship decks).
        /// Caravan / world pawns are off-map.
        /// </summary>
        private static bool IsOnHomeMap(Pawn pawn, Map home)
        {
            if (pawn == null || home == null) return false;
            Map map = pawn.MapHeld ?? pawn.Map;
            if (map == null) return false;
            if (map == home || map.IsPlayerHome) return true;
            if (map.Parent is PocketMapParent pocket && pocket.sourceMap != null)
            {
                if (pocket.sourceMap == home || pocket.sourceMap.IsPlayerHome) return true;
            }
            return false;
        }

        private static bool LooksLikeFight(Pawn victim, DamageInfo? dinfo)
        {
            if (!dinfo.HasValue) return false;
            DamageInfo info = dinfo.Value;
            if (info.Instigator is Pawn p && p.HostileTo(victim)) return true;
            return info.Def != null && info.Def.ExternalViolenceFor(victim);
        }

        private static bool IsAdult(Pawn pawn)
        {
            if (ModsConfig.BiotechActive)
                return pawn.DevelopmentalStage >= DevelopmentalStage.Adult;
            return pawn.ageTracker != null && pawn.ageTracker.AgeBiologicalYears >= 13;
        }

        private static IEnumerable<Pawn> ColonyHumanlikes()
        {
            List<Pawn> found = PawnsFinder.AllMapsCaravansAndTravellingTransporters_Alive_FreeColonists;
            if (found != null)
            {
                for (int i = 0; i < found.Count; i++)
                {
                    Pawn p = found[i];
                    if (p != null && p.RaceProps != null && p.RaceProps.Humanlike)
                        yield return p;
                }
                yield break;
            }
            foreach (Map map in Find.Maps)
            {
                if (map?.mapPawns?.FreeColonists == null) continue;
                foreach (Pawn p in map.mapPawns.FreeColonists)
                {
                    if (p != null && p.RaceProps != null && p.RaceProps.Humanlike)
                        yield return p;
                }
            }
        }

        private static void Gain(Pawn pawn, ThoughtDef def, Pawn other)
        {
            if (pawn?.needs?.mood?.thoughts == null || def == null) return;
            var thought = (Thought_Memory)ThoughtMaker.MakeThought(def);
            pawn.needs.mood.thoughts.memories.TryGainMemory(thought, other);
        }
    }
}
