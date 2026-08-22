using System.Collections.Generic;
using RimWorld;
using Verse;

namespace DeepColony
{
    /// <summary>D16 — blood / marriage kin around a pawn (dead included).</summary>
    public static class FamilyTreeUtility
    {
        public static bool IsVisibleFor(Pawn pawn)
        {
            if (pawn?.RaceProps == null || !pawn.RaceProps.Humanlike) return false;
            if (pawn.relations == null) return false;
            if (pawn.IsColonist || pawn.IsColonistPlayerControlled) return true;
            if (pawn.IsPrisonerOfColony) return true;
            if (pawn.IsSlaveOfColony) return true;
            if (pawn.Faction != null && pawn.Faction.IsPlayer) return true;
            if (GameComp_DeepColony.Instance?.WasEverPlayerColonist(pawn) == true) return true;
            return false;
        }

        public static FamilyTreeSnapshot Build(Pawn focus)
        {
            var snap = new FamilyTreeSnapshot { focus = focus };
            if (focus?.relations == null) return snap;

            snap.parents.AddRange(DirectParents(focus));
            for (int i = 0; i < snap.parents.Count; i++)
            {
                foreach (Pawn gp in DirectParents(snap.parents[i]))
                {
                    if (!snap.grandparents.Contains(gp))
                        snap.grandparents.Add(gp);
                }
            }

            snap.partners.AddRange(Partners(focus));
            snap.siblings.AddRange(Siblings(focus, snap.parents));
            snap.children.AddRange(DirectChildren(focus));
            for (int i = 0; i < snap.children.Count; i++)
            {
                foreach (Pawn gc in DirectChildren(snap.children[i]))
                {
                    if (!snap.grandchildren.Contains(gc))
                        snap.grandchildren.Add(gc);
                }
            }

            var mentor = focus.TryGetComp<Comp_DeepColony>()?.mentor;
            if (mentor != null) snap.mentor = mentor;
            snap.apprentices.AddRange(Apprentices(focus));
            return snap;
        }

        public static bool HasAnyKin(FamilyTreeSnapshot snap)
        {
            if (snap == null) return false;
            return snap.grandparents.Count > 0
                || snap.parents.Count > 0
                || snap.siblings.Count > 0
                || snap.partners.Count > 0
                || snap.children.Count > 0
                || snap.grandchildren.Count > 0
                || snap.mentor != null
                || snap.apprentices.Count > 0;
        }

        public static void JumpTo(Pawn pawn)
        {
            if (pawn == null) return;
            if (pawn.Spawned)
            {
                CameraJumper.TryJumpAndSelect(pawn);
                return;
            }
            if (pawn.Corpse != null && pawn.Corpse.SpawnedOrAnyParentSpawned)
            {
                CameraJumper.TryJumpAndSelect(pawn.Corpse);
                return;
            }
            CameraJumper.TryJumpAndSelect(pawn);
        }

        public static string RelationLabel(Pawn focus, Pawn other)
        {
            if (focus == null || other == null) return "";
            if (focus == other) return "DC_FamilyTree_Self".Translate();
            PawnRelationDef def = focus.GetMostImportantRelation(other);
            if (def == null) return "";
            return def.GetGenderSpecificLabelCap(other);
        }

        public static List<Pawn> DirectParents(Pawn pawn)
        {
            var list = new List<Pawn>();
            if (pawn?.relations == null) return list;
            foreach (DirectPawnRelation rel in pawn.relations.DirectRelations)
            {
                if (rel.def != PawnRelationDefOf.Parent || rel.otherPawn == null) continue;
                if (!list.Contains(rel.otherPawn)) list.Add(rel.otherPawn);
            }
            return list;
        }

        public static List<Pawn> DirectChildren(Pawn pawn)
        {
            var list = new List<Pawn>();
            if (pawn?.relations == null) return list;
            foreach (Pawn other in pawn.relations.RelatedPawns)
            {
                if (other?.relations == null) continue;
                if (other.relations.DirectRelationExists(PawnRelationDefOf.Parent, pawn)
                    && !list.Contains(other))
                    list.Add(other);
            }
            return list;
        }

        private static List<Pawn> Partners(Pawn pawn)
        {
            var list = new List<Pawn>();
            if (pawn?.relations == null) return list;
            foreach (DirectPawnRelation rel in pawn.relations.DirectRelations)
            {
                if (rel.otherPawn == null) continue;
                if (rel.def == PawnRelationDefOf.Spouse
                    || rel.def == PawnRelationDefOf.Lover
                    || rel.def == PawnRelationDefOf.Fiance)
                {
                    if (!list.Contains(rel.otherPawn)) list.Add(rel.otherPawn);
                }
            }
            return list;
        }

        private static List<Pawn> Siblings(Pawn pawn, List<Pawn> parents)
        {
            var list = new List<Pawn>();
            if (pawn?.relations == null) return list;
            if (PawnRelationDefOf.Sibling != null)
            {
                foreach (Pawn other in pawn.relations.RelatedPawns)
                {
                    if (other == pawn || other == null) continue;
                    if (!PawnRelationDefOf.Sibling.Worker.InRelation(pawn, other)) continue;
                    if (!list.Contains(other)) list.Add(other);
                }
            }
            if (list.Count == 0 && parents != null)
            {
                for (int i = 0; i < parents.Count; i++)
                {
                    foreach (Pawn kid in DirectChildren(parents[i]))
                    {
                        if (kid == pawn) continue;
                        if (!list.Contains(kid)) list.Add(kid);
                    }
                }
            }
            return list;
        }

        private static List<Pawn> Apprentices(Pawn pawn)
        {
            var list = new List<Pawn>();
            if (pawn?.relations == null || DC_DefOf.DC_MentorOf == null) return list;
            foreach (DirectPawnRelation rel in pawn.relations.DirectRelations)
            {
                if (rel.def != DC_DefOf.DC_MentorOf || rel.otherPawn == null) continue;
                if (!list.Contains(rel.otherPawn)) list.Add(rel.otherPawn);
            }
            return list;
        }
    }

    public class FamilyTreeSnapshot
    {
        public Pawn focus;
        public List<Pawn> grandparents = new List<Pawn>();
        public List<Pawn> parents = new List<Pawn>();
        public List<Pawn> siblings = new List<Pawn>();
        public List<Pawn> partners = new List<Pawn>();
        public List<Pawn> children = new List<Pawn>();
        public List<Pawn> grandchildren = new List<Pawn>();
        public Pawn mentor;
        public List<Pawn> apprentices = new List<Pawn>();
    }
}
