using HarmonyLib;
using RimWorld;
using Verse;

namespace DeepColony.Patches
{
    // ── Faction reputation ───────────────────────────────────────────────────────

    [HarmonyPatch(typeof(IncidentWorker_RaidEnemy), "TryExecuteWorker")]
    public static class Patch_RaidEnemy_TryExecute
    {
        public static void Postfix(IncidentParms parms, bool __result)
        {
            if (!__result || parms?.faction == null) return;
            if (parms.target is not Map map) return;
            if (map.ParentFaction != Faction.OfPlayer) return;
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

    // ── Trauma ───────────────────────────────────────────────────────────────────

    [HarmonyPatch(typeof(Pawn_HealthTracker), "MakeDowned")]
    public static class Patch_MakeDowned_Trauma
    {
        private const float CombatShockChance = 0.40f;
        private static readonly AccessTools.FieldRef<Pawn_HealthTracker, Pawn> PawnField =
            AccessTools.FieldRefAccess<Pawn_HealthTracker, Pawn>("pawn");

        public static void Postfix(Pawn_HealthTracker __instance, DamageInfo? dinfo)
        {
            Pawn victim = PawnField(__instance);
            if (victim == null || !victim.IsColonistPlayerControlled) return;
            if (!dinfo.HasValue) return;

            DamageInfo info = dinfo.Value;
            if (info.Def == null || info.Def.isExplosive) return;

            Thing instigator = info.Instigator;
            if (instigator == null || !instigator.HostileTo(victim)) return;
            if (!Rand.Chance(CombatShockChance)) return;

            TraumaDef def = DC_DefOf.DC_Trauma_CombatShock;
            if (def != null) TraumaUtility.ApplyTrauma(victim, def);
        }
    }

    [HarmonyPatch(typeof(Pawn), nameof(Pawn.Kill))]
    public static class Patch_Pawn_Kill_Trauma
    {
        public static void Postfix(Pawn __instance, DamageInfo? dinfo)
        {
            if (!__instance.RaceProps.Humanlike) return;

            MentorshipUtility.ClearAllLinksInvolving(__instance);

            bool wasColonist = __instance.Faction == Faction.OfPlayer || __instance.IsColonist;
            if (wasColonist)
            {
                GameComp_DeepColony.Instance?.NotifyColonistDied(__instance);
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
            if (map == null) return;

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

                if (TraumaUtility.HasTrauma(colonist, loss)
                    || TraumaUtility.HasTrauma(colonist, DC_DefOf.DC_Trauma_BereavementShock))
                {
                    TraumaDef bereave = DC_DefOf.DC_Trauma_BereavementShock;
                    if (bereave != null)
                        TraumaUtility.ApplyTrauma(colonist, bereave, __instance);
                }
                else
                {
                    TraumaUtility.ApplyTrauma(colonist, loss, __instance);
                }
            }
        }
    }

    // ── Generations + captivity ──────────────────────────────────────────────────

    [HarmonyPatch(typeof(Pawn), nameof(Pawn.SetFaction))]
    public static class Patch_Pawn_SetFaction_DeepColony
    {
        public static void Prefix(Pawn __instance, Faction newFaction, out bool __state)
        {
            __state = false;
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

            __state = prisonerCaptivity || rescuedFormer;
        }

        public static void Postfix(Pawn __instance, Faction newFaction, bool __state)
        {
            if (newFaction == null || !newFaction.IsPlayer) return;
            if (!__instance.RaceProps.Humanlike || __instance.Dead) return;

            if (__state)
            {
                TraumaDef captivity = DC_DefOf.DC_Trauma_Captivity;
                if (captivity != null)
                    TraumaUtility.ApplyTrauma(__instance, captivity);
            }

            InheritanceUtility.TryApplyInheritance(__instance);
        }
    }

    [HarmonyPatch(typeof(Pawn), nameof(Pawn.SpawnSetup))]
    public static class Patch_Pawn_SpawnSetup_Inheritance
    {
        public static void Postfix(Pawn __instance, Map map, bool respawningAfterLoad)
        {
            if (respawningAfterLoad) return;
            if (__instance?.Faction == null || !__instance.Faction.IsPlayer) return;
            if (!__instance.RaceProps.Humanlike || __instance.Dead) return;
            InheritanceUtility.TryApplyInheritance(__instance);
        }
    }

    [HarmonyPatch(typeof(PregnancyUtility), nameof(PregnancyUtility.ApplyBirthOutcome))]
    public static class Patch_Pregnancy_ApplyBirthOutcome
    {
        public static void Postfix(Thing __result, Pawn geneticMother, Pawn father)
        {
            if (__result is not Pawn baby) return;
            if (!baby.RaceProps.Humanlike) return;

            // Ensure parent links exist before inheritance rolls.
            if (geneticMother != null && baby.relations != null
                && !baby.relations.DirectRelationExists(PawnRelationDefOf.Parent, geneticMother))
            {
                // Vanilla usually already added these; no-op if present.
            }

            InheritanceUtility.TryApplyInheritance(baby);
        }
    }
}
