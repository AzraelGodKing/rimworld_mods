using Verse;

namespace DeepColony
{
    /// <summary>C03 — Ideology counseling precepts. Fail-open without Ideology.</summary>
    public static class IdeologyCounselUtility
    {
        public const string SacredDef = "DC_Precept_CounselingSacred";
        public const string StoicDef = "DC_Precept_CounselingStoic";

        public static bool CounselingIsSacred => SoftCompat.PlayerIdeoHasPrecept(SacredDef);
        public static bool CounselingIsStoic => SoftCompat.PlayerIdeoHasPrecept(StoicDef);

        public static float TherapyMultiplier()
        {
            if (CounselingIsSacred) return 1.25f;
            return 1f;
        }

        public static float NaturalFadeMultiplier()
        {
            if (CounselingIsStoic) return 0.65f;
            return 1f;
        }

        public static bool AutoCounselBlocked => CounselingIsStoic;
    }
}
