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
        private static string cachedLabel;

        /// <summary>Largest vanilla reference animal at or under the cap, or null if none fit.</summary>
        internal static string ReferenceAnimalLabel(float cap)
        {
            if (cap == cachedCap)
            {
                return cachedLabel;
            }

            ThingDef best = null;
            for (int i = 0; i < ReferenceAnimals.Length; i++)
            {
                ThingDef def = DefDatabase<ThingDef>.GetNamedSilentFail(ReferenceAnimals[i]);
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
            cachedLabel = best != null ? best.label + " (" + best.race.baseBodySize.ToString("0.##") + ")" : null;
            return cachedLabel;
        }

        internal static bool AllowsMelee(Pawn hunter)
        {
            NicetiesSettings settings = NicetiesSim.Settings;
            return settings != null && settings.meleeHunting && hunter != null;
        }

        internal static bool AllowsUnarmed()
        {
            NicetiesSettings settings = NicetiesSim.Settings;
            return settings != null && settings.meleeHunting && settings.unarmedHunting;
        }

        internal static bool PreyFitsMelee(Pawn prey)
        {
            if (prey?.RaceProps == null)
            {
                return true;
            }

            NicetiesSettings settings = NicetiesSim.Settings;
            float cap = settings != null ? settings.meleeHuntMaxBodySize : 1.5f;
            return prey.RaceProps.baseBodySize <= cap;
        }

        internal static bool UsesMeleeForHunt(Pawn hunter)
        {
            if (!AllowsMelee(hunter))
            {
                return false;
            }

            ThingWithComps primary = hunter.equipment?.Primary;
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

            ThingWithComps primary = p.equipment?.Primary;
            if (primary != null)
            {
                if (MeleeHunt.AllowsMelee(p) && primary.def.IsMeleeWeapon)
                {
                    CompEquippable eq = primary.TryGetComp<CompEquippable>();
                    Verb verb = eq?.PrimaryVerb;
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

            Pawn prey = t as Pawn;
            if (prey != null && !MeleeHunt.PreyFitsMelee(prey))
            {
                __result = false;
                JobFailReason.Is("Niceties_Hunt_TooBig".Translate(prey.LabelShort));
            }
        }
    }
}
