using System.Collections.Generic;
using RimWorld;
using Verse;

namespace DeepColony
{
    /// <summary>
    /// D18 — ex-lovers (and ex-spouses) may get back together. The fourth reunion
    /// marks a toxic relationship that counseling / therapy can ease. Date Night
    /// still owns romance schedules; this only writes vanilla Lover + trauma.
    /// </summary>
    public static class ExLoverReconcileUtility
    {
        public const int ToxicAfterCount = 3;
        private const int CheckInterval = 2500;

        public static void GameTick()
        {
            if (!DeepColonySettings.Get.enableExLoverReconcile) return;

            float mtb = DeepColonySettings.Get.exLoverReconcileMtbDays;
            if (mtb <= 0f) return;

            foreach (Map map in Find.Maps)
            {
                if (map?.IsPlayerHome != true) continue;
                if (map.mapPawns?.FreeColonistsSpawned == null) continue;
                foreach (Pawn colonist in map.mapPawns.FreeColonistsSpawned)
                {
                    if (!CanReconcilePawn(colonist)) continue;
                    TryReconcileFrom(colonist, mtb);
                }
            }
        }

        public static bool TryForceReconcile(Pawn pawn)
        {
            if (!CanReconcilePawn(pawn) || pawn.Map == null) return false;
            Pawn ex = FindExOnMap(pawn);
            if (ex == null || !CanReconcilePawn(ex)) return false;
            Reconcile(pawn, ex);
            return true;
        }

        private static void TryReconcileFrom(Pawn colonist, float mtbDays)
        {
            if (colonist.relations == null) return;
            foreach (DirectPawnRelation rel in colonist.relations.DirectRelations)
            {
                if (!IsExRelation(rel.def)) continue;
                Pawn other = rel.otherPawn;
                if (!CanReconcilePawn(other)) continue;
                if (other.MapHeld != colonist.Map) continue;
                if (colonist.thingIDNumber > other.thingIDNumber
                    && other.IsColonistPlayerControlled)
                    continue;
                if (HasCurrentPartner(colonist) || HasCurrentPartner(other))
                    continue;
                if (!Rand.MTBEventOccurs(mtbDays, 60000f, CheckInterval))
                    continue;
                Reconcile(colonist, other);
                return;
            }
        }

        private static void Reconcile(Pawn a, Pawn b)
        {
            ClearExRelations(a, b);
            a.relations.AddDirectRelation(PawnRelationDefOf.Lover, b);

            int countA = NoteReconcile(a, b);
            int countB = NoteReconcile(b, a);
            int count = System.Math.Max(countA, countB);
            bool toxic = count > ToxicAfterCount;

            if (toxic && DeepColonySettings.Get.enableTrauma)
            {
                TraumaDef def = DC_DefOf.DC_Trauma_ToxicRelationship;
                if (def != null)
                {
                    TraumaUtility.ApplyTrauma(a, def, b);
                    TraumaUtility.ApplyTrauma(b, def, a);
                }
                Find.LetterStack.ReceiveLetter(
                    "DC_Letter_ToxicRelationshipLabel".Translate(),
                "DC_Letter_ToxicRelationshipBody".Translate(
                    a.LabelShort.Named("A"),
                    b.LabelShort.Named("B"),
                    count.Named("COUNT")),
                    LetterDefOf.NegativeEvent,
                    new LookTargets(a, b));
                return;
            }

            Find.LetterStack.ReceiveLetter(
                "DC_Letter_ReconcileLabel".Translate(),
                "DC_Letter_ReconcileBody".Translate(
                    a.LabelShort.Named("A"),
                    b.LabelShort.Named("B"),
                    count.Named("COUNT")),
                LetterDefOf.PositiveEvent,
                new LookTargets(a, b));
        }

        public static int NoteReconcile(Pawn self, Pawn other)
        {
            if (self == null || other == null) return 0;
            var comp = self.TryGetComp<Comp_DeepColony>();
            if (comp == null) return 0;
            if (comp.reconcileCountsByPawn == null)
                comp.reconcileCountsByPawn = new Dictionary<int, int>();
            int id = other.thingIDNumber;
            comp.reconcileCountsByPawn.TryGetValue(id, out int count);
            count++;
            comp.reconcileCountsByPawn[id] = count;
            return count;
        }

        private static void ClearExRelations(Pawn a, Pawn b)
        {
            RemoveIfPresent(a, b, PawnRelationDefOf.ExLover);
            RemoveIfPresent(b, a, PawnRelationDefOf.ExLover);
            RemoveIfPresent(a, b, PawnRelationDefOf.ExSpouse);
            RemoveIfPresent(b, a, PawnRelationDefOf.ExSpouse);
        }

        private static void RemoveIfPresent(Pawn a, Pawn b, PawnRelationDef def)
        {
            if (a?.relations == null || b == null || def == null) return;
            if (a.relations.DirectRelationExists(def, b))
                a.relations.RemoveDirectRelation(def, b);
        }

        private static bool IsExRelation(PawnRelationDef def)
        {
            if (def == null) return false;
            if (def == PawnRelationDefOf.ExLover) return true;
            if (PawnRelationDefOf.ExSpouse != null && def == PawnRelationDefOf.ExSpouse)
                return true;
            return false;
        }

        private static bool HasCurrentPartner(Pawn pawn)
        {
            if (pawn?.relations == null) return false;
            return LovePartnerRelationUtility.ExistingMostLikedLovePartner(pawn, allowDead: false) != null;
        }

        private static bool CanReconcilePawn(Pawn pawn)
        {
            if (pawn == null || pawn.Dead || !pawn.Spawned) return false;
            if (pawn.RaceProps == null || !pawn.RaceProps.Humanlike) return false;
            if (pawn.relations == null) return false;
            if (pawn.DevelopmentalStage < DevelopmentalStage.Adult) return false;
            return true;
        }

        private static Pawn FindExOnMap(Pawn pawn)
        {
            if (pawn.relations == null || pawn.Map == null) return null;
            foreach (DirectPawnRelation rel in pawn.relations.DirectRelations)
            {
                if (!IsExRelation(rel.def)) continue;
                Pawn other = rel.otherPawn;
                if (other == null || other.Dead) continue;
                if (other.MapHeld != pawn.Map) continue;
                return other;
            }
            return null;
        }
    }
}
