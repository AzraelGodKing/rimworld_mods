using System.Collections.Generic;
using RimWorld;
using Verse;

namespace DeepColony
{
    /// <summary>
    /// D20–D24 — in-law welcome, kin homecoming, kin dying on the other side,
    /// breakup wound, and executing family.
    /// </summary>
    public static class FamilyBeatsUtility
    {
        [System.ThreadStatic]
        private static int executingVictimId = -1;

        private const int HomecomingMinAwayTicks = 20000; // ~8 hours
        private const int HomecomingCooldownTicks = 600000; // 10 days

        private static bool Enabled => DeepColonySettings.Get.enableFamilyJoin;

        public static void MarkLeavingHomeMap(Pawn pawn)
        {
            if (pawn == null || pawn.Dead) return;
            if (!pawn.IsColonistPlayerControlled) return;
            var comp = pawn.TryGetComp<Comp_DeepColony>();
            if (comp == null) return;
            comp.leftColonyMapTick = Find.TickManager?.TicksGame ?? 0;
        }

        public static void NotifySpawned(Pawn pawn)
        {
            if (!Enabled) return;
            if (pawn == null || pawn.Dead || !pawn.IsColonistPlayerControlled) return;
            if (!pawn.RaceProps.Humanlike) return;
            if (pawn.Map == null || !pawn.Map.IsPlayerHome) return;

            var comp = pawn.TryGetComp<Comp_DeepColony>();
            if (comp == null) return;
            if (comp.leftColonyMapTick < 0) return;
            int now = Find.TickManager?.TicksGame ?? 0;
            if (now - comp.leftColonyMapTick < HomecomingMinAwayTicks) return;
            if (comp.lastHomecomingTick >= 0 && now - comp.lastHomecomingTick < HomecomingCooldownTicks)
                return;

            Pawn kin = FindBestKinOnMap(pawn);
            if (kin == null) return;

            comp.lastHomecomingTick = now;
            Gain(pawn, DC_DefOf.DC_Thought_KinHomecoming, kin);
            Gain(kin, DC_DefOf.DC_Thought_KinHomecoming, pawn);
            Messages.Message(
                "DC_KinHomecoming".Translate(
                    pawn.LabelShort.Named("PAWN"),
                    kin.LabelShort.Named("KIN")),
                new LookTargets(pawn, kin),
                MessageTypeDefOf.PositiveEvent,
                false);
        }

        public static bool TryForceHomecoming(Pawn pawn)
        {
            if (pawn == null) return false;
            var comp = pawn.TryGetComp<Comp_DeepColony>();
            if (comp == null) return false;
            int now = Find.TickManager?.TicksGame ?? HomecomingMinAwayTicks + 1;
            comp.leftColonyMapTick = now - HomecomingMinAwayTicks - 1;
            comp.lastHomecomingTick = -1;
            NotifySpawned(pawn);
            return comp.lastHomecomingTick >= 0;
        }

        public static void NotifyMarriage(Pawn a, Pawn b)
        {
            if (!Enabled) return;
            if (a == null || b == null) return;
            if (!a.IsColonistPlayerControlled || !b.IsColonistPlayerControlled) return;
            if (a.thingIDNumber > b.thingIDNumber) return;

            int welcomed = 0;
            welcomed += WelcomeInLawsOf(a, b);
            welcomed += WelcomeInLawsOf(b, a);
            if (welcomed <= 0) return;
            Messages.Message(
                "DC_InLawWelcome".Translate(
                    a.LabelShort.Named("A"),
                    b.LabelShort.Named("B")),
                new LookTargets(a, b),
                MessageTypeDefOf.PositiveEvent,
                false);
        }

        public static void NotifyBreakup(Pawn a, Pawn b)
        {
            if (!Enabled) return;
            if (a == null || b == null) return;
            if (a.thingIDNumber > b.thingIDNumber) return;
            if (!a.RaceProps.Humanlike || !b.RaceProps.Humanlike) return;
            if (!a.IsColonistPlayerControlled && !b.IsColonistPlayerControlled) return;
            if (Find.TickManager != null && Find.TickManager.TicksGame < 600) return;

            Gain(a, DC_DefOf.DC_Thought_BreakupWound, b);
            Gain(b, DC_DefOf.DC_Thought_BreakupWound, a);
            Messages.Message(
                "DC_BreakupWound".Translate(
                    a.LabelShort.Named("A"),
                    b.LabelShort.Named("B")),
                new LookTargets(a, b),
                MessageTypeDefOf.NegativeEvent,
                false);

            if (DeepColonySettings.Get.enableTrauma
                && DC_DefOf.DC_Trauma_ToxicRelationship != null)
            {
                if (TraumaUtility.HasTrauma(a, DC_DefOf.DC_Trauma_ToxicRelationship))
                    TraumaUtility.ApplyTrauma(a, DC_DefOf.DC_Trauma_ToxicRelationship, b);
                if (TraumaUtility.HasTrauma(b, DC_DefOf.DC_Trauma_ToxicRelationship))
                    TraumaUtility.ApplyTrauma(b, DC_DefOf.DC_Trauma_ToxicRelationship, a);
            }
        }

        public static void BeginExecution(Pawn victim)
        {
            executingVictimId = victim != null ? victim.thingIDNumber : -1;
        }

        public static void EndExecution(Pawn victim, Pawn executor)
        {
            try
            {
                NotifyKinExecuted(victim, executor);
            }
            finally
            {
                executingVictimId = -1;
            }
        }

        public static void NotifyDied(Pawn victim)
        {
            if (!Enabled) return;
            if (victim == null || !victim.RaceProps.Humanlike) return;
            if (executingVictimId == victim.thingIDNumber) return;
            if (victim.Faction != null && victim.Faction.IsPlayer) return;
            if (victim.IsColonist) return;

            Map map = victim.MapHeld;
            if (map == null || map.mapPawns?.FreeColonistsSpawned == null) return;

            Pawn first = null;
            int count = 0;
            foreach (Pawn colonist in map.mapPawns.FreeColonistsSpawned)
            {
                if (colonist.Dead || colonist == victim) continue;
                if (!colonist.IsColonistPlayerControlled) continue;
                if (!IsFamilyOrEx(colonist, victim)) continue;
                Gain(colonist, DC_DefOf.DC_Thought_KinDiedOtherSide, victim);
                if (first == null) first = colonist;
                count++;
            }
            if (count == 0 || first == null) return;

            Find.LetterStack.ReceiveLetter(
                "DC_Letter_KinDiedLabel".Translate(victim.LabelShort.Named("PAWN")),
                "DC_Letter_KinDiedBody".Translate(
                    victim.LabelShort.Named("PAWN"),
                    first.LabelShort.Named("KIN")),
                LetterDefOf.NegativeEvent,
                new LookTargets(victim.Corpse ?? (Thing)victim, first));
        }

        private static void NotifyKinExecuted(Pawn victim, Pawn executor)
        {
            if (!Enabled) return;
            if (victim == null || !victim.RaceProps.Humanlike) return;
            Map map = victim.MapHeld;
            if (map?.mapPawns?.FreeColonistsSpawned == null) return;

            Pawn first = null;
            int count = 0;
            foreach (Pawn colonist in map.mapPawns.FreeColonistsSpawned)
            {
                if (colonist.Dead || colonist == victim) continue;
                if (!colonist.IsColonistPlayerControlled) continue;
                if (!IsFamilyOrEx(colonist, victim)) continue;
                Gain(colonist, DC_DefOf.DC_Thought_KinExecuted, victim);
                if (DeepColonySettings.Get.enableTrauma && DC_DefOf.DC_Trauma_Betrayal != null)
                    TraumaUtility.ApplyTrauma(colonist, DC_DefOf.DC_Trauma_Betrayal, victim);
                if (first == null) first = colonist;
                count++;
            }
            if (count == 0 || first == null) return;

            string execName = executor?.LabelShort ?? "warden";
            Thing look = (Thing)victim.Corpse ?? victim;
            Find.LetterStack.ReceiveLetter(
                "DC_Letter_KinExecutedLabel".Translate(victim.LabelShort.Named("PAWN")),
                "DC_Letter_KinExecutedBody".Translate(
                    victim.LabelShort.Named("PAWN"),
                    execName.Named("WARDEN"),
                    first.LabelShort.Named("KIN")),
                LetterDefOf.NegativeEvent,
                new LookTargets(look, first));
            ExternalDiplomacySoftCompat.OnKinExecuted(victim);
        }

        public static bool IsFamilyOrEx(Pawn a, Pawn b)
        {
            if (FamilyJoinUtility.KinWeight(a, b) > 0.001f) return true;
            return HasDirect(a, b, PawnRelationDefOf.ExLover)
                || HasDirect(a, b, PawnRelationDefOf.ExSpouse);
        }

        private static int WelcomeInLawsOf(Pawn spouse, Pawn newInLaw)
        {
            int n = 0;
            foreach (Pawn kin in InLawsOnMap(spouse, newInLaw))
            {
                Gain(kin, DC_DefOf.DC_Thought_InLawWelcome, newInLaw);
                n++;
            }
            return n;
        }

        private static IEnumerable<Pawn> InLawsOnMap(Pawn spouse, Pawn newInLaw)
        {
            if (spouse?.Map == null) yield break;
            var seen = new HashSet<int>();
            foreach (Pawn parent in FamilyTreeUtility.DirectParents(spouse))
            {
                if (!IsLivingColonistOnMap(parent, spouse.Map, spouse, newInLaw)) continue;
                if (!seen.Add(parent.thingIDNumber)) continue;
                yield return parent;
            }
            if (PawnRelationDefOf.Sibling?.Worker == null || spouse.relations == null) yield break;
            foreach (Pawn other in spouse.relations.RelatedPawns)
            {
                if (other == null || other == spouse || other == newInLaw) continue;
                if (!PawnRelationDefOf.Sibling.Worker.InRelation(spouse, other)) continue;
                if (!IsLivingColonistOnMap(other, spouse.Map, spouse, newInLaw)) continue;
                if (!seen.Add(other.thingIDNumber)) continue;
                yield return other;
            }
        }

        private static bool IsLivingColonistOnMap(Pawn pawn, Map map, Pawn skipA, Pawn skipB)
        {
            if (pawn == null || pawn.Dead || pawn == skipA || pawn == skipB) return false;
            if (!pawn.IsColonistPlayerControlled) return false;
            return pawn.MapHeld == map;
        }

        private static Pawn FindBestKinOnMap(Pawn pawn)
        {
            if (pawn.Map?.mapPawns?.FreeColonistsSpawned == null) return null;
            Pawn best = null;
            float bestW = 0f;
            foreach (Pawn other in pawn.Map.mapPawns.FreeColonistsSpawned)
            {
                if (other == pawn || other.Dead) continue;
                float w = FamilyJoinUtility.KinWeight(pawn, other);
                if (w > bestW)
                {
                    bestW = w;
                    best = other;
                }
            }
            return best;
        }

        private static bool HasDirect(Pawn a, Pawn b, PawnRelationDef def)
        {
            if (a?.relations == null || b?.relations == null || def == null) return false;
            return a.relations.DirectRelationExists(def, b)
                || b.relations.DirectRelationExists(def, a);
        }

        private static void Gain(Pawn pawn, ThoughtDef def, Pawn other)
        {
            if (pawn?.needs?.mood?.thoughts == null || def == null) return;
            var thought = (Thought_Memory)ThoughtMaker.MakeThought(def);
            pawn.needs.mood.thoughts.memories.TryGainMemory(thought, other);
        }
    }
}
