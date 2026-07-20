using System.Collections.Generic;
using System.Reflection;
using HarmonyLib;
using RimWorld;
using Verse;
using Verse.AI.Group;

namespace Nemesis
{
    // --- Nemesis cannot die until the hunt is over (Dredd foundation) ---

    [HarmonyPatch]
    public static class Patch_Pawn_Kill_Nemesis
    {
        [HarmonyTargetMethod]
        static MethodBase TargetMethod() =>
            AccessTools.Method(typeof(Pawn), "Kill", new[] { typeof(DamageInfo?), typeof(Hediff) });

        [HarmonyPrefix]
        [HarmonyPriority(Priority.First)]
        static bool Prefix(Pawn __instance)
        {
            GameComponent_Nemesis comp = GameComponent_Nemesis.Instance;
            if (comp == null || !comp.IsNemesisPawn(__instance)) return true;
            comp.HandleLethalDamage(__instance);
            return false;
        }
    }

    // --- Triggers ---

    [HarmonyPatch]
    public static class Patch_Pawn_Kill_TriggerNemesis
    {
        [HarmonyTargetMethod]
        static MethodBase TargetMethod() =>
            AccessTools.Method(typeof(Pawn), "Kill", new[] { typeof(DamageInfo?), typeof(Hediff) });

        [HarmonyPostfix]
        static void Postfix(Pawn __instance, DamageInfo? dinfo)
        {
            GameComponent_Nemesis comp = GameComponent_Nemesis.Instance;
            if (comp == null || comp.IsEngaged) return;
            if (__instance.Faction == null || __instance.Faction.IsPlayer || __instance.Faction.def.hidden) return;
            if (!__instance.RaceProps.Humanlike) return;

            Pawn attacker = dinfo?.Instigator as Pawn;
            if (attacker == null || !attacker.IsColonist) return;

            if (!Rand.Chance(NemesisMod.Settings?.killedAllyChance ?? 0.15f)) return;

            comp.CreateNemesis(__instance, NemesisTargetMode.Pawn, NemesisTrigger.KilledAlly, attacker);
        }
    }

    [HarmonyPatch]
    public static class Patch_Pawn_Kill_FixationTrigger
    {
        [HarmonyTargetMethod]
        static MethodBase TargetMethod() =>
            AccessTools.Method(typeof(Pawn), "Kill", new[] { typeof(DamageInfo?), typeof(Hediff) });

        [HarmonyPostfix]
        static void Postfix(Pawn __instance, DamageInfo? dinfo)
        {
            GameComponent_Nemesis comp = GameComponent_Nemesis.Instance;
            if (comp == null || comp.IsEngaged) return;
            if (!__instance.IsColonist) return;

            Pawn killer = dinfo?.Instigator as Pawn;
            if (killer?.Faction == null || killer.Faction.IsPlayer || killer.Faction.def.hidden) return;
            if (!killer.RaceProps.Humanlike) return;

            if (!Rand.Chance(NemesisMod.Settings?.fixationChance ?? 0.10f)) return;

            Map map = __instance.Map;
            if (map?.mapPawns?.FreeColonistsSpawned == null) return;

            List<Pawn> candidates = new List<Pawn>();
            List<Pawn> colonists = map.mapPawns.FreeColonistsSpawned;
            for (int i = 0; i < colonists.Count; i++)
            {
                Pawn p = colonists[i];
                if (p != null && p != __instance && !p.Dead)
                    candidates.Add(p);
            }
            if (candidates.Count == 0) return;

            comp.CreateNemesis(killer, NemesisTargetMode.Pawn, NemesisTrigger.Fixation,
                candidates[Rand.Range(0, candidates.Count)], useAsNemesis: killer);
        }
    }

    /// <summary>
    /// Wounded-and-escaped: a lethal blow on a hostile humanlike sometimes becomes a cinematic escape
    /// and starts a personal hunt against the colonist who struck them.
    /// </summary>
    [HarmonyPatch]
    public static class Patch_Pawn_Kill_WoundedEscape
    {
        [HarmonyTargetMethod]
        static MethodBase TargetMethod() =>
            AccessTools.Method(typeof(Pawn), "Kill", new[] { typeof(DamageInfo?), typeof(Hediff) });

        [HarmonyPrefix]
        [HarmonyPriority(Priority.High)]
        static bool Prefix(Pawn __instance, DamageInfo? dinfo)
        {
            GameComponent_Nemesis comp = GameComponent_Nemesis.Instance;
            if (comp == null || comp.IsEngaged) return true;
            if (__instance.Faction == null || __instance.Faction.IsPlayer || __instance.Faction.def.hidden) return true;
            if (!__instance.RaceProps.Humanlike) return true;
            if (comp.IsNemesisPawn(__instance)) return true;

            Pawn attacker = dinfo?.Instigator as Pawn;
            if (attacker == null || !attacker.IsColonist) return true;

            if (!Rand.Chance(NemesisMod.Settings?.woundedEscapeChance ?? 0.12f)) return true;

            HediffDef anesthetic = DefDatabase<HediffDef>.GetNamedSilentFail("Anesthetic");
            if (anesthetic != null)
                __instance.health.AddHediff(anesthetic);

            comp.CreateNemesis(__instance, NemesisTargetMode.Pawn, NemesisTrigger.WoundedAndEscaped,
                attacker, useAsNemesis: __instance);
            return false;
        }
    }

    [HarmonyPatch]
    public static class Patch_PrisonBreak_TriggerNemesis
    {
        [HarmonyTargetMethod]
        static MethodBase TargetMethod() =>
            AccessTools.Method(typeof(PrisonBreakUtility), nameof(PrisonBreakUtility.StartPrisonBreak), new[]
            {
                typeof(Pawn),
                typeof(string).MakeByRefType(),
                typeof(string).MakeByRefType(),
                typeof(LetterDef).MakeByRefType(),
                typeof(List<Pawn>).MakeByRefType(),
            });

        [HarmonyPostfix]
        static void Postfix(Pawn initiator, List<Pawn> escapingPrisoners)
        {
            GameComponent_Nemesis comp = GameComponent_Nemesis.Instance;
            if (comp == null || comp.IsEngaged) return;
            if (escapingPrisoners == null || escapingPrisoners.Count == 0) return;
            if (initiator?.Faction == null || initiator.Faction.IsPlayer || initiator.Faction.def.hidden) return;

            if (!Rand.Chance(NemesisMod.Settings?.prisonerEscapedChance ?? 0.10f)) return;

            comp.CreateNemesis(initiator, NemesisTargetMode.Colony, NemesisTrigger.PrisonerEscaped);
        }
    }

    /// <summary>Ideology slave rebellion — patched only when the method exists.</summary>
    [HarmonyPatch]
    public static class Patch_SlaveRebellion_TriggerNemesis
    {
        static bool Prepare()
        {
            return AccessTools.Method(
                typeof(SlaveRebellionUtility),
                "StartSlaveRebellion",
                new[] { typeof(Pawn), typeof(bool) }) != null;
        }

        [HarmonyTargetMethod]
        static MethodBase TargetMethod()
        {
            return AccessTools.Method(
                typeof(SlaveRebellionUtility),
                "StartSlaveRebellion",
                new[] { typeof(Pawn), typeof(bool) });
        }

        [HarmonyPostfix]
        static void Postfix(Pawn initiator)
        {
            GameComponent_Nemesis comp = GameComponent_Nemesis.Instance;
            if (comp == null || comp.IsEngaged) return;
            if (initiator?.Faction == null || initiator.Faction.IsPlayer || initiator.Faction.def.hidden) return;
            if (!Rand.Chance(NemesisMod.Settings?.slaveEscapedChance ?? 0.12f)) return;

            comp.CreateNemesis(initiator, NemesisTargetMode.Colony, NemesisTrigger.SlaveEscaped);
        }
    }

    // --- End-condition dirty flags (cheap; real check is staggered in GameComponent) ---

    [HarmonyPatch]
    public static class Patch_Pawn_Kill_EndConditionDirty
    {
        [HarmonyTargetMethod]
        static MethodBase TargetMethod() =>
            AccessTools.Method(typeof(Pawn), "Kill", new[] { typeof(DamageInfo?), typeof(Hediff) });

        [HarmonyPostfix]
        static void Postfix(Pawn __instance)
        {
            GameComponent_Nemesis comp = GameComponent_Nemesis.Instance;
            if (comp == null || !comp.IsEngaged) return;
            if (comp.IsTargetPawn(__instance) || comp.IsNemesisPawn(__instance))
                NemesisRegistry.ResolutionDirty = true;
        }
    }

    [HarmonyPatch(typeof(Pawn), nameof(Pawn.SetFaction))]
    public static class Patch_Pawn_SetFaction_HandOver
    {
        [HarmonyPostfix]
        static void Postfix(Pawn __instance, Faction newFaction)
        {
            GameComponent_Nemesis comp = GameComponent_Nemesis.Instance;
            if (comp == null || !comp.IsEngaged) return;
            if (!comp.IsTargetPawn(__instance)) return;
            if (newFaction == Faction.OfPlayer) return;
            NemesisRegistry.ResolutionDirty = true;
        }
    }

    [HarmonyPatch(typeof(Pawn_GuestTracker), nameof(Pawn_GuestTracker.SetGuestStatus))]
    public static class Patch_GuestStatus_ResolutionDirty
    {
        [HarmonyPostfix]
        static void Postfix(Pawn ___pawn)
        {
            GameComponent_Nemesis comp = GameComponent_Nemesis.Instance;
            if (comp == null || !comp.IsEngaged) return;
            if (___pawn != null && comp.IsNemesisPawn(___pawn))
            {
                NemesisRegistry.ResolutionDirty = true;
                // Arrest / guest-status change counts as engagement with the nemesis.
                if (___pawn.IsPrisonerOfColony)
                    comp.Data?.NotifyPlayerEngagedNemesis();
            }
        }
    }

    /// <summary>Colonist damage on the nemesis pawn resets the "ignored" streak.</summary>
    [HarmonyPatch(typeof(Thing), nameof(Thing.TakeDamage))]
    public static class Patch_TakeDamage_NemesisEngagement
    {
        [HarmonyPostfix]
        static void Postfix(Thing __instance, DamageInfo dinfo)
        {
            Pawn pawn = __instance as Pawn;
            if (pawn == null) return;
            GameComponent_Nemesis comp = GameComponent_Nemesis.Instance;
            if (comp == null || !comp.IsEngaged || !comp.IsNemesisPawn(pawn)) return;
            Pawn instigator = dinfo.Instigator as Pawn;
            if (instigator == null || !instigator.IsColonist) return;
            comp.Data?.NotifyPlayerEngagedNemesis();
        }
    }

    /// <summary>Elevated food-poison chance when eating from a silently tainted stack.</summary>
    [HarmonyPatch(typeof(Thing), nameof(Thing.Ingested))]
    public static class Patch_Ingested_FoodTampering
    {
        [HarmonyPostfix]
        static void Postfix(Thing __instance, Pawn ingester)
        {
            if (__instance == null || ingester == null || !ingester.IsColonist) return;
            NemesisData data = GameComponent_Nemesis.Instance?.Data;
            if (data == null || !data.active) return;
            if (data.taintedFoodThingId < 0 || __instance.thingIDNumber != data.taintedFoodThingId) return;
            if (Find.TickManager.TicksGame > data.taintedUntilTick) return;
            if (!Rand.Chance(0.40f)) return;

            HediffDef poison = DefDatabase<HediffDef>.GetNamedSilentFail("FoodPoisoning");
            if (poison == null) return;
            ingester.health.AddHediff(poison);
        }
    }

    /// <summary>Credit-taking + living-truce raid warning on hostile raid execution.</summary>
    [HarmonyPatch]
    public static class Patch_RaidEnemy_CreditTaking
    {
        static bool Prepare()
        {
            return AccessTools.Method(typeof(IncidentWorker_RaidEnemy), "TryExecuteWorker",
                new[] { typeof(IncidentParms) }) != null;
        }

        [HarmonyTargetMethod]
        static MethodBase TargetMethod() =>
            AccessTools.Method(typeof(IncidentWorker_RaidEnemy), "TryExecuteWorker",
                new[] { typeof(IncidentParms) });

        [HarmonyPrefix]
        static void Prefix(IncidentParms parms)
        {
            // Living truce: warn before a genuine hostile raid fires.
            NemesisData data = GameComponent_Nemesis.Instance?.Data;
            if (data == null || data.active || data.truceUntilTick <= 0) return;
            if (parms?.faction == null || parms.faction.IsPlayer) return;
            if (!parms.faction.HostileTo(Faction.OfPlayer)) return;
            Find.LetterStack.ReceiveLetter(
                "Nemesis_Letter_TruceWarnTitle".Translate(data.nemesisName),
                "Nemesis_Letter_TruceWarnBody".Translate(data.nemesisName),
                LetterDefOf.ThreatSmall);
        }

        [HarmonyPostfix]
        static void Postfix(bool __result, IncidentParms parms)
        {
            if (!__result || parms == null) return;
            GameComponent_Nemesis comp = GameComponent_Nemesis.Instance;
            if (comp?.Data == null || !comp.Data.active) return;
            Faction raidFaction = parms.faction;
            if (raidFaction == null || raidFaction.IsPlayer) return;
            Faction nemesisFaction = NemesisActions.FindFaction(comp.Data);
            if (nemesisFaction != null && raidFaction == nemesisFaction) return;
            if (!Rand.Chance(NemesisMod.Settings?.raidCreditChance ?? 0.15f)) return;

            Find.LetterStack.ReceiveLetter(
                "Nemesis_Letter_RaidCreditTitle".Translate(comp.Data.nemesisName),
                "Nemesis_Letter_RaidCreditBody".Translate(
                    comp.Data.nemesisName, raidFaction.Name, NemesisTaunts.TargetPhrase(comp.Data)),
                LetterDefOf.ThreatSmall);
        }
    }

    /// <summary>Track when trade/visitor lords clean up so InformantReveal can fire soon after.</summary>
    [HarmonyPatch(typeof(Lord), nameof(Lord.Cleanup))]
    public static class Patch_LordCleanup_VisitorLeave
    {
        [HarmonyPostfix]
        static void Postfix(Lord __instance)
        {
            if (__instance?.LordJob == null) return;
            LordJob job = __instance.LordJob;
            bool visitor = job is LordJob_TradeWithColony || job is LordJob_VisitColony;
            if (!visitor) return;
            NemesisData data = GameComponent_Nemesis.Instance?.Data;
            if (data == null || !data.active) return;
            data.lastVisitorLeaveTick = Find.TickManager.TicksGame;
        }
    }
}
