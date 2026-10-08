using HarmonyLib;
using RimWorld;
using Verse;

namespace Niceties
{
    internal static class ApparelCare
    {
        internal static float DailyWearChance(ThingWithComps gear, Pawn wearer)
        {
            NicetiesSettings settings = NicetiesSim.Settings;
            if (settings == null || !settings.enableApparelCare || gear == null)
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

            float chance = ChanceForQuality(gear);
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

        private static float ChanceForQuality(ThingWithComps gear)
        {
            CompQuality quality = gear.TryGetComp<CompQuality>();
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

        internal static Pawn EquipmentOwner(Thing thing)
        {
            if (thing?.ParentHolder is Pawn_EquipmentTracker tracker)
            {
                return tracker.pawn;
            }

            return null;
        }

        internal static string InspectLine(ThingWithComps gear, Pawn wearer, bool weapon)
        {
            NicetiesSettings settings = NicetiesSim.Settings;
            if (settings == null || !settings.enableApparelCare)
            {
                return null;
            }

            if (weapon && !settings.enableWeaponCare)
            {
                return null;
            }

            if (wearer == null)
            {
                return null;
            }

            float chance = DailyWearChance(gear, wearer);
            if (chance <= 0.001f)
            {
                return weapon
                    ? "Niceties_Weapon_NoWear".Translate()
                    : "Niceties_Apparel_NoWear".Translate();
            }

            if (chance >= 0.999f)
            {
                return weapon
                    ? "Niceties_Weapon_VanillaWear".Translate()
                    : "Niceties_Apparel_VanillaWear".Translate();
            }

            return weapon
                ? "Niceties_Weapon_ReducedWear".Translate(chance.ToStringPercent())
                : "Niceties_Apparel_ReducedWear".Translate(chance.ToStringPercent());
        }

        internal static void AppendInspect(ref string result, string line)
        {
            if (line.NullOrEmpty())
            {
                return;
            }

            if (result.NullOrEmpty())
            {
                result = line;
            }
            else
            {
                result = result + "\n" + line;
            }
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

            NicetiesSettings settings = NicetiesSim.Settings;
            if (settings == null || !settings.enableApparelCare)
            {
                return true;
            }

            Apparel apparel = __instance as Apparel;
            if (apparel != null)
            {
                Pawn wearer = apparel.Wearer;
                if (wearer == null)
                {
                    return true;
                }

                return RollWear(apparel, wearer);
            }

            if (!settings.enableWeaponCare)
            {
                return true;
            }

            ThingWithComps gear = __instance as ThingWithComps;
            if (gear == null || gear.def == null || !gear.def.IsWeapon)
            {
                return true;
            }

            Pawn owner = ApparelCare.EquipmentOwner(gear);
            if (owner == null)
            {
                return true;
            }

            return RollWear(gear, owner);
        }

        private static bool RollWear(ThingWithComps gear, Pawn wearer)
        {
            float chance = ApparelCare.DailyWearChance(gear, wearer);
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
            string line = ApparelCare.InspectLine(__instance, __instance.Wearer, weapon: false);
            ApparelCare.AppendInspect(ref __result, line);
        }
    }

    [HarmonyPatch(typeof(ThingWithComps), nameof(ThingWithComps.GetInspectString))]
    internal static class Patch_Weapon_GetInspectString
    {
        private static void Postfix(ThingWithComps __instance, ref string __result)
        {
            if (__instance is Apparel || __instance.def == null || !__instance.def.IsWeapon)
            {
                return;
            }

            Pawn owner = ApparelCare.EquipmentOwner(__instance);
            string line = ApparelCare.InspectLine(__instance, owner, weapon: true);
            ApparelCare.AppendInspect(ref __result, line);
        }
    }
}
