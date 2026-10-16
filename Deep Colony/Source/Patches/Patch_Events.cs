using System;
using HarmonyLib;
using RimWorld;
using Verse;
using Verse.AI.Group;

namespace DeepColony.Patches
{
    [HarmonyPatch(typeof(IncidentWorker_RaidEnemy), "TryExecuteWorker")]
    public static class Patch_RaidEnemy_TryExecute
    {
        public static void Prefix(IncidentParms parms)
        {
            if (!FactionAttitudeUtility.ConsequencesActive) return;
            if (parms?.faction == null) return;
            float factor = FactionAttitudeUtility.RaidPointsFactor(parms.faction);
            if (Math.Abs(factor - 1f) < 0.001f) return;
            parms.points *= factor;
        }

        public static void Postfix(IncidentParms parms, bool __result)
        {
            if (!__result || parms?.faction == null) return;
            if (parms.target is not Map map) return;
            if (map.ParentFaction != Faction.OfPlayer) return;
            if (__result && parms?.target is Map raidMap)
                ChildRaidUtility.NotifyRaidStarted(raidMap);
            FactionRepUtility.OnRaidFromFaction(parms.faction);
        }
    }

    [HarmonyPatch(typeof(IncidentWorker_TraderCaravanArrival), "TryExecuteWorker")]
    public static class Patch_TraderCaravan_TryExecute
    {
        public static void Postfix(IncidentParms parms, bool __result)
        {
            if (!__result || parms?.faction == null) return;
            FactionRepUtility.OnTradeCaravanFromFaction(parms.faction);
        }
    }

    [HarmonyPatch(typeof(TradeDeal), nameof(TradeDeal.TryExecute))]
    public static class Patch_TradeDeal_TryExecute
    {
        public static void Postfix(bool __result, ref bool actuallyTraded)
        {
            if (!__result || !actuallyTraded) return;
            Faction faction = TradeSession.trader?.Faction;
            if (faction != null) FactionRepUtility.OnSuccessfulTrade(faction);
        }
    }

    [HarmonyPatch(typeof(VisitorGiftForPlayerUtility), nameof(VisitorGiftForPlayerUtility.GiveGift),
        typeof(System.Collections.Generic.List<Pawn>), typeof(Faction),
        typeof(System.Collections.Generic.List<Thing>))]
    public static class Patch_VisitorGift_GiveGift
    {
        public static void Postfix(Faction faction)
        {
            FactionRepUtility.OnGiftFromFaction(faction);
        }
    }

    [HarmonyPatch(typeof(Pawn_HealthTracker), "MakeDowned")]
    public static class Patch_MakeDowned
    {
        public static void Postfix(Pawn_HealthTracker __instance, DamageInfo? dinfo)
        {
            Pawn victim = TrackerFields.HealthPawn(__instance);

            if (DeepColonySettings.Get.enableTrauma && dinfo.HasValue && victim != null)
            {
                DamageInfo flavorInfo = dinfo.Value;
                ChildRaidUtility.NotifyDowned(victim, flavorInfo);
                DisasterFlavorUtility.NotifyDowned(victim, flavorInfo);
                AnomalyOdysseyTraumaUtility.NotifyDowned(victim, flavorInfo);
            }

            if (DeepColonySettings.Get.enableTrauma
                && victim != null
                && victim.IsColonistPlayerControlled
                && dinfo.HasValue)
            {
                DamageInfo info = dinfo.Value;
                if (info.Def != null && !info.Def.isExplosive)
                {
                    Thing instigator = info.Instigator;
                    // Fire / toxic can down without a hostile pawn instigator.
                    bool environmental = info.Def == DamageDefOf.Flame
                        || (info.Def.defName != null && info.Def.defName.IndexOf("Burn", StringComparison.OrdinalIgnoreCase) >= 0)
                        || victim.IsBurning();
                    if (environmental
                        || (instigator != null && instigator.HostileTo(victim)))
                    {
                        TraumaEventUtility.TrySpecialtyOrCombatShock(victim, info);
                    }
                }
            }

            FamilyEchoUtility.NotifyKinDowned(victim, dinfo);
        }
    }

    [HarmonyPatch(typeof(Pawn), nameof(Pawn.Kill))]
    public static class Patch_Pawn_Kill
    {
        public static void Prefix(Pawn __instance)
        {
            if (__instance != null && __instance.RaceProps.Humanlike)
                DeathbedUtility.NotifyMentorDying(__instance);

            try
            {
                CodexArchive.LoseProgressOnDeath(__instance);
            }
            catch (Exception e)
            {
                Log.WarningOnce("[DeepColony] Codex death hook failed: " + e.Message, 0x5DC0DE02);
            }
        }

        public static void Postfix(Pawn __instance, DamageInfo? dinfo, Hediff exactCulprit)
        {
            if (__instance != null && __instance.RaceProps.Humanlike)
            {
                MentorshipUtility.ClearAllLinksInvolving(__instance);
                FactionEnvoyUtility.NotifyPawnDied(__instance);

                bool wasColonist = __instance.Faction == Faction.OfPlayer || __instance.IsColonist;
                if (wasColonist)
                {
                    HeirloomUtility.TryCreateFromDeath(__instance);
                    EstateUtility.NotifyDeath(__instance);
                    GameComp_DeepColony.Instance?.NotifyColonistDied(__instance, dinfo, exactCulprit);
                }

                // Shared-enemy goodwill crumbs when the player kills hostiles.
                if (__instance.Faction != null
                    && !__instance.Faction.IsPlayer
                    && __instance.HostileTo(Faction.OfPlayer)
                    && dinfo.HasValue
                    && dinfo.Value.Instigator is Pawn killer
                    && killer.Faction == Faction.OfPlayer)
                {
                    FactionRepUtility.OnPlayerKilledHostile(__instance.Faction);
                }

                Map map = __instance.MapHeld;
                if (map != null)
                {
                    foreach (Pawn colonist in map.mapPawns.FreeColonistsSpawned)
                    {
                        if (colonist.Dead || colonist == __instance) continue;
                        if (!colonist.IsColonistPlayerControlled) continue;

                        int opinion = colonist.relations?.OpinionOf(__instance) ?? 0;
                        if (opinion < 40) continue;

                        bool violent = !dinfo.HasValue
                            || dinfo.Value.Def == null
                            || dinfo.Value.Def.ExternalViolenceFor(__instance);
                        if (!violent) continue;

                        TraumaDef loss = DC_DefOf.DC_Trauma_ViolentLoss;
                        if (loss == null) continue;

                        Faction killerFaction = dinfo.HasValue ? dinfo.Value.Instigator?.Faction : null;

                        TraumaDef bereave = DC_DefOf.DC_Trauma_BereavementShock;
                        if (TraumaUtility.HasTrauma(colonist, loss)
                            || TraumaUtility.HasTrauma(colonist, bereave))
                        {
                            // Upgrade: replace Violent Loss with Bereavement (do not stack).
                            if (bereave != null)
                            {
                                TraumaUtility.RemoveTrauma(colonist, loss);
                                TraumaUtility.ApplyTrauma(colonist, bereave, __instance, killerFaction);
                            }
                        }
                        else
                        {
                            TraumaUtility.ApplyTrauma(colonist, loss, __instance, killerFaction);
                        }
                    }
                }
            }

            FamilyBeatsUtility.NotifyDied(__instance);

            if (__instance == null || !__instance.RaceProps.Humanlike) return;
            FamilyLifeUtility.RefreshAllLastOfTheLine(announceLast: true, announceContinue: false);
        }
    }

    public struct SetFactionPatchState
    {
        public bool suppressGoodwill;
        public bool captivityTrauma;
    }

    [HarmonyPatch(typeof(Pawn), nameof(Pawn.SetFaction))]
    public static class Patch_Pawn_SetFaction
    {
        public static void Prefix(Pawn __instance, Faction newFaction, out SetFactionPatchState __state)
        {
            __state = default;
            __state.suppressGoodwill = FamilyJoinUtility.ShouldSuppressGoodwillForJoin(__instance, newFaction);
            if (__state.suppressGoodwill) FamilyJoinUtility.EnterSuppress();

            Faction old = __instance.Faction;

            if (old != null && old.IsPlayer && (newFaction == null || !newFaction.IsPlayer))
            {
                GameComp_DeepColony.Instance?.MarkFormerPlayerColonist(__instance);
            }

            // Prisoner of a non-player host, or former colonist returning from enemy hands.
            bool prisonerCaptivity = __instance.IsPrisoner
                && __instance.HostFaction != null
                && !__instance.HostFaction.IsPlayer;
            bool rescuedFormer = newFaction != null && newFaction.IsPlayer
                && old != null && !old.IsPlayer
                && (GameComp_DeepColony.Instance?.WasEverPlayerColonist(__instance) ?? false);

            __state.captivityTrauma = prisonerCaptivity || rescuedFormer;
        }

        public static void Postfix(Pawn __instance, Faction newFaction, SetFactionPatchState __state)
        {
            if (newFaction != null && newFaction.IsPlayer
                && __instance.RaceProps.Humanlike && !__instance.Dead)
            {
                if (__state.captivityTrauma)
                {
                    TraumaDef captivity = DC_DefOf.DC_Trauma_Captivity;
                    if (captivity != null)
                        TraumaUtility.ApplyTrauma(__instance, captivity);
                }

                InheritanceUtility.TryApplyInheritance(__instance);
            }

            if (__instance?.RaceProps == null || !__instance.RaceProps.Humanlike) return;
            if (newFaction == null || !newFaction.IsPlayer) return;
            FamilyLifeUtility.NotifyReturned(__instance);
            FamilyLifeUtility.RefreshAllLastOfTheLine(announceLast: false, announceContinue: true);
        }

        public static Exception Finalizer(SetFactionPatchState __state, Exception __exception)
        {
            if (__state.suppressGoodwill) FamilyJoinUtility.ExitSuppress();
            return __exception;
        }
    }

    [HarmonyPatch(typeof(Pawn), nameof(Pawn.SpawnSetup))]
    public static class Patch_Pawn_SpawnSetup
    {
        public static void Postfix(Pawn __instance, Map map, bool respawningAfterLoad)
        {
            if (respawningAfterLoad) return;

            ParentReunionUtility.NotifySpawned(__instance);

            if (__instance?.Faction != null && __instance.Faction.IsPlayer
                && __instance.RaceProps.Humanlike && !__instance.Dead)
                InheritanceUtility.TryApplyInheritance(__instance);

            FamilyEchoUtility.NotifyChildArrived(__instance, map);
            FamilyBeatsUtility.NotifySpawned(__instance);

            if (__instance != null && map != null)
                FamilyJoinUtility.NotifySpawned(__instance);

            FamilyLifeUtility.NotifyReturned(__instance);
        }
    }

    [HarmonyPatch(typeof(PregnancyUtility), nameof(PregnancyUtility.ApplyBirthOutcome))]
    public static class Patch_Pregnancy_ApplyBirthOutcome
    {
        public static void Postfix(Thing __result, Pawn geneticMother, Pawn father)
        {
            try
            {
                if (__result == null) return;
                if (__result is not Pawn baby) return;
                if (!baby.RaceProps.Humanlike || baby.Dead) return;
                InheritanceUtility.TryApplyInheritance(baby);
                FamilyLifeUtility.NotifyBirth(baby);
            }
            catch (Exception e)
            {
                Log.Warning("[DeepColony] Birth postfix failed: " + e.Message);
            }
        }

        // AZR-158 / AZR-159 — swallow so the mother is not tick-wedged, then
        // restore a healthy pregnancy instead of ending labor with no baby.
        public static Exception Finalizer(Exception __exception, Pawn geneticMother, Pawn father)
        {
            if (__exception == null)
            {
                return null;
            }

            string who = geneticMother?.LabelShort ?? "unknown";
            Log.Warning("[DeepColony] ApplyBirthOutcome failed for " + who
                + " (" + __exception.GetType().Name + "): " + __exception.Message
                + ". Pregnancy will be restored.");
            LaborWedgeRecovery.NoteFailedBirth(geneticMother, father: father);
            return null;
        }
    }

    [HarmonyPatch(typeof(Building_Casket), nameof(Building_Casket.TryAcceptThing))]
    public static class Patch_Casket_TryAcceptThing_Funeral
    {
        public static void Postfix(Building_Casket __instance, Thing thing, bool __result)
        {
            if (!__result || thing is not Corpse corpse) return;
            Pawn inner = corpse.InnerPawn;
            if (inner == null) return;
            FuneralUtility.NotifyBodyLaidToRest(inner, "DC_FuneralEase");
        }
    }

    [HarmonyPatch(typeof(Corpse), nameof(Corpse.Destroy))]
    public static class Patch_Corpse_Destroy_Cremation
    {
        public static void Prefix(Corpse __instance, DestroyMode mode)
        {
            if (__instance?.InnerPawn == null) return;
            if (__instance.ParentHolder is Building_Casket) return;
            if (!__instance.IsBurning()) return;
            FuneralUtility.NotifyBodyLaidToRest(__instance.InnerPawn, "DC_FuneralBurn");
        }
    }

    [HarmonyPatch(typeof(IncidentWorker), "TryExecuteWorker")]
    public static class Patch_IncidentWorker_OdysseyCrash
    {
        public static void Postfix(IncidentWorker __instance, IncidentParms parms, bool __result)
        {
            if (!__result) return;
            AnomalyOdysseyTraumaUtility.NotifyIncident(__instance?.def, parms);
        }
    }

    [HarmonyPatch(typeof(Lord), nameof(Lord.Cleanup))]
    public static class Patch_Lord_Cleanup_IdeologyFuneral
    {
        public static void Prefix(Lord __instance)
        {
            if (!ModsConfig.IdeologyActive) return;
            if (__instance?.LordJob == null) return;
            FuneralUtility.NotifyIdeologyRitual(__instance.LordJob);
        }
    }
}
