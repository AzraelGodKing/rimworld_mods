using HarmonyLib;
using RimWorld;
using Verse;

namespace DeepColony.Patches
{
    /// <summary>
    /// Apprenticeship XP multiplier (Prefix) and perk-point grants at tree gate levels (Postfix).
    /// </summary>
    [HarmonyPatch(typeof(SkillRecord), nameof(SkillRecord.Learn))]
    public static class Patch_SkillRecord_Learn
    {
        // Passive near mentor / active teaching session
        private const float PassiveMentorMultiplier = 1.25f;
        private const float ActiveMentorMultiplier = 1.40f;
        private const float MentorProximityCells = 8f;

        public static void Prefix(SkillRecord __instance, ref float xp, bool direct,
            out int __state)
        {
            __state = __instance.levelInt;

            if (xp <= 0f || direct) return;

            Pawn pawn = __instance.Pawn;
            if (pawn?.IsColonistPlayerControlled != true) return;

            var comp = pawn.TryGetComp<Comp_DeepColony>();
            if (comp?.mentor == null) return;

            Pawn mentor = comp.mentor;
            if (mentor.Dead || !mentor.Spawned || mentor.MapHeld != pawn.MapHeld) return;

            bool active = ActiveMentoringSession.IsBeingMentored(pawn);
            if (!active && mentor.Position.DistanceTo(pawn.Position) > MentorProximityCells)
            {
                return;
            }

            xp *= active ? ActiveMentorMultiplier : PassiveMentorMultiplier;
        }

        public static void Postfix(SkillRecord __instance, int __state)
        {
            if (__instance.levelInt <= __state) return;

            Pawn pawn = __instance.Pawn;
            if (pawn?.IsColonistPlayerControlled != true) return;

            var comp = pawn.TryGetComp<Comp_DeepColony>();
            if (comp == null) return;

            // Points only at perk gate levels (matches the 2-node trees).
            int newLevel = __instance.levelInt;
            for (int level = __state + 1; level <= newLevel; level++)
            {
                if (level == 5 || level == 15)
                {
                    comp.NotifySkillLevelUp(__instance.def, level);
                }
            }
        }
    }
}
