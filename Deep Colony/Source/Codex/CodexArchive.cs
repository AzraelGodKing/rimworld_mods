using System.Collections.Generic;
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

        public const int MinIntellectual = 8;
        public const float LostProgress = 400f;

        public static ResearchProjectDef ProjectAtRisk()
        {
            if (!DeepColonySettings.Get.enableCodex)
            {
                return null;
            }
            ResearchProjectDef proj = Find.ResearchManager?.GetProject();
            return proj == null || proj.IsFinished ? null : proj;
        }

        public static bool IsKeyResearcher(Pawn pawn)
        {
            SkillRecord intellectual = pawn?.skills?.GetSkill(SkillDefOf.Intellectual);
            return intellectual != null && !intellectual.TotallyDisabled && intellectual.Level >= MinIntellectual;
        }

        public static bool IsAtRisk(Pawn pawn)
        {
            return pawn?.Faction == Faction.OfPlayer && IsKeyResearcher(pawn) && !MapHasArchive(pawn.MapHeld);
        }

        public static void AtRiskResearchers(List<Pawn> into)
        {
            into.Clear();
            if (ProjectAtRisk() == null)
            {
                return;
            }
            foreach (Map map in Find.Maps)
            {
                if (!map.IsPlayerHome || MapHasArchive(map))
                {
                    continue;
                }
                foreach (Pawn p in map.mapPawns.FreeColonistsSpawned)
                {
                    if (IsKeyResearcher(p))
                    {
                        into.Add(p);
                    }
                }
            }
        }

        public static void LoseProgressOnDeath(Pawn pawn)
        {
            if (!IsAtRisk(pawn))
            {
                return;
            }
            ResearchProjectDef proj = ProjectAtRisk();
            if (proj == null)
            {
                return;
            }
            float lost = LostProgress;
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
            try
            {
                add.Invoke(Find.ResearchManager, args);
            }
            catch (System.Exception e)
            {
                Log.WarningOnce("[DeepColony] Codex research loss skipped (AddProgress call failed): "
                    + (e.InnerException ?? e).Message, 0x5DC0DE01);
                return;
            }
            Messages.Message("DC_Codex_TechDecay".Translate(pawn.LabelShortCap, proj.LabelCap),
                pawn, MessageTypeDefOf.NegativeEvent);
        }
    }

    [HarmonyPatch(typeof(Pawn), nameof(Pawn.Kill))]
    public static class Patch_Pawn_Kill_TechDecay
    {
        // A throw here would abort Pawn.Kill itself, not just the Codex feature.
        public static void Prefix(Pawn __instance)
        {
            try
            {
                CodexArchive.LoseProgressOnDeath(__instance);
            }
            catch (System.Exception e)
            {
                Log.WarningOnce("[DeepColony] Codex death hook failed: " + e.Message, 0x5DC0DE02);
            }
        }
    }
}
