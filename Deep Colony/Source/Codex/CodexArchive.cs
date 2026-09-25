using System.Reflection;
using HarmonyLib;
using RimWorld;
using Verse;

namespace DeepColony
{
    // When convalescence wears off it can leave lingering weakness.
    // Infirmary recoveries roll that less often.
    public class HediffCompProperties_LeaveChronic : HediffCompProperties
    {
        public HediffCompProperties_LeaveChronic()
        {
            compClass = typeof(HediffComp_LeaveChronic);
        }
    }

    public class HediffComp_LeaveChronic : HediffComp
    {
        public override void CompPostPostRemoved()
        {
            if (!DeepColonySettings.Get.enableBody || Pawn?.health == null)
            {
                return;
            }
            HediffDef chronic = DefDatabase<HediffDef>.GetNamedSilentFail("DC_Hediff_LingeringWeakness");
            if (chronic == null || Pawn.health.hediffSet.GetFirstHediffOfDef(chronic) != null)
            {
                return;
            }
            float chance = BodyRooms.IsInfirmary(Pawn.GetRoom()) ? 0.12f : 0.35f;
            if (!Rand.Chance(chance))
            {
                return;
            }
            Pawn.health.AddHediff(chronic);
            Messages.Message("DC_Body_Chronic".Translate(Pawn.LabelShortCap), Pawn, MessageTypeDefOf.NeutralEvent);
        }
    }

    public static class CodexArchive
    {
        // Researcher died with Codex on and no archive on the map → the
        // current project slips. An archive still standing means the notes
        // survived. Do not touch progress if Codex is off.
        public static bool MapHasArchive(Map map)
        {
            if (map == null)
            {
                return false;
            }
            ThingDef def = DefDatabase<ThingDef>.GetNamedSilentFail("DC_Archive");
            if (def == null)
            {
                return false;
            }
            return map.listerBuildings.ColonistsHaveBuilding(def);
        }

        public static void LoseProgressOnDeath(Pawn pawn)
        {
            if (!DeepColonySettings.Get.enableCodex || pawn?.Faction != Faction.OfPlayer)
            {
                return;
            }
            if (pawn.skills?.GetSkill(SkillDefOf.Intellectual) == null
                || pawn.skills.GetSkill(SkillDefOf.Intellectual).Level < 8)
            {
                return;
            }
            if (MapHasArchive(pawn.MapHeld))
            {
                return;
            }
            ResearchProjectDef proj = Find.ResearchManager.GetProject();
            if (proj == null || proj.IsFinished)
            {
                return;
            }
            float lost = 400f;
            MethodInfo add = AccessTools.Method(typeof(ResearchManager), "AddProgress");
            if (add == null)
            {
                return;
            }
            ParameterInfo[] ps = add.GetParameters();
            object[] args;
            if (ps.Length == 2)
            {
                args = new object[] { proj, -lost };
            }
            else if (ps.Length >= 3)
            {
                args = new object[ps.Length];
                args[0] = proj;
                args[1] = -lost;
                for (int i = 2; i < ps.Length; i++)
                {
                    args[i] = ps[i].HasDefaultValue ? ps[i].DefaultValue : (ps[i].ParameterType.IsValueType
                        ? System.Activator.CreateInstance(ps[i].ParameterType)
                        : null);
                }
            }
            else
            {
                return;
            }
            add.Invoke(Find.ResearchManager, args);
            Messages.Message("DC_Codex_TechDecay".Translate(pawn.LabelShortCap, proj.LabelCap),
                pawn, MessageTypeDefOf.NegativeEvent);
        }
    }

    [HarmonyPatch(typeof(Pawn), nameof(Pawn.Kill))]
    public static class Patch_Pawn_Kill_TechDecay
    {
        public static void Prefix(Pawn __instance)
        {
            CodexArchive.LoseProgressOnDeath(__instance);
        }
    }
}
