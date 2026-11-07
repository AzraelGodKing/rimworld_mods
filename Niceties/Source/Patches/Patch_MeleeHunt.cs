#nullable enable
using HarmonyLib;
using RimWorld;
using Verse;
using Verse.AI;

namespace Niceties
{
    internal static class MeleeHunt
    {
        private static readonly string[] ReferenceAnimals =
        {
            "Squirrel", "Chicken", "Cat", "Turkey", "Husky", "Alpaca", "Deer", "Warg", "Pig", "Elk",
            "Muffalo", "Rhinoceros", "Elephant"
        };

        private static float cachedCap = -1f;
        private static string? cachedLabel;
        private static LoadedLanguage? cachedLanguage;

        /// <summary>Largest vanilla reference animal at or under the cap, or null if none fit.</summary>
        internal static string? ReferenceAnimalLabel(float cap)
        {
            // Keyed on the language too: def labels change when the player switches (AZR-550).
            if (cap == cachedCap && ReferenceEquals(cachedLanguage, LanguageDatabase.activeLanguage))
            {
                return cachedLabel;
            }

            ThingDef? best = null;
            for (int i = 0; i < ReferenceAnimals.Length; i++)
            {
                ThingDef? def = DefDatabase<ThingDef>.GetNamedSilentFail(ReferenceAnimals[i]);
                if (def?.race == null || def.race.baseBodySize > cap + 0.001f)
                {
                    continue;
                }

                if (best == null || def.race.baseBodySize > best.race.baseBodySize)
                {
                    best = def;
                }
            }

            cachedCap = cap;
            cachedLanguage = LanguageDatabase.activeLanguage;
            cachedLabel = best != null ? best.label + " (" + best.race.baseBodySize.ToString("0.##") + ")" : null;
            return cachedLabel;
        }

        internal static bool AllowsMelee(Pawn? hunter)
        {
            return hunter != null && NicetiesSim.Settings.meleeHunting;
        }

        internal static bool AllowsUnarmed()
        {
            NicetiesSimFlags settings = NicetiesSim.Settings;
            return settings.meleeHunting && settings.unarmedHunting;
        }

        internal static bool PreyFitsMelee(Pawn? prey)
        {
            return Refusal(prey) == null;
        }

        /// <summary>
        /// Why melee hunters skip this animal, as a translation key, or null if they may hunt it.
        /// Size uses the animal's real size, so a juvenile is judged by its own size (AZR-550).
        /// The revenge cap uses the chance the animal turns manhunter when hurt (AZR-568).
        /// </summary>
        internal static string? Refusal(Pawn? prey)
        {
            if (prey?.RaceProps == null)
            {
                return null;
            }

            NicetiesSimFlags settings = NicetiesSim.Settings;
            if (prey.BodySize > settings.meleeHuntMaxBodySize)
            {
                return "Niceties_Hunt_TooBig";
            }

            if (settings.meleeHuntMaxRevengeChance < 0.999f
                && PawnUtility.GetManhunterOnDamageChance(prey) > settings.meleeHuntMaxRevengeChance + 0.0001f)
            {
                return "Niceties_Hunt_TooDangerous";
            }

            return null;
        }

        internal static bool UsesMeleeForHunt(Pawn hunter)
        {
            if (!AllowsMelee(hunter))
            {
                return false;
            }

            ThingWithComps? primary = hunter.equipment?.Primary;
            if (primary == null)
            {
                return AllowsUnarmed();
            }

            return primary.def.IsMeleeWeapon;
        }
    }

    [HarmonyPatch(typeof(WorkGiver_HunterHunt), nameof(WorkGiver_HunterHunt.HasHuntingWeapon))]
    internal static class Patch_HasHuntingWeapon
    {
        private static void Postfix(Pawn p, ref bool __result)
        {
            if (__result || p == null)
            {
                return;
            }

            ThingWithComps? primary = p.equipment?.Primary;
            if (primary != null)
            {
                if (MeleeHunt.AllowsMelee(p) && primary.def.IsMeleeWeapon)
                {
                    CompEquippable? eq = primary.TryGetComp<CompEquippable>();
                    Verb? verb = eq?.PrimaryVerb;
                    if (verb != null && verb.HarmsHealth())
                    {
                        __result = true;
                    }
                }

                return;
            }

            if (MeleeHunt.AllowsUnarmed())
            {
                __result = true;
            }
        }
    }

    [HarmonyPatch(typeof(WorkGiver_HunterHunt), nameof(WorkGiver_Scanner.HasJobOnThing))]
    internal static class Patch_HunterHunt_HasJobOnThing
    {
        private static void Postfix(Pawn pawn, Thing t, ref bool __result)
        {
            if (!__result || pawn == null)
            {
                return;
            }

            if (!MeleeHunt.UsesMeleeForHunt(pawn))
            {
                return;
            }

            if (t is not Pawn prey)
            {
                return;
            }

            string? refusal = MeleeHunt.Refusal(prey);
            if (refusal != null)
            {
                __result = false;
                JobFailReason.Is(refusal.Translate(prey.LabelShort));
            }
        }
    }

    /// <summary>
    /// A hunt-designated animal that melee hunters will skip says so in its inspect pane,
    /// so a stalled hunt designation has a visible reason (AZR-568).
    /// </summary>
    [HarmonyPatch(typeof(Pawn), nameof(Pawn.GetInspectString))]
    internal static class Patch_Pawn_GetInspectString_MeleeHunt
    {
        private static void Postfix(Pawn __instance, ref string __result)
        {
            if (!__instance.Spawned || __instance.RaceProps == null || !__instance.RaceProps.Animal
                || !NicetiesSim.Settings.meleeHunting
                || __instance.Map.designationManager.DesignationOn(__instance, DesignationDefOf.Hunt) == null)
            {
                return;
            }

            string? refusal = MeleeHunt.Refusal(__instance);
            if (refusal != null)
            {
                ApparelCare.AppendInspect(ref __result, (refusal + "Inspect").Translate());
            }
        }
    }
}
