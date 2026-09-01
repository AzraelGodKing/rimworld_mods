using System.Collections.Generic;
using RimWorld;
using Verse;
using Verse.AI;

namespace DeepColony
{
    /// <summary>
    /// AZR-70 — wills, contested inheritance, unclaimed beds, heirloom lineage.
    /// HeirloomUtility still marks the item; this decides who it is for.
    /// </summary>
    public static class EstateUtility
    {
        public static bool Enabled => DeepColonySettings.Get.enableEstate;

        public static List<Pawn> CandidateHeirs(Pawn owner)
        {
            var list = new List<Pawn>();
            if (owner?.relations == null) return list;
            void Add(Pawn p)
            {
                if (p == null || p.Dead || p == owner) return;
                if (!p.IsColonistPlayerControlled) return;
                if (!list.Contains(p)) list.Add(p);
            }

            if (owner.GetSpouseCount(includeDead: false) > 0)
            {
                foreach (Pawn spouse in owner.GetSpouses(includeDead: false))
                    Add(spouse);
            }
            foreach (Pawn child in owner.relations.Children)
                Add(child);
            Pawn mother = owner.GetMother();
            Pawn father = owner.GetFather();
            Add(mother);
            Add(father);
            if (PawnRelationDefOf.Sibling != null)
            {
                foreach (Pawn other in PawnsFinder.AllMaps_FreeColonists)
                {
                    if (other == owner) continue;
                    if (PawnRelationDefOf.Sibling.Worker.InRelation(owner, other))
                        Add(other);
                }
            }
            return list;
        }

        public static Pawn ResolveNamedHeir(Pawn owner)
        {
            var comp = owner?.TryGetComp<Comp_DeepColony>();
            if (comp == null || comp.willHeirId < 0) return null;
            return FindPawnById(comp.willHeirId);
        }

        public static void SetHeir(Pawn owner, Pawn heir)
        {
            var comp = owner?.TryGetComp<Comp_DeepColony>();
            if (comp == null) return;
            comp.willHeirId = heir?.thingIDNumber ?? -1;
        }

        public static Pawn ResolveHeirOnDeath(Pawn victim)
        {
            if (victim == null) return null;
            Pawn named = ResolveNamedHeir(victim);
            if (named != null && !named.Dead && named.IsColonistPlayerControlled)
                return named;

            Pawn bestChild = null;
            int bestAge = -1;
            if (victim.relations != null)
            {
                foreach (Pawn child in victim.relations.Children)
                {
                    if (child == null || child.Dead || !child.IsColonistPlayerControlled) continue;
                    int age = child.ageTracker?.AgeBiologicalYears ?? 0;
                    if (age > bestAge)
                    {
                        bestAge = age;
                        bestChild = child;
                    }
                }
            }
            if (bestChild != null) return bestChild;

            if (victim.GetSpouseCount(includeDead: false) > 0)
            {
                foreach (Pawn spouse in victim.GetSpouses(includeDead: false))
                {
                    if (spouse != null && !spouse.Dead && spouse.IsColonistPlayerControlled)
                        return spouse;
                }
            }
            return null;
        }

        public static void NotifyDeath(Pawn victim)
        {
            if (!Enabled || victim == null) return;
            if (victim.Faction != Faction.OfPlayer && !victim.IsColonist) return;

            var gc = GameComp_DeepColony.Instance;
            Building_Bed bed = victim.ownership?.OwnedBed;
            if (gc != null && bed != null && !bed.Destroyed)
                gc.NoteUnclaimedBed(bed.thingIDNumber, victim.LabelShort);

            Pawn heir = ResolveHeirOnDeath(victim);
            if (heir != null)
            {
                Gain(heir, DC_DefOf.DC_Thought_Inherited, victim);
                FamilyLetterUtility.NotifyEstate(victim, heir);
            }

            if (victim.relations == null) return;
            foreach (Pawn child in victim.relations.Children)
            {
                if (child == null || child.Dead || child == heir) continue;
                if (!child.IsColonistPlayerControlled) continue;
                Gain(child, DC_DefOf.DC_Thought_Disinherited, victim);
            }
        }

        public static void TickUnclaimedBeds(Pawn sleeper)
        {
            if (!Enabled || sleeper == null || !sleeper.IsColonistPlayerControlled) return;
            if (sleeper.jobs?.curDriver is not JobDriver_LayDown lay || !lay.asleep) return;
            Building_Bed bed = sleeper.CurrentBed();
            if (bed == null) return;
            var gc = GameComp_DeepColony.Instance;
            if (gc == null) return;
            if (!gc.TryClaimUnclaimedBed(bed.thingIDNumber, out string former))
                return;
            if (former.NullOrEmpty()) return;
            if (DC_DefOf.DC_Thought_MovedIntoDeadRoom == null
                || sleeper.needs?.mood?.thoughts == null)
                return;
            var thought = (Thought_Memory)ThoughtMaker.MakeThought(DC_DefOf.DC_Thought_MovedIntoDeadRoom);
            sleeper.needs.mood.thoughts.memories.TryGainMemory(thought);
            Messages.Message(
                "DC_MovedIntoDeadRoom".Translate(
                    sleeper.LabelShort.Named("PAWN"),
                    former.Named("OWNER")),
                sleeper,
                MessageTypeDefOf.NeutralEvent,
                false);
        }

        public static string InspectHeir(Pawn pawn)
        {
            if (!Enabled) return null;
            Pawn heir = ResolveNamedHeir(pawn);
            if (heir == null) return null;
            return "DC_InspectWillHeir".Translate(heir.LabelShort);
        }

        public static string HeirloomInspect(Pawn pawn)
        {
            var gc = GameComp_DeepColony.Instance;
            if (gc == null || pawn == null) return null;
            Thing gear = pawn.equipment?.Primary;
            if (gear != null && gc.IsHeirloom(gear.thingIDNumber))
            {
                string line = gc.GetHeirloomLineage(gear.thingIDNumber);
                if (!line.NullOrEmpty())
                    return "DC_InspectHeirloomLine".Translate(line);
            }
            if (pawn.apparel?.WornApparel == null) return null;
            foreach (Apparel a in pawn.apparel.WornApparel)
            {
                if (!gc.IsHeirloom(a.thingIDNumber)) continue;
                string line = gc.GetHeirloomLineage(a.thingIDNumber);
                if (!line.NullOrEmpty())
                    return "DC_InspectHeirloomLine".Translate(line);
            }
            return null;
        }

        private static Pawn FindPawnById(int id)
        {
            if (id < 0) return null;
            foreach (Map map in Find.Maps)
            {
                if (map?.mapPawns?.AllPawns == null) continue;
                foreach (Pawn p in map.mapPawns.AllPawns)
                {
                    if (p.thingIDNumber == id) return p;
                }
            }
            List<Pawn> world = PawnsFinder.AllMapsCaravansAndTravellingTransporters_Alive_FreeColonists;
            if (world != null)
            {
                for (int i = 0; i < world.Count; i++)
                {
                    if (world[i].thingIDNumber == id) return world[i];
                }
            }
            return null;
        }

        private static void Gain(Pawn pawn, ThoughtDef def, Pawn other)
        {
            if (pawn?.needs?.mood?.thoughts == null || def == null) return;
            var thought = (Thought_Memory)ThoughtMaker.MakeThought(def);
            pawn.needs.mood.thoughts.memories.TryGainMemory(thought, other);
        }
    }
}
