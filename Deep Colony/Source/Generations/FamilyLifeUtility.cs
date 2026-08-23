using System.Collections.Generic;
using RimWorld;
using Verse;

namespace DeepColony
{
    /// <summary>
    /// E01–E05 — grandchild born, kin taken/returned, tended by family,
    /// last of the line / line continues, and step-family.
    /// </summary>
    public static class FamilyLifeUtility
    {
        private const int TendCooldownTicks = 60000; // 1 day
        private const int TakenDedupeTicks = 2500;
        private const int LastOfLineInterval = 2500;

        private static bool Enabled => DeepColonySettings.Get.enableFamilyJoin;

        public static void GameTick()
        {
            if (!Enabled) return;
            if (Find.TickManager.TicksGame % LastOfLineInterval != 0) return;
            RefreshAllLastOfTheLine(announceLast: false, announceContinue: true);
        }

        public static void NotifyBirth(Pawn baby)
        {
            if (!Enabled) return;
            if (baby == null || baby.Dead) return;
            if (!baby.RaceProps.Humanlike) return;
            if (Find.TickManager != null && Find.TickManager.TicksGame < 600) return;
            if (!IsColonyBirth(baby)) return;

            int welcomed = 0;
            Pawn firstGp = null;
            var seen = new HashSet<int>();
            foreach (Pawn parent in FamilyTreeUtility.DirectParents(baby))
            {
                foreach (Pawn gp in FamilyTreeUtility.DirectParents(parent))
                {
                    if (!TryWelcomeAncestor(gp, baby, parent, seen)) continue;
                    if (firstGp == null) firstGp = gp;
                    welcomed++;
                    foreach (Pawn ggp in FamilyTreeUtility.DirectParents(gp))
                    {
                        if (!TryWelcomeAncestor(ggp, baby, parent, seen)) continue;
                        welcomed++;
                    }
                }
            }
            if (welcomed > 0 && firstGp != null)
                FamilyLetterUtility.NotifyGrandchildBorn(baby, firstGp);

            RefreshAllLastOfTheLine(announceLast: false, announceContinue: true);
        }

        public static void NotifyStepFamily(Pawn a, Pawn b)
        {
            if (!Enabled) return;
            if (a == null || b == null) return;
            if (!a.IsColonistPlayerControlled || !b.IsColonistPlayerControlled) return;
            if (a.thingIDNumber > b.thingIDNumber) return;

            int n = 0;
            n += WelcomeStepChildrenOf(a, b);
            n += WelcomeStepChildrenOf(b, a);
            if (n <= 0) return;
            Messages.Message(
                "DC_StepFamily".Translate(
                    a.LabelShort.Named("A"),
                    b.LabelShort.Named("B")),
                new LookTargets(a, b),
                MessageTypeDefOf.PositiveEvent,
                false);
        }

        public static void NotifyTended(Pawn doctor, Pawn patient)
        {
            if (!Enabled) return;
            if (doctor == null || patient == null || doctor == patient) return;
            if (doctor.Dead || patient.Dead) return;
            if (!doctor.RaceProps.Humanlike || !patient.RaceProps.Humanlike) return;
            if (!doctor.IsColonistPlayerControlled) return;
            if (!patient.IsColonistPlayerControlled && !patient.IsPrisonerOfColony) return;
            if (FamilyJoinUtility.KinWeight(doctor, patient) <= 0.001f) return;

            var comp = patient.TryGetComp<Comp_DeepColony>();
            if (comp == null) return;
            int now = Find.TickManager?.TicksGame ?? 0;
            if (comp.lastFamilyTendTick >= 0 && now - comp.lastFamilyTendTick < TendCooldownTicks)
                return;
            comp.lastFamilyTendTick = now;

            Gain(patient, DC_DefOf.DC_Thought_TendedByFamily, doctor);
            Gain(doctor, DC_DefOf.DC_Thought_TendedFamily, patient);
        }

        public static void NotifyTaken(Pawn victim)
        {
            if (!Enabled) return;
            if (victim == null || !victim.RaceProps.Humanlike) return;
            var comp = victim.TryGetComp<Comp_DeepColony>();
            int now = Find.TickManager?.TicksGame ?? 0;
            if (comp != null)
            {
                if (comp.kinTakenTick >= 0 && now - comp.kinTakenTick < TakenDedupeTicks)
                    return;
                comp.kinTakenTick = now;
            }

            Pawn first = null;
            int count = 0;
            foreach (Pawn colonist in ColonyHumanlikes())
            {
                if (colonist.Dead || colonist == victim) continue;
                if (!colonist.IsColonistPlayerControlled) continue;
                if (!FamilyBeatsUtility.IsFamilyOrEx(colonist, victim)) continue;
                Gain(colonist, DC_DefOf.DC_Thought_KinTaken, victim);
                if (first == null) first = colonist;
                count++;
            }
            if (count == 0 || first == null) return;

            Find.LetterStack.ReceiveLetter(
                "DC_Letter_KinTakenLabel".Translate(victim.LabelShort.Named("PAWN")),
                "DC_Letter_KinTakenBody".Translate(
                    victim.LabelShort.Named("PAWN"),
                    first.LabelShort.Named("KIN")),
                LetterDefOf.NegativeEvent,
                new LookTargets(first, victim));
            RefreshAllLastOfTheLine(announceLast: false, announceContinue: false);
        }

        public static void NotifyReturned(Pawn pawn)
        {
            if (!Enabled) return;
            if (pawn == null || pawn.Dead) return;
            var comp = pawn.TryGetComp<Comp_DeepColony>();
            if (comp == null || comp.kinTakenTick < 0) return;
            comp.kinTakenTick = -1;

            Pawn first = null;
            foreach (Pawn colonist in ColonyHumanlikes())
            {
                if (colonist.Dead || colonist == pawn) continue;
                if (!colonist.IsColonistPlayerControlled) continue;
                if (colonist.needs?.mood?.thoughts?.memories != null
                    && DC_DefOf.DC_Thought_KinTaken != null)
                {
                    colonist.needs.mood.thoughts.memories.RemoveMemoriesOfDefWhereOtherPawnIs(
                        DC_DefOf.DC_Thought_KinTaken, pawn);
                }
                if (!FamilyBeatsUtility.IsFamilyOrEx(colonist, pawn)) continue;
                Gain(colonist, DC_DefOf.DC_Thought_KinReturned, pawn);
                if (first == null) first = colonist;
            }
            Gain(pawn, DC_DefOf.DC_Thought_KinReturned, first);
            if (first != null)
            {
                Messages.Message(
                    "DC_KinReturned".Translate(
                        pawn.LabelShort.Named("PAWN"),
                        first.LabelShort.Named("KIN")),
                    new LookTargets(pawn, first),
                    MessageTypeDefOf.PositiveEvent,
                    false);
            }
            RefreshAllLastOfTheLine(announceLast: false, announceContinue: true);
        }

        public static void RefreshAllLastOfTheLine(bool announceLast, bool announceContinue)
        {
            if (!Enabled) return;
            foreach (Pawn colonist in LivingColonyHumanlikes())
                RefreshLastOfTheLine(colonist, announceLast, announceContinue);
        }

        public static bool TryForceLastOfTheLine(Pawn pawn)
        {
            if (pawn == null) return false;
            var comp = pawn.TryGetComp<Comp_DeepColony>();
            if (comp == null) return false;
            comp.sawColonyBloodKin = true;
            comp.lastOfTheLine = true;
            return true;
        }

        public static bool IsBloodKin(Pawn a, Pawn b)
        {
            if (a?.relations == null || b?.relations == null || a == b) return false;
            if (HasDirect(a, b, PawnRelationDefOf.Parent)
                || HasDirect(b, a, PawnRelationDefOf.Parent))
                return true;
            if (PawnRelationDefOf.Sibling?.Worker != null
                && PawnRelationDefOf.Sibling.Worker.InRelation(a, b))
                return true;
            if (PawnRelationDefOf.Grandparent?.Worker != null
                && (PawnRelationDefOf.Grandparent.Worker.InRelation(a, b)
                    || PawnRelationDefOf.Grandparent.Worker.InRelation(b, a)))
                return true;
            return false;
        }

        private static void RefreshLastOfTheLine(Pawn pawn, bool announceLast, bool announceContinue)
        {
            if (pawn == null || pawn.Dead || !pawn.IsColonist) return;
            if (!pawn.RaceProps.Humanlike) return;
            var comp = pawn.TryGetComp<Comp_DeepColony>();
            if (comp == null) return;

            bool living = HasLivingColonyBloodKin(pawn);
            if (living)
            {
                bool restored = comp.lastOfTheLine;
                comp.sawColonyBloodKin = true;
                comp.lastOfTheLine = false;
                if (restored && announceContinue)
                {
                    Gain(pawn, DC_DefOf.DC_Thought_LineContinues, null);
                    Find.LetterStack.ReceiveLetter(
                        "DC_Letter_LineContinuesLabel".Translate(pawn.LabelShort.Named("PAWN")),
                        "DC_Letter_LineContinuesBody".Translate(pawn.LabelShort.Named("PAWN")),
                        LetterDefOf.PositiveEvent,
                        pawn);
                }
                return;
            }

            // Never last-of-the-line until they have actually had colony blood kin
            // (starting pawns with no family stay quiet).
            if (!comp.sawColonyBloodKin) return;
            if (comp.lastOfTheLine) return;
            // Message + flag only when the last kin died — not when they left the map.
            if (!announceLast) return;
            comp.lastOfTheLine = true;
            Messages.Message(
                "DC_LastOfTheLine".Translate(pawn.LabelShort.Named("PAWN")),
                pawn,
                MessageTypeDefOf.NegativeEvent,
                false);
        }

        private static bool HasLivingColonyBloodKin(Pawn pawn)
        {
            foreach (Pawn other in LivingColonyHumanlikes())
            {
                if (other == pawn || other.Dead) continue;
                if (IsBloodKin(pawn, other)) return true;
            }
            return false;
        }

        private static bool TryWelcomeAncestor(Pawn ancestor, Pawn baby, Pawn skipParent, HashSet<int> seen)
        {
            if (ancestor == null || ancestor.Dead) return false;
            if (ancestor == baby || ancestor == skipParent) return false;
            if (!ancestor.IsColonistPlayerControlled) return false;
            if (!seen.Add(ancestor.thingIDNumber)) return false;
            Gain(ancestor, DC_DefOf.DC_Thought_GrandchildBorn, baby);
            return true;
        }

        private static int WelcomeStepChildrenOf(Pawn parent, Pawn newSpouse)
        {
            int n = 0;
            foreach (Pawn child in FamilyTreeUtility.DirectChildren(parent))
            {
                if (child == null || child.Dead || child == newSpouse) continue;
                if (!child.IsColonistPlayerControlled) continue;
                if (child.relations != null
                    && child.relations.DirectRelationExists(PawnRelationDefOf.Parent, newSpouse))
                    continue;
                Gain(child, DC_DefOf.DC_Thought_StepFamily, newSpouse);
                n++;
            }
            return n;
        }

        private static bool IsColonyBirth(Pawn baby)
        {
            if (baby.Faction != null && baby.Faction.IsPlayer) return true;
            foreach (Pawn parent in FamilyTreeUtility.DirectParents(baby))
            {
                if (parent != null && parent.IsColonistPlayerControlled) return true;
            }
            return false;
        }

        private static IEnumerable<Pawn> LivingColonyHumanlikes()
        {
            var seen = new HashSet<int>();
            if (Find.Maps != null)
            {
                foreach (Map map in Find.Maps)
                {
                    if (map?.mapPawns?.AllPawns == null) continue;
                    foreach (Pawn p in map.mapPawns.AllPawns)
                    {
                        TryYieldLivingColonist(p, seen, out Pawn yieldPawn);
                        if (yieldPawn != null) yield return yieldPawn;
                        Pawn carried = p.carryTracker?.CarriedThing as Pawn;
                        if (carried != null)
                        {
                            TryYieldLivingColonist(carried, seen, out Pawn yieldCarried);
                            if (yieldCarried != null) yield return yieldCarried;
                        }
                    }
                }
            }

            List<Pawn> found = PawnsFinder.AllMapsCaravansAndTravellingTransporters_Alive_FreeColonists;
            if (found != null)
            {
                for (int i = 0; i < found.Count; i++)
                {
                    TryYieldLivingColonist(found[i], seen, out Pawn yieldPawn);
                    if (yieldPawn != null) yield return yieldPawn;
                }
            }

            if (Find.WorldPawns?.AllPawnsAlive == null) yield break;
            foreach (Pawn p in Find.WorldPawns.AllPawnsAlive)
            {
                TryYieldLivingColonist(p, seen, out Pawn yieldPawn);
                if (yieldPawn != null) yield return yieldPawn;
            }
        }

        private static void TryYieldLivingColonist(Pawn pawn, HashSet<int> seen, out Pawn result)
        {
            result = null;
            if (pawn == null || pawn.Dead || pawn.Destroyed) return;
            if (pawn.RaceProps == null || !pawn.RaceProps.Humanlike) return;
            if (!pawn.IsColonist) return;
            if (!seen.Add(pawn.thingIDNumber)) return;
            result = pawn;
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

        private static bool HasDirect(Pawn a, Pawn b, PawnRelationDef def)
        {
            if (a?.relations == null || b == null || def == null) return false;
            return a.relations.DirectRelationExists(def, b);
        }

        private static void Gain(Pawn pawn, ThoughtDef def, Pawn other)
        {
            if (pawn?.needs?.mood?.thoughts == null || def == null) return;
            var thought = (Thought_Memory)ThoughtMaker.MakeThought(def);
            pawn.needs.mood.thoughts.memories.TryGainMemory(thought, other);
        }
    }
}
