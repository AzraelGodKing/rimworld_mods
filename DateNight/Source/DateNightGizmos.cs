#nullable enable
using System.Collections.Generic;
using HarmonyLib;
using RimWorld;
using UnityEngine;
using Verse;

namespace DateNight
{
    /// <summary>
    /// Romance panel and "Ask out tonight" buttons on a selected adult colonist
    /// who has a lover or a ladder partner (AZR-524).
    /// </summary>
    [HarmonyPatch(typeof(Pawn), nameof(Pawn.GetGizmos))]
    public static class Patch_Pawn_GetGizmos_DateNight
    {
        public static IEnumerable<Gizmo> Postfix(IEnumerable<Gizmo> __result, Pawn __instance)
        {
            foreach (Gizmo g in __result)
            {
                yield return g;
            }

            Pawn pawn = __instance;
            if (!DateNightMod.Settings.showRomanceGizmos || !Eligible(pawn))
            {
                yield break;
            }

            Pawn? partner = DateNightRomance.FindCommittedPartner(pawn);
            yield return new Command_Action
            {
                defaultLabel = "DateNight_Gizmo_Romance".Translate(),
                defaultDesc = "DateNight_Gizmo_RomanceDesc".Translate(),
                icon = DateNightTex.Romance,
                action = () => Find.WindowStack.Add(new Dialog_DateNightRomance(pawn))
            };

            if (partner == null)
            {
                yield break;
            }

            if (ModsConfig.BiotechActive && LovePartnerRelationUtility.LovePartnerRelationExists(pawn, partner))
            {
                bool trying = DateNightBaby.IsTrying(pawn, partner);
                if (trying || DateNightBaby.CanConceive(pawn, partner))
                {
                    Pawn lover = partner;
                    yield return new Command_Toggle
                    {
                        defaultLabel = "DateNight_Gizmo_Baby".Translate(),
                        defaultDesc = "DateNight_Gizmo_BabyDesc".Translate(lover.LabelShort),
                        icon = DateNightTex.Baby,
                        isActive = () => trying,
                        toggleAction = () => DateNightBaby.SetTrying(pawn, lover, !trying)
                    };
                }
            }

            PlannedDate? plan = DateNightAskOut.PlanFor(pawn);
            if (plan != null)
            {
                yield return new Command_Action
                {
                    defaultLabel = "DateNight_Gizmo_AskOutCancel".Translate(),
                    defaultDesc = "DateNight_Gizmo_AskOutCancelDesc".Translate(partner.LabelShort, plan.hour),
                    icon = DateNightTex.AskOut,
                    action = () => DateNightAskOut.Cancel(pawn)
                };
                yield break;
            }

            var ask = new Command_Action
            {
                defaultLabel = "DateNight_Gizmo_AskOut".Translate(),
                defaultDesc = "DateNight_Gizmo_AskOutDesc".Translate(partner.LabelShort),
                icon = DateNightTex.AskOut,
                action = () => DateNightAskOut.Plan(pawn, partner)
            };
            if (!DateNightRomance.CanDateTogether(pawn, partner) || partner.Map != pawn.Map)
            {
                ask.Disable("DateNight_Gizmo_AskOutUnavailable".Translate(partner.LabelShort));
            }
            yield return ask;
        }

        private static bool Eligible(Pawn pawn)
        {
            return pawn.IsColonistPlayerControlled
                && pawn.RaceProps.Humanlike
                && pawn.ageTracker != null && pawn.ageTracker.Adult
                && pawn.DevelopmentalStage.Adult();
        }
    }

    [StaticConstructorOnStartup]
    public static class DateNightTex
    {
        // Vanilla heart mote; no extra texture to ship.
        public static readonly Texture2D Romance = ContentFinder<Texture2D>.Get("Things/Mote/Heart", reportFailure: false)
            ?? BaseContent.BadTex;
        public static readonly Texture2D Baby = ContentFinder<Texture2D>.Get("UI/Icons/Genes/Gene_Fertile", reportFailure: false)
            ?? Romance;
        public static readonly Texture2D AskOut = ContentFinder<Texture2D>.Get("UI/Commands/AsColonist", reportFailure: false)
            ?? Romance;
    }
}
