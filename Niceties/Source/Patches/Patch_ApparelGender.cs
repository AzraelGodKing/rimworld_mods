#nullable enable
using System;
using HarmonyLib;
using RimWorld;
using Verse;

namespace Niceties
{
    /// <summary>
    /// Wear any gendered cut (AZR-550). 1.x cleared <c>apparel.gender</c> on the shared defs,
    /// which also put raiders and traders in cross-gender cuts and had to run before starting
    /// pawns were generated (AZR-653). 2.0 leaves the defs alone: the gender check answers
    /// "fits" for colonists choosing clothes, and the wrong-gender thought is switched off.
    /// Pawn generation keeps the vanilla answer unless "also generated pawns" is on.
    /// </summary>
    internal static class ApparelGender
    {
        [ThreadStatic]
        private static int generating;

        internal static bool Generating => generating > 0;

        internal static void BeginGenerate() => generating++;

        internal static void EndGenerate()
        {
            if (generating > 0)
            {
                generating--;
            }
        }

        /// <summary>True when the gender check should pass for any wearer right now.</summary>
        internal static bool AnyCutFits()
        {
            NicetiesSimFlags s = NicetiesSim.Settings;
            if (!s.wearAnyGender)
            {
                return false;
            }

            return !Generating || s.wearAnyGenderGenerated;
        }
    }

    [HarmonyPatch(typeof(ApparelProperties), nameof(ApparelProperties.CorrectGenderForWearing))]
    internal static class Patch_CorrectGenderForWearing
    {
        private static void Postfix(ref bool __result)
        {
            if (!__result && ApparelGender.AnyCutFits())
            {
                __result = true;
            }
        }
    }

    [HarmonyPatch(typeof(ThoughtWorker_WrongApparelGender), "CurrentStateInternal")]
    internal static class Patch_WrongApparelGender
    {
        private static void Postfix(ref ThoughtState __result)
        {
            if (__result.Active && NicetiesSim.Settings.wearAnyGender)
            {
                __result = ThoughtState.Inactive;
            }
        }
    }

    [HarmonyPatch(typeof(PawnGenerator), nameof(PawnGenerator.GeneratePawn), new[] { typeof(PawnGenerationRequest) })]
    internal static class Patch_GeneratePawn_GenderScope
    {
        private static void Prefix()
        {
            ApparelGender.BeginGenerate();
        }

        private static Exception? Finalizer(Exception? __exception)
        {
            ApparelGender.EndGenerate();
            return __exception;
        }
    }
}
