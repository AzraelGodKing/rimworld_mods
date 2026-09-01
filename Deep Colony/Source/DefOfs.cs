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
        public static PawnRelationDef DC_Confidant;
        public static PawnRelationDef DC_Rival;

        // ── Jobs ─────────────────────────────────────────────────────────────────
        public static JobDef DC_Job_Mentor;
        public static JobDef DC_Job_CounselTrauma;
        public static JobDef DC_Job_GroupCounsel;
        public static JobDef DC_Job_CounselPrisoner;

        // ── Traumas ──────────────────────────────────────────────────────────────
        public static TraumaDef DC_Trauma_CombatShock;
        public static TraumaDef DC_Trauma_ViolentLoss;
        public static TraumaDef DC_Trauma_Captivity;
        public static TraumaDef DC_Trauma_Massacre;
        public static TraumaDef DC_Trauma_BereavementShock;
        public static TraumaDef DC_Trauma_Fire;
        public static TraumaDef DC_Trauma_Toxic;
        public static TraumaDef DC_Trauma_Insect;
        public static TraumaDef DC_Trauma_Betrayal;
        public static TraumaDef DC_Trauma_ToxicRelationship;
        [MayRequire("Ludeon.RimWorld.Anomaly")]
        public static TraumaDef DC_Trauma_Horror;
        [MayRequire("Ludeon.RimWorld.Odyssey")]
        public static TraumaDef DC_Trauma_Isolation;

        // ── Thoughts (non-trauma class) ───────────────────────────────────────────
        public static ThoughtDef DC_Thought_TraumaScar;
        public static ThoughtDef DC_Thought_Seasoned;
        public static ThoughtDef DC_Thought_Flashback;
        public static ThoughtDef DC_Thought_DayOfRemembrance;
        public static ThoughtDef DC_Thought_PerkReflection;
        public static ThoughtDef DC_Thought_Heirloom;
        public static ThoughtDef DC_Thought_GrewUpHere;
        public static ThoughtDef DC_Thought_ChildRaidWitness;
        public static ThoughtDef DC_Thought_GrandChefHomestead;
        public static ThoughtDef DC_Thought_FamilyMeal;
        public static ThoughtDef DC_Thought_ParentReunion;
        public static ThoughtDef DC_Thought_SpouseRemembrance;
        public static ThoughtDef DC_Thought_InLawWelcome;
        public static ThoughtDef DC_Thought_KinHomecoming;
        public static ThoughtDef DC_Thought_KinDiedOtherSide;
        public static ThoughtDef DC_Thought_BreakupWound;
        public static ThoughtDef DC_Thought_KinExecuted;
        public static ThoughtDef DC_Thought_GrandchildBorn;
        public static ThoughtDef DC_Thought_KinTaken;
        public static ThoughtDef DC_Thought_KinReturned;
        public static ThoughtDef DC_Thought_TendedByFamily;
        public static ThoughtDef DC_Thought_TendedFamily;
        public static ThoughtDef DC_Thought_LineContinues;
        public static ThoughtDef DC_Thought_StepFamily;
        public static ThoughtDef DC_Thought_FamilyPrisonVisit;
        public static ThoughtDef DC_Thought_VisitedKinPrisoner;
        public static ThoughtDef DC_Thought_KinReleased;
        public static ThoughtDef DC_Thought_TraditionTaught;
        public static ThoughtDef DC_Thought_KinDownedBeside;
        public static ThoughtDef DC_Thought_EmptyNest;
        public static ThoughtDef DC_Thought_ParentsDivorced;
        public static ThoughtDef DC_Thought_Inherited;
        public static ThoughtDef DC_Thought_Disinherited;
        public static ThoughtDef DC_Thought_MovedIntoDeadRoom;
        public static ThoughtDef DC_Thought_NoisyRoom;

        // ── Hediffs ───────────────────────────────────────────────────────────────
        public static HediffDef DC_Hediff_Elder;
        public static HediffDef DC_Hediff_Flashback;
        public static HediffDef DC_Hediff_CombatHabit;
        public static HediffDef DC_Hediff_TraumaDraftPenalty;
        public static HediffDef DC_Hediff_ChronicStress;
        public static HediffDef DC_Hediff_HeirloomEcho;

        static DC_DefOf() => DefOfHelper.EnsureInitializedInCtor(typeof(DC_DefOf));
    }
}
