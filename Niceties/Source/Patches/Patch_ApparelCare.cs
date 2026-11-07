#nullable enable
using HarmonyLib;
using RimWorld;
using Verse;

namespace Niceties
{
    internal static class ApparelCare
    {
        internal static float DailyWearChance(Apparel gear, Pawn wearer)
        {
            NicetiesSimFlags settings = NicetiesSim.Settings;
            if (!settings.enableApparelCare)
            {
                return 1f;
            }

            QualityCategory? quality = gear.TryGetComp<CompQuality>()?.Quality;
            int crafting = -1;
            if (settings.apparelCraftingBonus && wearer.skills != null)
            {
                SkillRecord? record = wearer.skills.GetSkill(SkillDefOf.Crafting);
                if (record != null && !record.TotallyDisabled)
                {
                    crafting = record.Level;
                }
            }

            return NicetiesRules.DailyWearChance(settings.apparelQualityScaling, settings.protectCorpseApparel,
                (int)(quality ?? QualityCategory.Normal), crafting, wearer.Dead);
        }

        internal static string? InspectLine(Apparel gear, Pawn? wearer)
        {
            if (wearer == null || !NicetiesSim.Settings.enableApparelCare)
            {
                return null;
            }

            float chance = DailyWearChance(gear, wearer);
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

        internal static void AppendInspect(ref string result, string? line)
        {
            if (line == null || line.Length == 0)
            {
                return;
            }

            result = string.IsNullOrEmpty(result) ? line : result + "\n" + line;
        }
    }

    /// <summary>
    /// Worn apparel loses HP to the daily <c>Deterioration</c> damage from the apparel
    /// tracker. Equipped weapons never get that damage in vanilla, so 2.0 drops the
    /// well-kept weapons toggle, which did nothing (AZR-550).
    /// </summary>
    [HarmonyPatch(typeof(Thing), nameof(Thing.TakeDamage))]
    internal static class Patch_Thing_TakeDamage_Deterioration
    {
        [HarmonyPriority(Priority.High)]
        private static bool Prefix(Thing __instance, DamageInfo dinfo)
        {
            if (dinfo.Def != DamageDefOf.Deterioration || __instance is not Apparel apparel)
            {
                return true;
            }

            Pawn? wearer = apparel.Wearer;
            if (wearer == null || !NicetiesSim.Settings.enableApparelCare)
            {
                return true;
            }

            float chance = ApparelCare.DailyWearChance(apparel, wearer);
            if (chance <= 0f)
            {
                return false;
            }

            return chance >= 1f || Rand.Chance(chance);
        }
    }

    [HarmonyPatch(typeof(Apparel), nameof(Apparel.GetInspectString))]
    internal static class Patch_Apparel_GetInspectString
    {
        // The inspect pane asks every frame for the one selected thing; rebuild the line
        // about once a second instead (AZR-549).
        private const int CacheTicks = 60;
        private static int cachedThing = -1;
        private static int cachedTick = int.MinValue;
        private static LoadedLanguage? cachedLanguage;
        private static string? cachedLine;

        internal static void Invalidate()
        {
            cachedThing = -1;
        }

        private static void Postfix(Apparel __instance, ref string __result)
        {
            int tick = Find.TickManager?.TicksGame ?? 0;
            if (__instance.thingIDNumber != cachedThing || tick < cachedTick || tick - cachedTick >= CacheTicks
                || !ReferenceEquals(cachedLanguage, LanguageDatabase.activeLanguage))
            {
                cachedThing = __instance.thingIDNumber;
                cachedTick = tick;
                cachedLanguage = LanguageDatabase.activeLanguage;
                cachedLine = ApparelCare.InspectLine(__instance, __instance.Wearer);
            }

            ApparelCare.AppendInspect(ref __result, cachedLine);
        }
    }
}
