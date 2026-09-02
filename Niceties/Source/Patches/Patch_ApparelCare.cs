using HarmonyLib;
using RimWorld;
using Verse;

namespace Niceties
{
    internal static class ApparelCare
    {
        internal static float DailyWearChance(Apparel apparel, Pawn wearer)
        {
            NicetiesSettings settings = NicetiesMod.Settings;
            if (settings == null || !settings.enableApparelCare)
            {
                return 1f;
            }

            if (wearer == null)
            {
                return 1f;
            }

            if (wearer.Dead && !settings.protectCorpseApparel)
            {
                return 1f;
            }

            if (!settings.apparelQualityScaling)
            {
                return 0f;
            }

            float chance = ChanceForQuality(apparel);
            if (settings.apparelCraftingBonus && wearer.skills != null)
            {
                SkillRecord crafting = wearer.skills.GetSkill(SkillDefOf.Crafting);
                if (crafting != null && !crafting.TotallyDisabled)
                {
                    chance *= 1f - (crafting.Level / 20f) * 0.5f;
                }
            }

            return chance < 0f ? 0f : (chance > 1f ? 1f : chance);
        }

        private static float ChanceForQuality(Apparel apparel)
        {
            CompQuality quality = apparel.TryGetComp<CompQuality>();
            QualityCategory q = quality != null ? quality.Quality : QualityCategory.Normal;
            switch (q)
            {
                case QualityCategory.Awful:
                    return 1f;
                case QualityCategory.Poor:
                    return 0.8f;
                case QualityCategory.Normal:
                    return 0.45f;
                case QualityCategory.Good:
                    return 0.2f;
                case QualityCategory.Excellent:
                    return 0.08f;
                case QualityCategory.Masterwork:
                    return 0f;
                case QualityCategory.Legendary:
                    return 0f;
                default:
                    return 0.45f;
            }
        }

        internal static string InspectLine(Apparel apparel)
        {
            if (NicetiesMod.Settings == null || !NicetiesMod.Settings.enableApparelCare)
            {
                return null;
            }

            Pawn wearer = apparel.Wearer;
            if (wearer == null)
            {
                return null;
            }

            float chance = DailyWearChance(apparel, wearer);
            if (chance <= 0.001f)
            {
                return "Niceties_Apparel_NoWear".Translate();
            }

            if (chance >= 0.999f)
            {
                return "Niceties_Apparel_VanillaWear".Translate();
            }

            return "Niceties_Apparel_ReducedWear".Translate(chance.ToStringPercent());
        }
    }

    [HarmonyPatch(typeof(Thing), nameof(Thing.TakeDamage))]
    internal static class Patch_Thing_TakeDamage_Deterioration
    {
        [HarmonyPriority(Priority.High)]
        private static bool Prefix(Thing __instance, DamageInfo dinfo)
        {
            if (dinfo.Def != DamageDefOf.Deterioration)
            {
                return true;
            }

            Apparel apparel = __instance as Apparel;
            if (apparel == null)
            {
                return true;
            }

            Pawn wearer = apparel.Wearer;
            if (wearer == null)
            {
                return true;
            }

            float chance = ApparelCare.DailyWearChance(apparel, wearer);
            if (chance <= 0f)
            {
                return false;
            }

            if (chance >= 1f)
            {
                return true;
            }

            return Rand.Chance(chance);
        }
    }

    [HarmonyPatch(typeof(Apparel), nameof(Apparel.GetInspectString))]
    internal static class Patch_Apparel_GetInspectString
    {
        private static void Postfix(Apparel __instance, ref string __result)
        {
            string line = ApparelCare.InspectLine(__instance);
            if (line.NullOrEmpty())
            {
                return;
            }

            if (__result.NullOrEmpty())
            {
                __result = line;
            }
            else
            {
                __result = __result + "\n" + line;
            }
        }
    }
}
