using HarmonyLib;
using RimWorld;
using Verse;
using Verse.AI;

namespace Niceties
{
    internal static class MeleeHunt
    {
        internal static bool AllowsMelee(Pawn hunter)
        {
            return NicetiesMod.Settings != null && NicetiesMod.Settings.meleeHunting && hunter != null;
        }

        internal static bool AllowsUnarmed()
        {
            NicetiesSettings settings = NicetiesMod.Settings;
            return settings != null && settings.meleeHunting && settings.unarmedHunting;
        }

        internal static bool PreyFitsMelee(Pawn prey)
        {
            if (prey?.RaceProps == null)
            {
                return true;
            }

            float cap = NicetiesMod.Settings != null ? NicetiesMod.Settings.meleeHuntMaxBodySize : 1.5f;
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
