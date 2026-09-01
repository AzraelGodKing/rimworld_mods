using HarmonyLib;
using RimWorld;
using Verse;

namespace DeepColony.Patches
{
    [HarmonyPatch(typeof(SkillRecord), nameof(SkillRecord.Learn))]
    public static class Patch_SkillRecord_Learn
    {
        private const float MentorProximityCells = 8f;

        public static void Prefix(SkillRecord __instance, ref float xp, bool direct, out int __state)
        {
            __state = __instance.levelInt;
            if (xp == 0f || direct) return;

            Pawn pawn = __instance.Pawn;
            if (pawn?.IsColonistPlayerControlled != true) return;

            var comp = pawn.TryGetComp<Comp_DeepColony>();
            if (comp == null) return;

            if (xp < 0f && DeepColonySettings.Get.enablePerks)
            {
                int tier = comp.HighestUnlockedPerkTierForSkill(__instance.def);
                if (tier >= 2)
                    xp = 0f;
                else if (tier >= 1)
                    xp *= 0.5f;
                return;
            }

            if (xp <= 0f) return;

            if (DeepColonySettings.Get.enablePerks)
            {
                int peak = comp.GetPeakSkill(__instance.def);
                if (peak > __instance.Level)
                    xp *= 2f;
            }

            // Professional rivalry competitive XP
            if (DeepColonySettings.Get.enableMentoring
                && RivalryUtility.HasRivalBoost(pawn, __instance.def))
            {
                xp *= 1.10f;
            }

            if (!DeepColonySettings.Get.enableMentoring) return;
            if (comp.mentor == null) return;

            // Skill-focus: only boost the mentored skill
            SkillDef focus = comp.GetMentoredSkill();
            if (focus != null && __instance.def != focus) return;

            Pawn mentor = comp.mentor;
            if (mentor.Dead || !mentor.Spawned || mentor.MapHeld != pawn.MapHeld) return;

            bool active = ActiveMentoringSession.IsBeingMentored(pawn);
            if (!active && mentor.Position.DistanceTo(pawn.Position) > MentorProximityCells)
                return;

            var settings = DeepColonySettings.Get;
            float mult = active ? settings.activeMentorMultiplier : settings.passiveMentorMultiplier;

            // Elders teach better
            if (ElderUtility.IsElder(mentor))
                mult *= 1.15f;

            // Biotech blackboard in room during active teaching
            if (active)
            {
                mult *= MentorshipUtility.ChalkboardRoomMultiplier(pawn);
                mult *= MentorshipUtility.ClassroomExtraMultiplier(pawn);
            }

            xp *= mult;
            xp *= QuietHoursUtility.MentorXpMultiplier(pawn);
        }

        public static void Postfix(SkillRecord __instance, float xp, bool direct, int __state)
        {
            if (direct) return;

            Pawn pawn = __instance.Pawn;
            if (pawn?.IsColonistPlayerControlled != true) return;

            var comp = pawn.TryGetComp<Comp_DeepColony>();
            if (comp == null) return;

            if (xp > 0f)
            {
                comp.RecordPeakSkill(__instance.def, __instance.Level);

                if (DeepColonySettings.Get.enableMentoring && comp.mentor != null)
                    MentorshipUtility.TryGraduate(comp.mentor, pawn);
            }

            if (__instance.levelInt == __state) return;
            if (!DeepColonySettings.Get.enablePerks) return;

            if (__instance.levelInt > __state)
            {
                int newLevel = __instance.levelInt;
                for (int level = __state + 1; level <= newLevel; level++)
                {
                    if (level == 5 || level == 10 || level == 15 || level == 20)
                        comp.NotifySkillLevelUp(__instance.def, level);
                }
                comp.RecordPeakSkill(__instance.def, newLevel);
            }
            else
            {
                comp.SyncPerksToSkillLevels(announce: true);
            }
        }
    }
}
