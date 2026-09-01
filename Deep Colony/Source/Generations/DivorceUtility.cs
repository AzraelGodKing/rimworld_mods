using System.Collections.Generic;
using RimWorld;
using Verse;

namespace DeepColony
{
    /// <summary>
    /// AZR-97 — player-facing divorce. Adds ExSpouse so D23 breakup wound fires
    /// once via the existing relation patch. Date Night still owns romance jobs.
    /// </summary>
    public static class DivorceUtility
    {
        public static bool AreSpouses(Pawn a, Pawn b)
        {
            if (a?.relations == null || b?.relations == null) return false;
            if (PawnRelationDefOf.Spouse == null) return false;
            return a.relations.DirectRelationExists(PawnRelationDefOf.Spouse, b)
                || b.relations.DirectRelationExists(PawnRelationDefOf.Spouse, a);
        }

        public static bool CanDivorce(Pawn actor, Pawn other, out string reason)
        {
            reason = null;
            if (!DeepColonySettings.Get.enableFamilyJoin)
            {
                reason = "DC_DivorceDisabled".Translate();
                return false;
            }
            if (actor == null || other == null || actor == other)
            {
                reason = "DC_DivorceInvalid".Translate();
                return false;
            }
            if (!actor.IsColonistPlayerControlled || !other.IsColonistPlayerControlled)
            {
                reason = "DC_DivorceNeedColonists".Translate();
                return false;
            }
            if (!AreSpouses(actor, other))
            {
                reason = "DC_DivorceNeedSpouse".Translate();
                return false;
            }
            return true;
        }

        public static void TryDivorce(Pawn a, Pawn b)
        {
            if (!CanDivorce(a, b, out _)) return;
            if (a.thingIDNumber > b.thingIDNumber)
            {
                Pawn tmp = a;
                a = b;
                b = tmp;
            }

            a.relations.TryRemoveDirectRelation(PawnRelationDefOf.Spouse, b);
            b.relations.TryRemoveDirectRelation(PawnRelationDefOf.Spouse, a);
            if (PawnRelationDefOf.Fiance != null)
            {
                a.relations.TryRemoveDirectRelation(PawnRelationDefOf.Fiance, b);
                b.relations.TryRemoveDirectRelation(PawnRelationDefOf.Fiance, a);
            }

            if (PawnRelationDefOf.ExSpouse != null)
            {
                if (!a.relations.DirectRelationExists(PawnRelationDefOf.ExSpouse, b))
                    a.relations.AddDirectRelation(PawnRelationDefOf.ExSpouse, b);
                if (!b.relations.DirectRelationExists(PawnRelationDefOf.ExSpouse, a))
                    b.relations.AddDirectRelation(PawnRelationDefOf.ExSpouse, a);
            }

            NotifyChildren(a, b);
            FamilyLetterUtility.NotifyDivorce(a, b);

            Messages.Message(
                "DC_Divorced".Translate(
                    a.LabelShort.Named("A"),
                    b.LabelShort.Named("B")),
                new LookTargets(a, b),
                MessageTypeDefOf.NegativeEvent,
                false);
        }

        private static void NotifyChildren(Pawn a, Pawn b)
        {
            if (DC_DefOf.DC_Thought_ParentsDivorced == null) return;
            var kids = new HashSet<Pawn>();
            CollectChildren(a, kids);
            CollectChildren(b, kids);
            foreach (Pawn child in kids)
            {
                if (child == null || child.Dead || child == a || child == b) continue;
                if (!child.IsColonistPlayerControlled) continue;
                if (child.needs?.mood?.thoughts == null) continue;
                var thought = (Thought_Memory)ThoughtMaker.MakeThought(DC_DefOf.DC_Thought_ParentsDivorced);
                child.needs.mood.thoughts.memories.TryGainMemory(thought, a);
            }
        }

        private static void CollectChildren(Pawn parent, HashSet<Pawn> into)
        {
            if (parent?.relations == null) return;
            foreach (Pawn child in parent.relations.Children)
            {
                if (child != null) into.Add(child);
            }
        }
    }
}
