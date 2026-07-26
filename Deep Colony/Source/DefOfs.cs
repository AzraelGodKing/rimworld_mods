using RimWorld;
using Verse;

namespace DeepColony
{
    [DefOf]
    public static class DC_DefOf
    {
        // ── Relations ─────────────────────────────────────────────────────────────
        public static PawnRelationDef DC_MentorOf;
        public static PawnRelationDef DC_ApprenticeOf;

        // ── Jobs ─────────────────────────────────────────────────────────────────
        public static JobDef DC_Job_Mentor;

        public static JobDef DC_Job_CounselTrauma;

        // ── Traumas ──────────────────────────────────────────────────────────────
        public static TraumaDef DC_Trauma_CombatShock;
        public static TraumaDef DC_Trauma_ViolentLoss;
        public static TraumaDef DC_Trauma_Captivity;
        public static TraumaDef DC_Trauma_Massacre;
        public static TraumaDef DC_Trauma_BereavementShock;

        static DC_DefOf() => DefOfHelper.EnsureInitializedInCtor(typeof(DC_DefOf));
    }
}
